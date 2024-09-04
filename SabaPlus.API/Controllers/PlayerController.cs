using Microsoft.AspNetCore.Mvc;
using SabaPlus.API.Models.Balance;
using SabaPlus.API.Models.Bonus;
using SabaPlus.API.Models.FreeSpin;
using SabaPlus.API.Models.PlaceBet;
using SabaPlus.API.Models.PlaceBetAndSettle;
using SabaPlus.API.Models.PlayerInfo;
using SabaPlus.API.Models.Settle;
using SabaPlus.API.Models.VoidGame;
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

		[HttpPost("GetPlayerInfo")]
		public async ValueTask<GetPlayerInfoResult> GetPlayerInfo(
			[FromServices] IGetService<GetPlayerInfoRequest, GetPlayerInfoResult> service,
			GetPlayerInfoRequest source)
		{
			var result = await service.GetAsync(source, cancellationToken: HttpContext.RequestAborted).ConfigureAwait(false);

			return result;
		}

		[HttpPost("PlaceBetAndSettle")]
		public async ValueTask<PlaceBetAndSettleResult> PlaceBetAndSettle(
			[FromServices] IGetService<PlaceBetAndSettleRequest, PlaceBetAndSettleResult> service,
			PlaceBetAndSettleRequest source)
		{
			var result = await service.GetAsync(source, cancellationToken: HttpContext.RequestAborted).ConfigureAwait(false);

			return result;
		}

		[HttpPost("FreeSpin")]
		public async ValueTask<FreeSpinResult> FreeSpin(
			[FromServices] IGetService<FreeSpinRequest, FreeSpinResult> service,
			FreeSpinRequest source)
		{
			var result = await service.GetAsync(source, cancellationToken: HttpContext.RequestAborted).ConfigureAwait(false);

			return result;
		}

		[HttpPost("VoidGame")]
		public async ValueTask<VoidGameResult> VoidGame(
		[FromServices] IGetService<VoidGameRequest, VoidGameResult> service,
		VoidGameRequest source)
		{
			var result = await service.GetAsync(source, cancellationToken: HttpContext.RequestAborted).ConfigureAwait(false);

			return result;
		}

		[HttpPost("Bonus")]
		public async ValueTask<BonusResult> Bonus(
			[FromServices] IGetService<BonusRequest, BonusResult> service,
				BonusRequest source)
		{
			var result = await service.GetAsync(source, cancellationToken: HttpContext.RequestAborted).ConfigureAwait(false);

			return result;
		}
		
	}
}