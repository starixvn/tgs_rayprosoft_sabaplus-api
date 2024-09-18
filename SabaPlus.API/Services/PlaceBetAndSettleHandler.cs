using AutoMapper;
using MediatR;
using SabaPlus.API.Messages;
using SabaPlus.API.Models;
using SabaPlus.API.Models.PlaceBetAndSettle;
using SabaPlus.Common;
using SabaPlus.Core;
using SabaPlus.Repositories.Helpers;
using SabaPlus.Repositories.Models;
using Tgs.CashService;
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
			BalanceResponse balanceResponse = null;
			//TODO must calculate adjust amount, if > 0 then add, < 0 then deduct
			var playerCode = request.Payload.OperatorCustId.ToPlayerCode();

			var bets = request.Payload.BetDataSet;

			var betAmount = bets?.Sum(bet => bet.ActualStake);
			var payout = bets?.Sum(bet => bet.Payout);
			var adjustAmount = (payout - betAmount) ?? 0;

			var refId = string.Join('@', bets.Select(bet => bet.TicketNo));

			//TODO deduct
			if (adjustAmount < 0)
			{
				balanceResponse = await cashServiceClient.GameProviderDeductBalanceAsync(
				playerCode,
				adjustAmount,
				new ProviderInfo(request.Payload.GameProviderCode, refId, request.Seq, ActionConstants.PlaceBetAndSettlePlaceBet),
				string.Empty,
				cancellationToken);

				if (!ErrorMapper.IsSuccess(balanceResponse.ErrorCode))
				{
					return GenerateFailResponse(balanceResponse.ErrorCode, balanceResponse.ErrorMessage);
				}
			}

			var criteriaList = GetPlaceBetAndSettleCriterias(request);

			//TODO only insert bettrans_bet
			var spResults = await InsertTransactionsAsync(criteriaList);
			var failedSp = spResults.FirstOrDefault(spResult => !ErrorMapper.IsSuccess(spResult.StatusCode));

			if (failedSp is not null)
			{
				if (adjustAmount > 0)
				{
					await RefundAsync(request, cancellationToken);
				}
			}

			//TODO add balance
			if (adjustAmount > 0)
			{
				balanceResponse = await cashServiceClient.GameProviderAddBalanceAsync(
				playerCode,
				adjustAmount,
				new ProviderInfo(request.Payload.GameProviderCode, refId, request.Seq, ActionConstants.PlaceBetAndSettlePlaceBet),
				string.Empty,
				cancellationToken);

				if (!ErrorMapper.IsSuccess(balanceResponse.ErrorCode))
				{
					return GenerateFailResponse(balanceResponse.ErrorCode, balanceResponse.ErrorMessage);
				}
			}

			if (adjustAmount == 0)
			{
				balanceResponse = await cashServiceClient.GameProviderGetBalanceAsync(playerCode, cancellationToken).ConfigureAwait(false);
			}

			var balance = balanceResponse?.Balance ?? 0;

			var res = new PlaceBetAndSettleResult();

			//TODO reject all or accept partial?!?
			//First, reject all or accept all

			var dataSets = spResults.Select((spResult, index) =>
							new PlaceBetAndSettleResultDataSet { RefNo = spResult.RefNo.ToString(), TicketNo = request.Payload.BetDataSet[index].TicketNo })
							.ToArray();
			res.Payload = new PlaceBetAndSettlePayload(request.Payload.OperatorCustId, balance, dataSets);

			//TODO insert rounds...
			await PublishBetMessage(criteriaList, spResults, cancellationToken);

			return res;
		}
		catch (Exception ex)
		{
			_ = sentryClient.CaptureException(ex);
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
		_ = sentryClient.CaptureException(new Exception($"{errorCode} - {errorMsg}"));
		return new PlaceBetAndSettleResult { ErrorCode = ErrorMapper.GetErrorCode(errorCode) };
	}
}