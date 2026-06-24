using OmniFlow.Application.DTOs.Account;

namespace OmniFlow.Application.Interfaces;

public interface IAccountService
{
	Task<RegistrationVerificationResponse> RegisterAsync(RegisterRequest request);
	Task<AuthenticationResponse> LoginAsync(AuthenticationRequest request);
	Task<AuthenticationResponse> RefreshTokenAsync(string token);
	Task VerifyEmailAsync(VerifyEmailRequest request);
	Task ResendVerificationEmailAsync(ResendVerificationEmailRequest request);
	Task ChangeVerificationEmailAsync(ChangeVerificationEmailRequest request);
	Task ForgotPasswordAsync(string email);
	Task ResetPasswordAsync(ResetPasswordRequest request);
}
