namespace OmniFlow.Application.DTOs.Account;

public class RegistrationVerificationResponse
{
	public string Message { get; set; } = default!;
	public bool RequiresEmailVerification { get; set; }
}