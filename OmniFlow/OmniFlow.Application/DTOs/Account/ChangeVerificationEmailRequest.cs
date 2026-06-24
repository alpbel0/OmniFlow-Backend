namespace OmniFlow.Application.DTOs.Account;

public class ChangeVerificationEmailRequest
{
	public string OldEmail { get; set; } = default!;
	public string NewEmail { get; set; } = default!;
	public string Password { get; set; } = default!;
}
