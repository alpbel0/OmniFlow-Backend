using System.Net.Mail;
using OmniFlow.Application.Interfaces;

namespace OmniFlow.Api.IntegrationTests.Setup;

public sealed class TestEmailService : IEmailService
{
    public int VerificationEmailCount { get; private set; }
    public string? LastVerificationEmail { get; private set; }
    public string? LastVerificationUrl { get; private set; }
    public string? LastPasswordResetEmail { get; private set; }
    public string? LastPasswordResetUrl { get; private set; }
    public bool FailPasswordResetDelivery { get; set; }

    public Task SendVerificationEmailAsync(string email, string verificationUrl)
    {
        VerificationEmailCount++;
        LastVerificationEmail = email;
        LastVerificationUrl = verificationUrl;
        return Task.CompletedTask;
    }

    public Task SendPasswordResetEmailAsync(string email, string resetUrl)
    {
        if (FailPasswordResetDelivery)
            throw new SmtpException("Simulated SMTP failure.");

        LastPasswordResetEmail = email;
        LastPasswordResetUrl = resetUrl;
        return Task.CompletedTask;
    }

    public void Reset()
    {
        VerificationEmailCount = 0;
        LastVerificationEmail = null;
        LastVerificationUrl = null;
        LastPasswordResetEmail = null;
        LastPasswordResetUrl = null;
        FailPasswordResetDelivery = false;
    }
}
