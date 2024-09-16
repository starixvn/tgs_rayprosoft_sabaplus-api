using Microsoft.AspNetCore.Mvc;
using SabaPlus.API.Models.Balance;
using SabaPlus.API.Models.Bonus;
using SabaPlus.API.Models.FreeSpin;
using SabaPlus.API.Models.PlaceBet;
using SabaPlus.API.Models.PlaceBetAndSettle;
using SabaPlus.API.Models.PlayerInfo;
using SabaPlus.API.Models.EndGame;
using SabaPlus.API.Models.VoidGame;
using MediatR;

namespace SabaPlus.API.Controllers
{
	[Route("api/player")]
	[ApiController]
	public class PlayerController(IMediator mediator) : ControllerBase
	{
		[HttpPost("placebet")]
		public Task<PlaceBetResult> PlaceBet(PlaceBetRequest request) => mediator.Send(request);

		[HttpPost("getbalance")]
		public Task<GetBalanceResult> GetBalance(GetBalanceRequest request) => mediator.Send(request);

		[HttpPost("endgame")]
		public Task<EndGameResult> EndGame(EndGameRequest request) => mediator.Send(request);

		[HttpPost("GetPlayerInfo")]
		public Task<GetPlayerInfoResult> GetPlayerInfo(GetPlayerInfoRequest request) => mediator.Send(request);

		[HttpPost("PlaceBetAndSettle")]
		public Task<PlaceBetAndSettleResult> PlaceBetAndSettle(PlaceBetAndSettleRequest request) => mediator.Send(request);

		[HttpPost("FreeSpin")]
		public Task<FreeSpinResult> FreeSpin(FreeSpinRequest request) => mediator.Send(request);

		[HttpPost("VoidGame")]
		public Task<VoidGameResult> VoidGame(VoidGameRequest request) => mediator.Send(request);

		[HttpPost("Bonus")]
		public Task<BonusResult> Bonus(BonusRequest request) => mediator.Send(request);
	}
}