namespace OmniFlow.Application.Settings;

public class MailSettings
{
	public string SmtpHost { get; set; } = default!;
	public int SmtpPort { get; set; }
	public string SenderEmail { get; set; } = default!;
	public string SenderName { get; set; } = default!;
}
