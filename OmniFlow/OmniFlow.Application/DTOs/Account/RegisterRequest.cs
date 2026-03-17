namespace OmniFlow.Application.DTOs.Account;

public class RegisterRequest
{
	public string Username { get; set; } = default!;
	public string Email { get; set; } = default!;
	public string Password { get; set; } = default!;
	public string ConfirmPassword { get; set; } = default!;
}
