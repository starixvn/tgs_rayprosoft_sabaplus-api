using AutoMapper;
using MediatR;
using SabaPlus.API.Messages;
using SabaPlus.API.Models;
using SabaPlus.API.Models.PlaceBetAndSettle;
using SabaPlus.Common;
using SabaPlus.Core;
using SabaPlus.Repositories.Helpers;
using SabaPlus.Repositories.Models;
using Sentry;
using Tgs.CashService.Client;

namespace SabaPlus.API.Services;

public class PlaceBetAndSettleHandler(
	ISabaPlusApiMessagePublishHandler messagePublisher,
	ICashServiceClient cashServiceClient,
	IGameProviderRepository gameProviderRepository,
	IMapper mapper,
	ISentryClient sentryClient
	) : IRequestHandler<PlaceBetAndSettleRequest, PlaceBetAndSettleResult>
{
	public async Task<PlaceBetAndSettleResult> Handle(PlaceBetAndSettleRequest request, CancellationToken cancellationToken = default)
	{
		try
		{
			var deductBalanceRes = await DeductBalanceAsync(request, cancellationToken).ConfigureAwait(false);
			if (!ErrorMapper.IsSuccess(deductBalanceRes.ErrorCode))
			{
				return GenerateFailResponse(deductBalanceRes.ErrorCode, deductBalanceRes.ErrorMessage);
			}

			var criteriaList = GetPlaceBetAndSettleCriterias(request);
			var spResults = await InsertTransactionsAsync(criteriaList);
			var failedSp = spResults.Find(spResult => !ErrorMapper.IsSuccess(spResult.StatusCode));
			if (failedSp != default)
			{
				await RefundAsync(request, cancellationToken);
				return GenerateFailResponse(failedSp.StatusCode, failedSp.ErrMsg);
			}

			var settleRes = await SettleAsync(request, cancellationToken);
			var res = new PlaceBetAndSettleResult();
			if (settleRes == null || ErrorMapper.IsSuccess(settleRes?.ErrorCode))
			{
				var dataSets = spResults.Select((spResult, index) =>
								new PlaceBetAndSettleResultDataSet { RefNo = spResult.RefNo.ToString(), TicketNo = request.Payload.BetDataSet[index].TicketNo })
								.ToArray();
				res.Payload = new PlaceBetAndSettlePayload(request.Payload.OperatorCustId, settleRes?.Balance ?? deductBalanceRes.Balance, dataSets);
			}
			else
			{
				res.ErrorCode = ErrorMapper.GetErrorCode(settleRes?.ErrorCode);
			}
			await PublishBetMessage(criteriaList, spResults, cancellationToken);
			return res;
		}
		catch (Exception ex)
		{
			sentryClient.CaptureException(ex);
			return new PlaceBetAndSettleResult { ErrorCode = ErrorMapper.UnspecifiedError };
		}
	}

	private async ValueTask<List<SPResult>> InsertTransactionsAsync(IEnumerable<PlaceBetAndSettleCriteria> criteriaList)
	{
		var result = new List<SPResult>();
		foreach (var criteria in criteriaList)
		{
			var spResult = await gameProviderRepository.CallStoredProcedureAsync<PlaceBetAndSettleCriteria, SPResult>(criteria, StoreProcedureMapper.InsertSignalSP);
			result.Add(spResult);
		}
		return result;
	}

	private IEnumerable<PlaceBetAndSettleCriteria> GetPlaceBetAndSettleCriterias(PlaceBetAndSettleRequest request)
	{
		return request.Payload.BetDataSet.Select(betData =>
		{
			var criteria = mapper.Map<PlaceBetAndSettleCriteria>(betData!);
			criteria.TransDate = DateTime.Parse(request.TimeStamp).ToUniversalTime();
			criteria.Seq = request.Seq;
			criteria.OperatorCustId = request.Payload.OperatorCustId;
			criteria.GameType = request.Payload.GameType;
			criteria.IpAddress = request.Payload.IpAddress;
			criteria.GameProviderCode = request.Payload.GameProviderCode;
			return criteria;
		});
	}

	private async ValueTask<BalanceResponse> DeductBalanceAsync(PlaceBetAndSettleRequest request, CancellationToken cancellationToken)
	{
		var betAmount = request.Payload.BetDataSet.Sum(bet => bet.ActualStake);
		var refId = string.Join('@', request.Payload.BetDataSet.Select(bet => bet.TicketNo));
		return await cashServiceClient.GameProviderDeductBalanceAsync(
			request.Payload.OperatorCustId.ToPlayerCode(),
			betAmount,
			new Tgs.CashService.ProviderInfo(request.Payload.GameProviderCode, refId, request.Seq, ActionConstants.PlaceBetAndSettlePlaceBet),
			string.Empty,
			cancellationToken);
	}

	private async ValueTask<BalanceResponse?> SettleAsync(PlaceBetAndSettleRequest request, CancellationToken cancellationToken)
	{
		var amount = request.Payload.BetDataSet.Sum(bet => bet.Payout);
		return amount > 0 ? await AddBalanceAsync(amount, ActionConstants.PlaceBetAndSettleSettle, request, cancellationToken) : null;
	}

	private async ValueTask RefundAsync(PlaceBetAndSettleRequest request, CancellationToken cancellationToken)
	{
		var amount = request.Payload.BetDataSet.Sum(bet => bet.ActualStake);
		await AddBalanceAsync(amount, ActionConstants.PlaceBetAndSettleFail, request, cancellationToken);
	}

	private async ValueTask<BalanceResponse> AddBalanceAsync(decimal amount, string action, PlaceBetAndSettleRequest request, CancellationToken cancellationToken)
	{
		var refId = string.Join('@', request.Payload.BetDataSet.Select(bet => bet.TicketNo));
		return await cashServiceClient.GameProviderAddBalanceAsync(
			request.Payload.OperatorCustId.ToPlayerCode(),
			amount,
			new Tgs.CashService.ProviderInfo(request.Payload.GameProviderCode, refId, request.Seq, action),
			string.Empty,
			cancellationToken);
	}

	private async ValueTask PublishBetMessage(IEnumerable<PlaceBetAndSettleCriteria> criteriaList, List<SPResult> spResults, CancellationToken cancellationToken)
	{
		foreach (var item in criteriaList.Select((criteria, i) => (criteria, i)))
		{
			await messagePublisher.HandleBetAsync(HandleMessageMapper.PlaceBetAndSettle(item.criteria, spResults[item.i].RefNo!.Value), cancellationToken);
		}
	}

	private PlaceBetAndSettleResult GenerateFailResponse(string errorCode, string errorMsg)
	{
		sentryClient.CaptureException(new Exception($"{errorCode} - {errorMsg}"));
		return new PlaceBetAndSettleResult { ErrorCode = ErrorMapper.GetErrorCode(errorCode) };
	}
}