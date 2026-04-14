namespace OmniFlow.Application.DTOs.Account;

public class VerifyEmailRequest
{
	public string Email { get; set; } = default!;
	public string Token { get; set; } = default!;
}