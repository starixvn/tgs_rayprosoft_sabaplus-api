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
	IMapper mapper,
	ISentryClient sentryClient
	) : IRequestHandler<EndGameRequest, EndGameResult>
{
	public async Task<EndGameResult> Handle(EndGameRequest request, CancellationToken cancellationToken = default)
	{
		try
		{
			var gameProviderCode = await gameProviderRepository.GetGameProviderCodeByGameTypeAsync(request.Payload.GameType!);

			var criteriaList = GetEndGameCriteriasAsync(request, gameProviderCode);
			var spResults = await SettleTransactionsAsync(criteriaList);
			var failedSp = spResults.Find(spResult => !ErrorMapper.IsSuccess(spResult.StatusCode!));

			if (failedSp != default)
			{
				return GenerateFailResponse(failedSp.StatusCode!, failedSp.ErrMsg);
			}

			var balanceResult = await AddBalanceAsync(request, gameProviderCode, cancellationToken);
			if (!ErrorMapper.IsSuccess(balanceResult.ErrorCode))
			{
				return GenerateFailResponse(balanceResult.ErrorCode, balanceResult.ErrorCode);
			}

			var res = new EndGameResult
			{
				Payload = new EndGameResponsePayload(request.Payload.OperatorCustId, balanceResult.Balance)
			};
			await PublishMessageAsync(criteriaList, cancellationToken);
			return res;
		}
		catch (Exception ex)
		{
			sentryClient.CaptureException(ex);
			return new EndGameResult { ErrorCode = ErrorMapper.UnspecifiedError };
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
				request.Payload.OperatorCustId!.ToPlayerCode(),
				amount!.Value,
				new Tgs.CashService.ProviderInfo(gameProviderCode!, refId, request.Seq, ActionConstants.EndGame),
				string.Empty,
				cancellationToken);
		}
		return await cashClient.GameProviderGetBalanceAsync(request.Payload.OperatorCustId!.ToPlayerCode(), cancellationToken);
	}

	private async ValueTask PublishMessageAsync(IEnumerable<EndGameCriteria> criteriaList, CancellationToken cancellationToken)
	{
		foreach (var criteria in criteriaList)
		{
			await messagePublisher.HandleBetAsync(HandleMessageMapper.EndGame(criteria), cancellationToken);
		}
	}

	private EndGameResult GenerateFailResponse(string errorCode, string errorMsg)
	{
		sentryClient.CaptureException(new Exception($"{errorCode} - {errorMsg}"));
		return new EndGameResult { ErrorCode = ErrorMapper.GetErrorCode(errorCode) };
	}
}