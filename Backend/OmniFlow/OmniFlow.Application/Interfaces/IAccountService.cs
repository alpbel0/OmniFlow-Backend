using OmniFlow.Application.DTOs.Account;

namespace OmniFlow.Application.Interfaces;

public interface IAccountService
{
	Task<AuthenticationResponse> RegisterAsync(RegisterRequest request);
	Task<AuthenticationResponse> LoginAsync(AuthenticationRequest request);
	Task<AuthenticationResponse> RefreshTokenAsync(string token);
	Task ForgotPasswordAsync(string email);
}
