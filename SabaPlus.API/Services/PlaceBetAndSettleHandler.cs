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
		if (request.Payload.BetDataSet.Length == 0 || string.IsNullOrEmpty(request.Payload.WalletSideToken))
		{
			return ErrorMapper.ParameterIncorrect;
		}
		var bet = bets.First();

		var gameProviderCode = request.Payload.GameProviderCode;
		var playerInfo = await playerRepository.GetPlayerInfoByPlayerCodeAsync(request.Payload.OperatorCustId.ToPlayerCode());
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

		var (criteria, spResult) = await InsertBetTransAsync(request, bet);
		if (!ErrorMapper.IsSuccess(spResult.StatusCode))
		{
			return ResultHelper.GenerateFailedResult<PlaceBetAndSettleResult, PlaceBetAndSettlePayload>(spResult.StatusCode, spResult.ErrMsg);
		}
		if (adjustAmount == 0)
		{
			balanceResponse = await cashServiceClient.GameProviderGetBalanceAsync(playerCode, cancellationToken);
		}
		else
		{
			balanceResponse = await cashServiceClient.GameProviderAddOrDeductBalanceByActionAsync(
			playerCode,
			adjustAmount,
			new ProviderInfo(request.Payload.GameProviderCode, refId, request.Seq, ActionConstants.PlaceBetAndSettle),
			false,
			string.Empty,
			cancellationToken);
		}
		if (!ErrorMapper.IsSuccess(balanceResponse.ErrorCode))
		{
			return ResultHelper.GenerateFailedResult<PlaceBetAndSettleResult, PlaceBetAndSettlePayload>(balanceResponse.ErrorCode, balanceResponse.ErrorMessage);
		}
		await messagePublisher.HandleBetAsync(HandleMessageMapper.PlaceBetAndSettle(criteria, spResult?.RefNo ?? 0), cancellationToken);

		var dataSets = new PlaceBetAndSettleResultDataSet { RefNo = spResult.RefNo.ToString(), TicketNo = bet.TicketNo };
		return new PlaceBetAndSettleResult
		{
			Payload = new PlaceBetAndSettlePayload(request.Payload.OperatorCustId, balanceResponse.Balance, [dataSets])
		};
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