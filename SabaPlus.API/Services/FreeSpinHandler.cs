using AutoMapper;
using MediatR;
using SabaPlus.API.Messages;
using SabaPlus.API.Models;
using SabaPlus.API.Models.FreeSpin;
using SabaPlus.Common;
using SabaPlus.Core;
using SabaPlus.Repositories.Helpers;
using SabaPlus.Repositories.Models;
using Tgs.CashService.Client;

namespace SabaPlus.API.Services;

public class FreeSpinHandler(
	ISabaPlusApiMessagePublishHandler messagePublisher,
	ICashServiceClient cashClient,
	IGameProviderRepository gameProviderRepository,
	IMapper mapper
	) : IRequestHandler<FreeSpinRequest, FreeSpinResult>
{
	public async Task<FreeSpinResult> Handle(FreeSpinRequest request, CancellationToken cancellationToken = default)
	{
		try
		{
			var gameProviderCode = await gameProviderRepository.GetGameProviderCodeByGameTypeAsync(request.Payload.GameType);

			var criteriaList = GetFreeSpinCriterias(request, gameProviderCode);
			var spResults = await InsertTransactionsAsync(criteriaList);
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

			var betDataSet = spResults.Select((spResult, index) =>
								new FreeSpinResponseDataSet { RefNo = spResult.RefNo.ToString(), TicketNo = request.Payload.BetDataSet[index].TicketNo });
			var res = new FreeSpinResult
			{
				Payload = new FreeSpinPayload(request.Payload.OperatorCustId, balanceResult.Balance, betDataSet.ToArray())
			};

			await PublishBetMessageAsync(criteriaList, spResults, cancellationToken);
			return await GenerateResponseAsync(request, res, cancellationToken);
		}
		catch (Exception ex)
		{
			SentrySdk.CaptureException(ex);
			return await GenerateResponseAsync(request, new FreeSpinResult { ErrorCode = ErrorMapper.UnspecifiedError }, cancellationToken);
		}
	}

	private async Task<List<SPResult>> InsertTransactionsAsync(IEnumerable<FreeSpinCriteria> criteriaList)
	{
		var result = new List<SPResult>();
		foreach (var criteria in criteriaList)
		{
			var spResult = await gameProviderRepository.CallStoredProcedureAsync<FreeSpinCriteria, SPResult>(criteria, StoreProcedureMapper.InsertSignalSP);
			result.Add(spResult);
		}
		return result;
	}

	private IEnumerable<FreeSpinCriteria> GetFreeSpinCriterias(FreeSpinRequest request, string gameProviderCode)
	{
		return request.Payload.BetDataSet.Select(betData =>
		{
			var criteria = mapper.Map<FreeSpinCriteria>(betData!);
			criteria.TransDate = DateTime.Parse(request.TimeStamp).ToUniversalTime();
			criteria.Seq = request.Seq;
			criteria.OperatorCustId = request.Payload.OperatorCustId;
			criteria.GameType = request.Payload.GameType;
			criteria.IpAddress = request.Payload.IpAddress;
			criteria.GameProviderCode = gameProviderCode;
			//criteria.GameId = request.Payload.S1GameId;
			return criteria;
		});
	}

	private async ValueTask<BalanceResponse> AddBalanceAsync(FreeSpinRequest request, string gameProviderCode, CancellationToken cancellationToken)
	{
		var amount = request.Payload.BetDataSet.Sum(data => data.Payout);
		var refId = string.Join('@', request.Payload.BetDataSet.Select(data => data.TicketNo));
		if (amount > 0)
		{
			return await cashClient.GameProviderAddBalanceAsync(
				request.Payload.OperatorCustId!,
				amount,
				new Tgs.CashService.ProviderInfo(gameProviderCode!, refId, request.Seq, ActionConstants.EndGame),
				string.Empty,
				cancellationToken);
		}
		return await cashClient.GameProviderGetBalanceAsync(request.Payload.OperatorCustId!, cancellationToken);
	}

	private async ValueTask PublishBetMessageAsync(IEnumerable<FreeSpinCriteria> criteriaList, List<SPResult> spResults, CancellationToken cancellationToken)
	{
		foreach (var item in criteriaList.Select((criteria, i) => (criteria, i)))
		{
			await messagePublisher.HandleBetAsync(HandleMessageMapper.FreeSpin(item.criteria, spResults[item.i].RefNo!.Value), cancellationToken);
		}
	}

	private FreeSpinResult GenerateFailResponse(string errorCode)
	{
		return new FreeSpinResult { ErrorCode = ErrorMapper.GetErrorCode(errorCode) };
	}

	private async ValueTask<FreeSpinResult> GenerateResponseAsync(FreeSpinRequest request, FreeSpinResult result, CancellationToken cancellationToken)
	{
		var apiLogMessage = new ApiLogMessage()
		{
			Action = ActionConstants.FreeSpin,
			Request = request,
			Response = result
		};
		await messagePublisher.LogApi(apiLogMessage, cancellationToken);

		return result;
	}
}