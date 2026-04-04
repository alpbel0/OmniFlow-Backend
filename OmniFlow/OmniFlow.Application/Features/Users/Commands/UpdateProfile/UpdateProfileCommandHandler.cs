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

		if (request.Bio != null)
		{
			user.Bio = request.Bio.Trim();
		}

		if (request.ProfilePhotoUrl != null)
		{
			user.ProfilePhotoUrl = request.ProfilePhotoUrl.Trim();
		}

		await _userRepository.UpdateAsync(user);
		return Unit.Value;
	}
}