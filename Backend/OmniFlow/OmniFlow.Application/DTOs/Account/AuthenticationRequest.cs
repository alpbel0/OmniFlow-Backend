namespace OmniFlow.Application.DTOs.Account;

public class AuthenticationRequest
{
	public string Email { get; set; } = default!;
	public string Password { get; set; } = default!;
}
