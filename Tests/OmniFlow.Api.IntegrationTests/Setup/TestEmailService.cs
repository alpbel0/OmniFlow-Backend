using System.Net.Mail;
using OmniFlow.Application.Interfaces;

namespace OmniFlow.Api.IntegrationTests.Setup;

public sealed class TestEmailService : IEmailService
{
    public string? LastPasswordResetEmail { get; private set; }
    public string? LastPasswordResetUrl { get; private set; }
    public bool FailPasswordResetDelivery { get; set; }

    public Task SendVerificationEmailAsync(string email, string verificationUrl) => Task.CompletedTask;

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
        LastPasswordResetEmail = null;
        LastPasswordResetUrl = null;
        FailPasswordResetDelivery = false;
    }
}
