using AutoMapper;
using MediatR;
using SabaPlus.API.Messages;
using SabaPlus.API.Models;
using SabaPlus.API.Models.EndGame;
using SabaPlus.Common;
using SabaPlus.Core;
using SabaPlus.Repositories.Helpers;
using SabaPlus.Repositories.Models;
using Tgs.CashService.Client;

namespace SabaPlus.API.Services;

public class EndGameHandler(
	ISabaPlusApiMessagePublishHandler messagePublisher,
	ICashServiceClient cashClient,
	IGameProviderRepository gameProviderRepository,
	IMapper mapper
	) : IRequestHandler<EndGameRequest, EndGameResult>
{
	public async Task<EndGameResult> Handle(EndGameRequest request, CancellationToken cancellationToken = default)
	{
		try
		{
			var gameProviderCode = await gameProviderRepository.GetGameProviderCodeByGameIdAsync(request.Payload.BetDataSet?.FirstOrDefault()?.GameId!);

			var criteriaList = GetEndGameCriteriasAsync(request, gameProviderCode);
			var spResults = await SettleTransactionsAsync(criteriaList);
			var failedSp = spResults.Find(spResult => !ErrorMapper.IsSuccess(spResult.StatusCode!));

			if (failedSp != default)
			{
				return await GenerateResponseAsync(request, GenerateFailResponse(failedSp.StatusCode!), cancellationToken);
			}

			var balanceResult = await AddBalanceAsync(request, gameProviderCode, cancellationToken);
			if (!ErrorMapper.IsSuccess(balanceResult.ErrorCode))
			{
				return await GenerateResponseAsync(request, GenerateFailResponse(balanceResult.ErrorCode), cancellationToken);
			}

			var res = new EndGameResult
			{
				Payload = new EndGameResponsePayload(request.Payload.OperatorCustId, balanceResult.Balance)
			};
			await PublishMessageAsync(criteriaList, cancellationToken);
			return await GenerateResponseAsync(request, res, cancellationToken);
		}
		catch (Exception ex)
		{
			SentrySdk.CaptureException(ex);
			return await GenerateResponseAsync(request, new EndGameResult { ErrorCode = ErrorMapper.UnspecifiedError }, cancellationToken);
		}
	}

	private async ValueTask<List<SPResult>> SettleTransactionsAsync(IEnumerable<EndGameCriteria> criteriaList)
	{
		var result = new List<SPResult>();
		foreach (var criteria in criteriaList)
		{
			var spResult = await gameProviderRepository.CallStoredProcedureAsync<EndGameCriteria, SPResult>(criteria, StoreProcedureMapper.InsertSignalSP);
			result.Add(spResult);
		}
		return result;
	}

	private IEnumerable<EndGameCriteria> GetEndGameCriteriasAsync(EndGameRequest request, string gameProviderCode)
	{
		return request.Payload.BetDataSet.Select(betData =>
		{
			var criteria = mapper.Map<EndGameCriteria>(betData!);
			criteria.Seq = request.Seq;
			criteria.OperatorCustId = request.Payload.OperatorCustId;
			criteria.GameType = request.Payload.GameType;
			criteria.GameProviderCode = gameProviderCode;
			return criteria;
		});
	}

	private async ValueTask<BalanceResponse> AddBalanceAsync(EndGameRequest request, string gameProviderCode, CancellationToken cancellationToken)
	{
		var amount = request.Payload.BetDataSet?.Sum(data => data.Payout);
		var refId = string.Join('@', request.Payload.BetDataSet.Select(betData => betData.TicketNo));

		if (amount > 0)
		{
			return await cashClient.GameProviderAddBalanceAsync(
				request.Payload.OperatorCustId!,
				amount!.Value,
				new Tgs.CashService.ProviderInfo(gameProviderCode!, refId, request.Seq, ActionConstants.EndGame),
				string.Empty,
				cancellationToken);
		}
		return await cashClient.GameProviderGetBalanceAsync(request.Payload.OperatorCustId!, cancellationToken);
	}

	private async ValueTask PublishMessageAsync(IEnumerable<EndGameCriteria> criteriaList, CancellationToken cancellationToken)
	{
		foreach (var criteria in criteriaList)
		{
			await messagePublisher.HandleBetAsync(HandleMessageMapper.EndGame(criteria), cancellationToken);
		}
	}

	private EndGameResult GenerateFailResponse(string errorCode)
	{
		return new EndGameResult { ErrorCode = ErrorMapper.GetErrorCode(errorCode) };
	}

	private async ValueTask<EndGameResult> GenerateResponseAsync(EndGameRequest request, EndGameResult result, CancellationToken cancellationToken)
	{
		var apiLogMessage = new ApiLogMessage()
		{
			Action = ActionConstants.EndGame,
			Request = request,
			Response = result
		};
		await messagePublisher.LogApi(apiLogMessage, cancellationToken);

		return result;
	}
}