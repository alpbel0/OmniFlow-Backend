using MediatR;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;

namespace OmniFlow.Application.Features.Users.Commands.UpdateProfile;

public class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, Unit>
{
	private readonly IUserRepositoryAsync _userRepository;
	private readonly IAuthenticatedUserService _authenticatedUserService;

	public UpdateProfileCommandHandler(
		IUserRepositoryAsync userRepository,
		IAuthenticatedUserService authenticatedUserService)
	{
		_userRepository = userRepository;
		_authenticatedUserService = authenticatedUserService;
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

		if (request.UpdateLocation)
		{
			user.Location = string.IsNullOrWhiteSpace(request.Location)
				? null
				: request.Location.Trim();
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
