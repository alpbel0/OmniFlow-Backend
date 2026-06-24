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
			|| _mailSettings.SmtpPort <= 0
			|| string.IsNullOrWhiteSpace(_mailSettings.SenderEmail)
			|| string.IsNullOrWhiteSpace(_mailSettings.FrontendVerifyUrl))
		{
			_logger.LogError("Verification email delivery is unavailable because mail settings are incomplete.");
			throw new SmtpException("Mail service configuration is incomplete.");
		}

		var smtpUsername = _mailSettings.SmtpUsername?.Trim();
		var smtpPassword = string.IsNullOrWhiteSpace(_mailSettings.SmtpPassword)
			? string.Empty
			: string.Concat(_mailSettings.SmtpPassword.Where(c => !char.IsWhiteSpace(c)));

		try
		{
			using var message = new MailMessage
			{
				From = new MailAddress(_mailSettings.SenderEmail.Trim(), _mailSettings.SenderName),
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
				Timeout = 10000
			};

			if (!string.IsNullOrWhiteSpace(smtpUsername))
			{
				client.Credentials = new NetworkCredential(
					smtpUsername,
					smtpPassword);
			}

			_logger.LogInformation(
				"Sending verification email to {Email} via {SmtpHost}:{SmtpPort}.",
				email,
				_mailSettings.SmtpHost,
				_mailSettings.SmtpPort);

			await client.SendMailAsync(message);
			_logger.LogInformation("Verification email sent to {Email}.", email);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to send verification email to {Email}. Error: {Message}", email, ex.Message);
			if (ex is SmtpException)
				throw;

			throw new SmtpException("Verification email delivery failed.", ex);
		}
	}

	public async Task SendPasswordResetEmailAsync(string email, string resetUrl)
	{
		if (string.IsNullOrWhiteSpace(_mailSettings.SmtpHost)
			|| _mailSettings.SmtpPort <= 0
			|| string.IsNullOrWhiteSpace(_mailSettings.SenderEmail)
			|| string.IsNullOrWhiteSpace(_mailSettings.FrontendResetUrl))
		{
			_logger.LogError("Password reset email delivery is unavailable because mail settings are incomplete.");
			throw new SmtpException("Mail service configuration is incomplete.");
		}

		var smtpUsername = _mailSettings.SmtpUsername?.Trim();
		var smtpPassword = string.IsNullOrWhiteSpace(_mailSettings.SmtpPassword)
			? string.Empty
			: string.Concat(_mailSettings.SmtpPassword.Where(c => !char.IsWhiteSpace(c)));

		try
		{
			using var message = new MailMessage
			{
				From = new MailAddress(_mailSettings.SenderEmail.Trim(), _mailSettings.SenderName),
				Subject = "Reset your OmniFlow password",
				Body = $"<p>You requested a password reset. Click the link below to reset your password:</p><p><a href=\"{resetUrl}\">Reset password</a></p><p>If you didn't request this, you can ignore this email.</p>",
				IsBodyHtml = true
			};
			message.To.Add(email);

			using var client = new SmtpClient(_mailSettings.SmtpHost, _mailSettings.SmtpPort)
			{
				EnableSsl = _mailSettings.EnableSsl,
				DeliveryMethod = SmtpDeliveryMethod.Network,
				UseDefaultCredentials = false,
				Timeout = 10000
			};

			if (!string.IsNullOrWhiteSpace(smtpUsername))
			{
				client.Credentials = new NetworkCredential(
					smtpUsername,
					smtpPassword);
			}

			_logger.LogInformation(
				"Sending password reset email to {Email} via {SmtpHost}:{SmtpPort}.",
				email,
				_mailSettings.SmtpHost,
				_mailSettings.SmtpPort);

			await client.SendMailAsync(message);
			_logger.LogInformation("Password reset email sent to {Email}.", email);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to send password reset email to {Email}. Error: {Message}", email, ex.Message);
			if (ex is SmtpException)
				throw;

			throw new SmtpException("Password reset email delivery failed.", ex);
		}
	}
}
