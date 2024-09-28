using MediatR;
using SabaPlus.API.Models.BetResultCheck;
using SabaPlus.API.Models.EndGame;
using SabaPlus.API.Models.VoidGame;
using SabaPlus.Common;
using SabaPlus.Core;
using SabaPlus.Core.Models;
using Tgs.GameProviders.Client;

namespace SabaPlus.API.Services;

public class BetResultCheckHandler(
	IGameProvidersClient gameProvidersClient,
	IGameProviderRepository gameProviderRepository,
	IMediator mediator)
	: IRequestHandler<BetResultCheckScheduleRequest, BetResultCheckResult>
{
	public async Task<BetResultCheckResult> Handle(BetResultCheckScheduleRequest request, CancellationToken cancellationToken)
	{
		try
		{
			var tickets = await gameProviderRepository.GetRunningTicketsAsync(request.DiffInMinute).ConfigureAwait(false);

			foreach (var ticket in tickets)
			{
				await HandleEachTicketAsync(ticket);
			}
		}
		catch (Exception ex)
		{
			_ = SentrySdk.CaptureException(ex);
		}

		return new BetResultCheckResult();
	}

	public async Task HandleEachTicketAsync(BetResultCheckTicket ticket)
	{
		try
		{
			var result = await gameProvidersClient.BetResultCheck<BetResultCheckFromProvider>(
				ticket.GameProviderCode,
				ticket.ExternalGameId,
				ticket.TicketNo,
				ticket.PlayerCode)
				.ConfigureAwait(false);

			switch (result.Result.Status)
			{
				case 1:
				case 2:
				case 3:
					await HandleEndGameAsync(result, ticket);
					break;

				case 4:
				case 5:
				case 6:
					await HandleVoidAsync(result, ticket);
					break;
			}
		}
		catch (Exception ex)
		{
			_ = SentrySdk.CaptureException(ex);
		}
	}

	public async Task HandleEndGameAsync(BetResultCheckFromProvider data, BetResultCheckTicket ticket)
	{
		EndGameDataSet[] dataSet = [new EndGameDataSet
		{
			TicketNo = data.Result.TicketNo,
			GameId = ticket.ExternalGameId,
			GameRoundId = data.Result.GameRoundId,
			TransactionId = data.Result.TransactionId,
			ActualStake = data.Result.ActualStake,
			NetStake = data.Result.NetStake,
			Payout = data.Result.Payout,
			Odds = data.Result.Odds,
			GameResult = data.Result.GameResult,
			SettleDate = data.Result.SettleDate,
			OtherBetInfo = data.Result.OtherBetInfo,
		}];
		var payload = new EndGameRequestPayload(data.Result.OperatorCustId, data.Query.Configuration.GameType, dataSet)
		{
			Action = ActionConstants.BetResultCheck
		};
		var request = new EndGameRequest
		{
			Payload = payload,
			Seq = Guid.NewGuid().ToString(),
			TimeStamp = DateTime.Now.ToString(),
		};

		_ = await mediator.Send(request);
	}

	public async Task HandleVoidAsync(BetResultCheckFromProvider data, BetResultCheckTicket ticket)
	{
		VoidGameRequestDataSet[] dataSet = [new VoidGameRequestDataSet
		{
			TicketNo = data.Result.TicketNo,
			GameId = ticket.ExternalGameId,
			GameRoundId = data.Result.GameRoundId,
			TransactionId = data.Result.TransactionId,
		}];
		var payload = new VoidGameRequestPayload(data.Result.OperatorCustId, data.Query.Configuration.GameType, dataSet)
		{
			Action = ActionConstants.BetResultCheck
		};
		var request = new VoidGameRequest
		{
			Payload = payload,
			Seq = Guid.NewGuid().ToString(),
			TimeStamp = DateTime.Now.ToString(),
		};

		_ = await mediator.Send(request);
	}
}