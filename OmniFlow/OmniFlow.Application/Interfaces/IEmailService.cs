namespace OmniFlow.Application.Interfaces;

public interface IEmailService
{
	Task SendVerificationEmailAsync(string email, string verificationUrl);
	Task SendPasswordResetEmailAsync(string email, string resetUrl);
}