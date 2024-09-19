using AutoMapper;
using MediatR;
using SabaPlus.API.Messages;
using SabaPlus.API.Models;
using SabaPlus.API.Models.PlaceBetAndSettle;
using SabaPlus.API.Services.Shared;
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
			BalanceResponse? balanceResponse = null;
			//TODO must calculate adjust amount, if > 0 then add, < 0 then deduct
			var playerCode = request.Payload.OperatorCustId.ToPlayerCode();

			var bets = request.Payload.BetDataSet;

			if (bets?.Length == 0)
			{
				return ResultHelper.GenerateFailedResult<PlaceBetAndSettleResult, PlaceBetAndSettlePayload>(
					ErrorMapper.ParameterIncorrect.ToString(),
					ErrorMapper.GetErrorMessage(ErrorMapper.ParameterIncorrect));
			}

			//TODO process on one bet
			var bet = bets.First();

			var betAmount = bet.ActualStake;
			var payout = bet.Payout;
			var adjustAmount = payout - betAmount;

			var refId = bet.TicketNo;

			//TODO deduct
			if (adjustAmount < 0)
			{
				balanceResponse = await cashServiceClient.GameProviderDeductBalanceAsync(
				playerCode,
				Math.Abs(adjustAmount),
				new ProviderInfo(request.Payload.GameProviderCode, refId, request.Seq, ActionConstants.PlaceBetAndSettlePlaceBet),
				string.Empty,
				cancellationToken);

				if (!ErrorMapper.IsSuccess(balanceResponse.ErrorCode))
				{
					return ResultHelper.GenerateFailedResult<PlaceBetAndSettleResult, PlaceBetAndSettlePayload>(balanceResponse.ErrorCode, balanceResponse.ErrorMessage);
				}
			}

			var criteria = ConvertToCriteria(request.TimeStamp, request.Seq, request.Payload, bet);
			var spResult = await gameProviderRepository.CallStoredProcedureAsync<PlaceBetAndSettleCriteria, SPResult>(
				criteria,
				StoreProcedureMapper.InsertSignalSP);

			var isFailedSp = !ErrorMapper.IsSuccess(spResult.StatusCode);

			if (IsAddBalance(adjustAmount, isFailedSp))
			{
				var action = isFailedSp ? ActionConstants.PlaceBetAndSettleFail : ActionConstants.PlaceBetAndSettlePlaceBet;

				balanceResponse = await cashServiceClient.GameProviderAddBalanceAsync(
				playerCode,
				Math.Abs(adjustAmount),
				new ProviderInfo(request.Payload.GameProviderCode, refId, request.Seq, action),
				string.Empty,
				cancellationToken);

				if (!ErrorMapper.IsSuccess(balanceResponse.ErrorCode))
				{
					return ResultHelper.GenerateFailedResult<PlaceBetAndSettleResult, PlaceBetAndSettlePayload>(balanceResponse.ErrorCode, balanceResponse.ErrorMessage);
				}
			}

			if (adjustAmount == 0)
			{
				balanceResponse = await cashServiceClient.GameProviderGetBalanceAsync(playerCode, cancellationToken).ConfigureAwait(false);
			}

			var balance = balanceResponse?.Balance ?? 0;

			if (isFailedSp)
			{
				return ResultHelper.GenerateFailedResult<PlaceBetAndSettleResult, PlaceBetAndSettlePayload>(spResult.StatusCode, spResult.ErrMsg);
			}
			var refNo = spResult.RefNo.ToString();
			var dataSets = new PlaceBetAndSettleResultDataSet { RefNo = refNo, TicketNo = bet.TicketNo };
			var res = new PlaceBetAndSettleResult
			{
				Payload = new PlaceBetAndSettlePayload(request.Payload.OperatorCustId, balance, [dataSets])
			};

			await messagePublisher.HandleBetAsync(HandleMessageMapper.PlaceBetAndSettle(criteria, spResult.RefNo ?? 0), cancellationToken);

			return res;
		}
		catch (Exception ex)
		{
			_ = sentryClient.CaptureException(ex);
			return new PlaceBetAndSettleResult { ErrorCode = ErrorMapper.UnspecifiedError };
		}
	}

	private static bool IsAddBalance(decimal adjustAmount, bool failedSp)
		=> failedSp && adjustAmount > 0 || adjustAmount < 0;

	private PlaceBetAndSettleCriteria ConvertToCriteria(
		string TimeStamp,
		string Seq,
		PlaceBetAndSettleRequestPayload payload,
		PlaceBetAndSettleDataSet betData)
	{
		var criteria = mapper.Map<PlaceBetAndSettleCriteria>(betData!);
		criteria.TransDate = DateTime.Parse(TimeStamp).ToUniversalTime();
		criteria.Seq = Seq;
		criteria.OperatorCustId = payload.OperatorCustId;
		criteria.GameType = payload.GameType;
		criteria.IpAddress = payload.IpAddress;
		criteria.GameProviderCode = payload.GameProviderCode;

		return criteria;
	}
}