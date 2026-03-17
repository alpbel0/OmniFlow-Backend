namespace OmniFlow.Application.DTOs.Account;

public class AuthenticationResponse
{
	public string AccessToken { get; set; } = default!;
	public string? RefreshToken { get; set; }
	public Guid Id { get; set; }
	public string Username { get; set; } = default!;
	public string Email { get; set; } = default!;
	public string Role { get; set; } = default!;
}
