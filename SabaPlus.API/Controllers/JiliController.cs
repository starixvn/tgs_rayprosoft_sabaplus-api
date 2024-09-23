using MediatR;
using Microsoft.AspNetCore.Mvc;
using SabaPlus.API.Middlewares.Attributes;
using SabaPlus.API.Models;
using SabaPlus.API.Models.Balance;
using SabaPlus.API.Models.Bonus;
using SabaPlus.API.Models.EndGame;
using SabaPlus.API.Models.FreeSpin;
using SabaPlus.API.Models.PlaceBet;
using SabaPlus.API.Models.PlaceBetAndSettle;
using SabaPlus.API.Models.PlayerInfo;
using SabaPlus.API.Models.VoidGame;

namespace SabaPlus.API.Controllers;

[Route("api/jili")]
[ApiController]
public class JiliController(IMediator mediator) : ControllerBase
{
	[HttpPost("placebet")]
	public async Task<PlaceBetResult> PlaceBetAsync(PlaceBetRequest request)
		=> await mediator.Send(BuildRequest(request)).ConfigureAwait(false);

	[HttpPost("getbalance")]
	public async Task<GetBalanceResult> GetBalanceAsync(GetBalanceRequest request)
		=> await mediator.Send(BuildRequest(request)).ConfigureAwait(false);

	[HttpPost("endgame")]
	[ReadableBody]
	public async Task<EndGameResult> EndGameAsync(EndGameRequest request) => await mediator.Send(BuildRequest(request)).ConfigureAwait(false);

	[HttpPost("GetPlayerInfo")]
	public async Task<GetPlayerInfoResult> GetPlayerInfoAsync(GetPlayerInfoRequest request)
		=> await mediator.Send(BuildRequest(request)).ConfigureAwait(false);

	[HttpPost("PlaceBetAndSettle")]
	public async Task<PlaceBetAndSettleResult> PlaceBetAndSettleAsync(PlaceBetAndSettleRequest request)
		=> await mediator.Send(BuildRequest(request)).ConfigureAwait(false);

	[HttpPost("FreeSpin")]
	public async Task<FreeSpinResult> FreeSpinAsync(FreeSpinRequest request)
		=> await mediator.Send(BuildRequest(request)).ConfigureAwait(false);

	[HttpPost("VoidGame")]
	public async Task<VoidGameResult> VoidGameAsync(VoidGameRequest request)
		=> await mediator.Send(BuildRequest(request)).ConfigureAwait(false);

	[HttpPost("Bonus")]
	public async Task<BonusResult> BonusAsync(BonusRequest request)
		=> await mediator.Send(BuildRequest(request)).ConfigureAwait(false);

	private static TRequest BuildRequest<TRequest>(TRequest request) where TRequest : GameProviderRequestWithCode
	{
		request.GameProviderCode = "SABAPLUS-JILI";

		return request;
	}
}