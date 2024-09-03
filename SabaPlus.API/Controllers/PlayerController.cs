using Microsoft.AspNetCore.Mvc;
using SabaPlus.API.Models.Balance;
using SabaPlus.API.Models.PlaceBet;
using SabaPlus.API.Models.Settle;
using Tgs.ServiceUtility;

namespace SabaPlus.API.Controllers
{
	[Route("api/player")]
	[ApiController]
	public class PlayerController : ControllerBase
	{
		[HttpPost("placebet")]
		public async ValueTask<PlaceBetResult> PlaceBet(
			[FromServices] IGetService<PlaceBetRequest, PlaceBetResult> service,
			PlaceBetRequest source
			)
		{
			var result = await service.GetAsync(source, cancellationToken: HttpContext.RequestAborted).ConfigureAwait(false);

			return result;
		}

		[HttpPost("getbalance")]
		public async ValueTask<GetBalanceResult> GetBalance(
			[FromServices] IGetService<GetBalanceRequest, GetBalanceResult> service,
			GetBalanceRequest source
			)
		{
			var result = await service.GetAsync(source, cancellationToken: HttpContext.RequestAborted).ConfigureAwait(false);

			return result;
		}

		[HttpPost("endgame")]
		public async ValueTask<EndGameResult> EndGame(
			[FromServices] IGetService<EndGameRequest, EndGameResult> service,
			EndGameRequest source
			)
		{
			var result = await service.GetAsync(source, cancellationToken: HttpContext.RequestAborted).ConfigureAwait(false);

			return result;
		}
	}
}