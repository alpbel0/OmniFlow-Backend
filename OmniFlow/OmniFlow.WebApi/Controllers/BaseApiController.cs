using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace OmniFlow.WebApi.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/[controller]")]
public abstract class BaseApiController : ControllerBase
{
	private ISender? _mediator;

	protected ISender Mediator =>
		_mediator ??= HttpContext.RequestServices.GetRequiredService<ISender>();
}
