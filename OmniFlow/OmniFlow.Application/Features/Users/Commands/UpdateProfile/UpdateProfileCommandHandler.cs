using MediatR;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;

namespace OmniFlow.Application.Features.Users.Commands.UpdateProfile;

public class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, Unit>
{
	private readonly IUserRepositoryAsync _userRepository;
	private readonly IAuthenticatedUserService _authenticatedUserService;
	private readonly IGeocodingService _geocodingService;

	public UpdateProfileCommandHandler(
		IUserRepositoryAsync userRepository,
		IAuthenticatedUserService authenticatedUserService,
		IGeocodingService geocodingService)
	{
		_userRepository = userRepository;
		_authenticatedUserService = authenticatedUserService;
		_geocodingService = geocodingService;
	}

	public async Task<Unit> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
	{
		var currentUserId = Guid.Parse(_authenticatedUserService.UserId);
		var user = await _userRepository.GetByIdAsync(currentUserId);

		if (user == null)
		{
			throw new EntityNotFoundException("User", currentUserId);
		}

		if (request.UpdateBio)
		{
			user.Bio = string.IsNullOrWhiteSpace(request.Bio)
				? null
				: request.Bio.Trim();
		}

		if (request.UpdateProfilePhotoUrl)
		{
			user.ProfilePhotoUrl = string.IsNullOrWhiteSpace(request.ProfilePhotoUrl)
				? null
				: request.ProfilePhotoUrl.Trim();
		}

		var hasManualLocation = request.UpdateLocation && !string.IsNullOrWhiteSpace(request.Location);
		var resolvedLocation = hasManualLocation
			? request.Location!.Trim()
			: null;

		if (!hasManualLocation &&
			request.UpdateLocationCoordinates &&
			request.LocationLatitude.HasValue &&
			request.LocationLongitude.HasValue)
		{
			var reverseResult = await _geocodingService.ReverseGeocodeAsync(
				request.LocationLatitude.Value,
				request.LocationLongitude.Value,
				cancellationToken);

			resolvedLocation = string.IsNullOrWhiteSpace(reverseResult?.DisplayName)
				? null
				: reverseResult.DisplayName.Trim();
		}

		if (request.UpdateLocation || resolvedLocation is not null)
		{
			user.Location = resolvedLocation;
		}

		if (request.UpdateLocationCoordinates)
		{
			user.LocationLatitude = request.LocationLatitude;
			user.LocationLongitude = request.LocationLongitude;
		}

		if (request.UpdateTravelStyles)
		{
			user.TravelStyles = request.TravelStyles?
				.Distinct()
				.ToList() ?? new List<Domain.Enums.TravelStyle>();
		}

		await _userRepository.UpdateAsync(user);
		return Unit.Value;
	}
}
