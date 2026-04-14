namespace OmniFlow.Application.Settings;

public class MailSettings
{
	public string SmtpHost { get; set; } = default!;
	public int SmtpPort { get; set; }
	public string? SmtpUsername { get; set; }
	public string? SmtpPassword { get; set; }
	public bool EnableSsl { get; set; } = true;
	public string SenderEmail { get; set; } = default!;
	public string SenderName { get; set; } = default!;
	public string FrontendVerifyUrl { get; set; } = default!;
}
