using AutoMapper;
using SabaPlus.API.Messages;
using SabaPlus.API.Models;
using SabaPlus.API.Models.PlaceBet;
using SabaPlus.Common;
using SabaPlus.Core;
using SabaPlus.Repositories.Helpers;
using SabaPlus.Repositories.Models;
using Tgs.CashService;
using Tgs.CashService.Client;
using Tgs.ServiceUtility;

namespace SabaPlus.API.Services;

public class PlaceBetGetService(
	ISabaPlusApiMessagePublishHandler messagePublisher,
	IGameProviderRepository gameProviderRepository,
	ICashServiceClient cashClient,
	IMapper mapper
	) : IGetService<PlaceBetRequest, PlaceBetResult>
{
	public async ValueTask<PlaceBetResult> GetAsync(PlaceBetRequest request, CancellationToken cancellationToken = default)
	{
		try
		{
			var playerCode = request.Payload.OperatorCustId;
			var betAmount = request.Payload.BetDataSet.Sum(betData => betData.ActualStake)!.Value;
			var refId = string.Join('@', request.Payload.BetDataSet.Select(betData => betData.TicketNo));
			var seq = request.Seq;

			var balanceResult = await DeductBalanceAsync(request, cancellationToken);
			if (!ErrorMapper.IsSuccess(balanceResult.ErrorCode))
			{
				return await GenerateResponseAsync(request, GenerateFailResponse(balanceResult.ErrorCode), cancellationToken);
			}

			var spResults = await InsertTransactionsAsync(request, cancellationToken);
			var failedSp = spResults.Find(spResult => !ErrorMapper.IsSuccess(spResult.StatusCode!));
			if (failedSp != default)
			{
				await RefundBalanceAsync(request, cancellationToken);
				return await GenerateResponseAsync(request, GenerateFailResponse(failedSp.StatusCode!), cancellationToken);
			}

			var betDataSet = spResults.Select((spResult, index) =>
								new PlaceBetResultDataSet { RefNo = spResult.RefNo.ToString(), TicketNo = request.Payload.BetDataSet[index].TicketNo });
			var res = new PlaceBetResult
			{
				Payload = new PlaceBetResponsePayload(playerCode, balanceResult.Balance, betDataSet.ToArray())
			};
			return await GenerateResponseAsync(request, res, cancellationToken);
		}
		catch (Exception ex)
		{
			SentrySdk.CaptureException(ex);
			return await GenerateResponseAsync(request, new PlaceBetResult { ErrorCode = ErrorMapper.UnspecifiedError }, cancellationToken);
		}
	}

	private async ValueTask<BalanceResponse> DeductBalanceAsync(PlaceBetRequest request, CancellationToken cancellationToken)
	{
		var betAmount = request.Payload.BetDataSet.Sum(betData => betData.ActualStake)!.Value;
		var refId = string.Join('@', request.Payload.BetDataSet.Select(betData => betData.TicketNo));
		return await cashClient.GameProviderDeductBalanceAsync(
			request.Payload.OperatorCustId!,
			betAmount,
			new ProviderInfo(request.Payload.ProviderCode!, refId, request.Seq, ActionConstants.PlaceBet),
			string.Empty,
			cancellationToken
			).ConfigureAwait(false);
	}

	private async Task<List<SPResult>> InsertTransactionsAsync(PlaceBetRequest request, CancellationToken cancellationToken)
	{
		var criteriaList = request.Payload.BetDataSet.Select(betData =>
		{
			var criteria = mapper.Map<PlaceBetCriteria>(betData!);
			criteria.Seq = request.Seq;
			criteria.OperatorCustId = request.Payload.OperatorCustId;
			criteria.GameType = request.Payload.GameType;
			criteria.IpAddress = request.Payload.IpAddress;
			criteria.GameProviderCode = request.Payload.ProviderCode;
			criteria.TransDate = DateTime.Parse(request.TimeStamp).ToUniversalTime();
			return criteria;
		});
		var result = new List<SPResult>();
		foreach (var criteria in criteriaList)
		{
			var spResult = await gameProviderRepository.CallStoredProcedureAsync<PlaceBetCriteria, SPResult>(criteria, StoreProcedureMapper.BetInsertSP);
			result.Add(spResult);
		}
		return result;
	}

	private async ValueTask RefundBalanceAsync(PlaceBetRequest request, CancellationToken cancellationToken)
	{
		var betAmount = request.Payload.BetDataSet.Sum(betData => betData.ActualStake)!.Value;
		var refId = string.Join('@', request.Payload.BetDataSet.Select(betData => betData.TicketNo));
		await cashClient.GameProviderAddBalanceAsync(
			request.Payload.OperatorCustId!,
			betAmount,
			new ProviderInfo(request.Payload.ProviderCode!, refId, request.Seq, ActionConstants.PlaceBetFail),
			string.Empty,
			cancellationToken
			).ConfigureAwait(false);
	}

	private PlaceBetResult GenerateFailResponse(string errorCode)
	{
		return new PlaceBetResult { ErrorCode = ErrorMapper.GetErrorCode(errorCode) };
	}

	private async ValueTask<PlaceBetResult> GenerateResponseAsync(PlaceBetRequest request, PlaceBetResult result, CancellationToken cancellationToken)
	{
		// TODO: insert into bet.rounds
		var apiLogMessage = new ApiLogMessage()
		{
			Action = ActionConstants.PlaceBet,
			Request = request,
			Response = result
		};
		await messagePublisher.LogApi(apiLogMessage, cancellationToken);

		return result;
	}
}