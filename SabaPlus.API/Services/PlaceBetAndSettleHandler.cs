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
	IPlayerRepository playerRepository,
	IMapper mapper,
	ISentryClient sentryClient
	) : IRequestHandler<PlaceBetAndSettleRequest, PlaceBetAndSettleResult>
{
	public async Task<PlaceBetAndSettleResult> Handle(PlaceBetAndSettleRequest request, CancellationToken cancellationToken = default)
	{
		try
		{
			if (string.IsNullOrEmpty(request.Payload.WalletSideToken))
			{
				request.Payload.GameProviderCode = await gameProviderRepository.GetGameProviderCodeByGameTypeAsync(request.Payload.GameType);
			}

			var betErrorCode = await IsValidBetAsync(request);
			if (betErrorCode != ErrorMapper.Success)
			{
				return ResultHelper.GenerateFailedResult<PlaceBetAndSettleResult, PlaceBetAndSettlePayload>(
					betErrorCode,
					ErrorMapper.GetErrorMessage(betErrorCode));
			}
			var playerCode = request.Payload.OperatorCustId.ToPlayerCode();
			var bet = request.Payload.BetDataSet.First();

			var balanceResponse = await cashServiceClient.GameProviderGetBalanceAsync(playerCode, cancellationToken).ConfigureAwait(false);
			var balance = balanceResponse?.Balance ?? 0;

			if (balance < bet.ActualStake)
			{
				return ResultHelper.GenerateFailedResult<PlaceBetAndSettleResult, PlaceBetAndSettlePayload>(
					ErrorMapper.InsufficientFunds,
					ErrorMapper.GetErrorMessage(ErrorMapper.InsufficientFunds));
			}

			return await ProcessBetAsync(request, bet, balance, cancellationToken).ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			_ = sentryClient.CaptureException(ex);
			return new PlaceBetAndSettleResult { ErrorCode = ErrorMapper.UnspecifiedError };
		}
	}

	private async ValueTask<int> IsValidBetAsync(PlaceBetAndSettleRequest request)
	{
		var bets = request.Payload.BetDataSet;
		if (request.Payload.BetDataSet.Length == 0)
		{
			return ErrorMapper.ParameterIncorrect;
		}
		var bet = bets.First();

		if (IsFreeBet(bet))
		{
			return ErrorMapper.Success;
		}

		var gameProviderCode = request.Payload.GameProviderCode;
		var playerInfo = await playerRepository.GetPlayerInfoByPlayerCodeAsync(request.Payload.OperatorCustId.ToPlayerCode(), gameProviderCode);
		if (playerInfo == null)
		{
			return ErrorMapper.AccountNotExist;
		}

		var gameProviderBetLimit = await gameProviderRepository.GetBetLimitAsync(gameProviderCode, playerInfo.CurrencyCode);
		var brandBetLimit = await playerRepository.GetBrandBetLimitAsync(playerInfo.BrandId, playerInfo.CurrencyCode);
		var minBet = Math.Max(gameProviderBetLimit!.MinBet, brandBetLimit!.MinBet);
		var maxBet = Math.Min(gameProviderBetLimit.MaxBet, brandBetLimit.MaxBet);

		if (bet.ActualStake < minBet || bet.ActualStake > maxBet)
		{
			return ErrorMapper.PlayerExceededGameLimit;
		}
		return ErrorMapper.Success;
	}

	private bool IsFreeBet(PlaceBetAndSettleDataSet bet)
	{
		var freeBetKey = "FreeBet";
		var freeSpinKey = "FreeSpin";
		if (bet.ActualStake == 0 && bet.OtherBetInfo != null)
		{
			var isFreeBet = bet.OtherBetInfo.ContainsKey(freeBetKey) && bet.OtherBetInfo[freeBetKey].ToLowerInvariant() == "true";
			var isFreeSpin = bet.OtherBetInfo.ContainsKey(freeSpinKey) && bet.OtherBetInfo[freeSpinKey].ToLowerInvariant() == "true";

			return isFreeBet || isFreeSpin;
		}
		return false;
	}

	private async Task<PlaceBetAndSettleResult> ProcessBetAsync(
		PlaceBetAndSettleRequest request,
		PlaceBetAndSettleDataSet bet,
		decimal currentBalance,
		CancellationToken cancellationToken)
	{
		BalanceResponse? balanceResponse = null;
		var adjustAmount = CalculateAdjustAmount(bet);
		var refId = bet.TicketNo;
		var playerCode = request.Payload.OperatorCustId.ToPlayerCode();

		var isExistTicket = await IsExistTicketAsync(request);

		if (!isExistTicket && adjustAmount < 0)
		{
			balanceResponse = await cashServiceClient.GameProviderDeductBalanceAsync(
				playerCode,
				Math.Abs(adjustAmount),
				new ProviderInfo(request.Payload.GameProviderCode, refId, request.Seq, ActionConstants.PlaceBetAndSettle),
				cancellationToken: cancellationToken);

			if (!ErrorMapper.IsSuccess(balanceResponse.ErrorCode))
			{
				return ResultHelper.GenerateFailedResult<PlaceBetAndSettleResult, PlaceBetAndSettlePayload>(balanceResponse.ErrorCode, balanceResponse.ErrorMessage);
			}
		}

		var (criteria, spResult) = await InsertBetTransAsync(request, bet);
		var isFailedSp = !ErrorMapper.IsSuccess(spResult?.StatusCode ?? "0");

		if (!isExistTicket && IsAddBalance(adjustAmount, isFailedSp))
		{
			var action = isFailedSp ? ActionConstants.PlaceBetAndSettleFail : ActionConstants.PlaceBetAndSettle;

			balanceResponse = await cashServiceClient.GameProviderAddBalanceAsync(
				playerCode,
				Math.Abs(adjustAmount),
				new ProviderInfo(request.Payload.GameProviderCode, refId, request.Seq, action),
				cancellationToken: cancellationToken);

			if (!ErrorMapper.IsSuccess(balanceResponse.ErrorCode))
			{
				return ResultHelper.GenerateFailedResult<PlaceBetAndSettleResult, PlaceBetAndSettlePayload>(balanceResponse.ErrorCode, balanceResponse.ErrorMessage);
			}
		}

		if (isFailedSp)
		{
			return ResultHelper.GenerateFailedResult<PlaceBetAndSettleResult, PlaceBetAndSettlePayload>(spResult.StatusCode, spResult?.ErrMsg ?? string.Empty);
		}

		var balance = balanceResponse?.Balance ?? currentBalance;

		await messagePublisher.HandleBetAsync(HandleMessageMapper.PlaceBetAndSettle(criteria, spResult?.RefNo ?? 0), cancellationToken);

		return GenerateSuccessResult(request, bet, spResult, balance);
	}

	private async Task<bool> IsExistTicketAsync(PlaceBetAndSettleRequest request)
	{
		return await cashServiceClient.IsExistGameProviderTransactionLogAsync(request.Payload.OperatorCustId.ToPlayerCode(),
			new ProviderInfo(request.Payload.GameProviderCode, request.Payload.BetDataSet.First().TicketNo, ActionConstants.PlaceBetAndSettle));
	}

	private async Task<(PlaceBetAndSettleCriteria, SPResult)> InsertBetTransAsync(PlaceBetAndSettleRequest request, PlaceBetAndSettleDataSet bet)
	{
		var criteria = ConvertToCriteria(request.TimeStamp, request.Seq, request.Payload, bet);
		var spResult = await gameProviderRepository.CallStoredProcedureAsync<PlaceBetAndSettleCriteria, SPResult>(
						criteria,
						StoreProcedureMapper.InsertSignalSP);

		return (criteria, spResult);
	}

	private static decimal CalculateAdjustAmount(PlaceBetAndSettleDataSet bet)
	{
		var betAmount = bet.ActualStake;
		var payout = bet.Payout;
		var adjustAmount = payout - betAmount;

		return adjustAmount;
	}

	private static PlaceBetAndSettleResult GenerateSuccessResult(
		PlaceBetAndSettleRequest request,
		PlaceBetAndSettleDataSet bet,
		SPResult spResult,
		decimal balance)
	{
		var refNo = spResult.RefNo.ToString();
		var dataSets = new PlaceBetAndSettleResultDataSet { RefNo = refNo, TicketNo = bet.TicketNo };
		var res = new PlaceBetAndSettleResult
		{
			Payload = new PlaceBetAndSettlePayload(request.Payload.OperatorCustId, balance, [dataSets])
		};

		return res;
	}

	private static bool IsAddBalance(decimal adjustAmount, bool failedSp)
		=> ShouldRefund(adjustAmount, failedSp) || adjustAmount > 0;

	private static bool ShouldRefund(decimal adjustAmount, bool failedSp) => failedSp && adjustAmount < 0;

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