using MediatR;
using Microsoft.AspNetCore.Mvc;
using SabaPlus.API.Models.BetResultCheck;

namespace SabaPlus.API.Controllers;

[Route("api/bet")]
[ApiController]
public class BetController(IMediator mediator) : ControllerBase
{
	[HttpPost("ResultCheck")]
	public Task<BetResultCheckResult> BetResultCheckAsync(BetResultCheckRequest request)
		=> mediator.Send(request);
}