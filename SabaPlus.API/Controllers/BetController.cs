using MediatR;
using Microsoft.AspNetCore.Mvc;
using SabaPlus.API.Models.BetResultCheck;

namespace SabaPlus.API.Controllers;

[Route("api/bet")]
[ApiController]
public class BetController(IMediator mediator) : ControllerBase
{
	[HttpPost("ResultCheckSchdedule")]
	public Task<BetResultCheckResult> ResultCheckSchdeduleAsync(BetResultCheckScheduleRequest request)
		=> mediator.Send(request);
}