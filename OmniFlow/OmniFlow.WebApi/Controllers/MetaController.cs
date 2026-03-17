using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniFlow.WebApi.Models;

namespace OmniFlow.WebApi.Controllers;

[ApiController]
[Route("api/meta")]
public class MetaController : ControllerBase
{
	private readonly IWebHostEnvironment _env;

	public MetaController(IWebHostEnvironment env)
	{
		_env = env;
	}

	/// <summary>Public health check endpoint.</summary>
	[AllowAnonymous]
	[HttpGet("health")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	public IActionResult Health() => Ok(new { status = "healthy" });

	/// <summary>Returns API version and environment info. Requires authentication.</summary>
	[Authorize]
	[HttpGet("info")]
	[ProducesResponseType(typeof(Metadata), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	public IActionResult Info() =>
		Ok(new Metadata("1.0.0", _env.EnvironmentName, "running"));
}
