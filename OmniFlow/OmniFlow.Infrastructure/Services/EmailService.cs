using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Settings;

namespace OmniFlow.Infrastructure.Services;

public class EmailService : IEmailService
{
	private readonly MailSettings _mailSettings;
	private readonly ILogger<EmailService> _logger;

	public EmailService(IOptions<MailSettings> mailSettings, ILogger<EmailService> logger)
	{
		_mailSettings = mailSettings.Value;
		_logger = logger;
	}

	public async Task SendVerificationEmailAsync(string email, string verificationUrl)
	{
		if (string.IsNullOrWhiteSpace(_mailSettings.SmtpHost)
			|| string.IsNullOrWhiteSpace(_mailSettings.SenderEmail)
			|| string.IsNullOrWhiteSpace(_mailSettings.FrontendVerifyUrl))
		{
			_logger.LogInformation("Verification email skipped because mail settings are incomplete for {Email}.", email);
			return;
		}

		try
		{
			using var message = new MailMessage
			{
				From = new MailAddress(_mailSettings.SenderEmail, _mailSettings.SenderName),
				Subject = "Verify your OmniFlow email address",
				Body = $"<p>Please verify your email address by clicking the link below:</p><p><a href=\"{verificationUrl}\">Verify email</a></p>",
				IsBodyHtml = true
			};
			message.To.Add(email);

			using var client = new SmtpClient(_mailSettings.SmtpHost, _mailSettings.SmtpPort)
			{
				EnableSsl = _mailSettings.EnableSsl,
				DeliveryMethod = SmtpDeliveryMethod.Network,
				UseDefaultCredentials = false,
				Timeout = 10000  // 10 second timeout
			};

			if (!string.IsNullOrWhiteSpace(_mailSettings.SmtpUsername))
			{
				client.Credentials = new NetworkCredential(
					_mailSettings.SmtpUsername,
					_mailSettings.SmtpPassword ?? string.Empty);
			}

			await client.SendMailAsync(message);
			_logger.LogInformation("Verification email sent to {Email}.", email);
		}
		catch (Exception ex)
		{
			// Log email failure but don't throw - email delivery shouldn't block registration
			// User can manually request resend-verification if email doesn't arrive
			_logger.LogError(ex, "Failed to send verification email to {Email}. Error: {Message}", email, ex.Message);
		}
	}
}