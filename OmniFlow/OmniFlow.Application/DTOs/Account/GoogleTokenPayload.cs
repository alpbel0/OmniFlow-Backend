namespace OmniFlow.Application.DTOs.Account;

public class GoogleTokenPayload
{
	public string Email { get; set; } = string.Empty;
	public bool EmailVerified { get; set; }
	public string? Name { get; set; }
	public string Subject { get; set; } = string.Empty;
	public string? Picture { get; set; }
}
