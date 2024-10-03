using System.Net.Sockets;
using AutoMapper;
using MediatR;
using SabaPlus.API.Messages;
using SabaPlus.API.Models;
using SabaPlus.API.Models.EndGame;
using SabaPlus.API.Services.Shared;
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
			var balance = 0M;
			var errorCode = ErrorMapper.Success.ToString();
			for (var i = 0; i < request.Payload.BetDataSet.Length; i++)
			{
				var balanceResponse = await ProcessEndGameAsync(request, request.Payload.BetDataSet[i], gameProviderCode, cancellationToken);
				if (!ErrorMapper.IsSuccess(balanceResponse.ErrorCode))
				{
					errorCode = balanceResponse.ErrorCode;
					sentryClient.CaptureMessage($"{errorCode} - {balanceResponse.ErrorMessage}", ResultHelper.GetSentryLevelFromConfig(balanceResponse.ErrorCode));
				}
				balance = balanceResponse.Balance;
			}

			return new EndGameResult
			{
				ErrorCode = ErrorMapper.GetErrorCode(errorCode),
				Payload = new EndGameResponsePayload(request.Payload.OperatorCustId, balance)
			};
		}
		catch (Exception ex)
		{
			sentryClient.CaptureException(ex);
			return new EndGameResult { ErrorCode = ErrorMapper.UnspecifiedError };
		}
	}

	public async Task<BalanceResponse> ProcessEndGameAsync(
		EndGameRequest request,
		EndGameDataSet ticket,
		string gameProviderCode,
		CancellationToken cancellationToken)
	{
		var adjustBalanceResult = await GetAdjustBalanceAsync(ticket, request.Payload.OperatorCustId, request.Payload.GameType);
		if (!ErrorMapper.IsSuccess(adjustBalanceResult.StatusCode))
		{
			return new BalanceResponse { Balance = 0, ErrorCode = adjustBalanceResult.StatusCode, ErrorMessage = adjustBalanceResult.ErrMsg };
		}

		var criteria = GetEndGameCriteria(request, ticket, gameProviderCode);
		var spResult = await gameProviderRepository.CallStoredProcedureAsync<EndGameCriteria, SPResult>(criteria, StoreProcedureMapper.InsertSignalSP);
		if (!ErrorMapper.IsSuccess(spResult.StatusCode))
		{
			return new BalanceResponse { Balance = 0, ErrorCode = spResult.StatusCode, ErrorMessage = spResult.ErrMsg };
		}

		var balanceResult = await AddBalanceAsync(ticket, criteria.PlayerCode, gameProviderCode, adjustBalanceResult.BalanceAdjust, request.Seq);
		if (ErrorMapper.IsSuccess(balanceResult.ErrorCode))
		{
			await messagePublisher.HandleBetAsync(HandleMessageMapper.EndGame(criteria), cancellationToken);
		}

		if (ErrorMapper.IsDulicateTransactionByAction(balanceResult.ErrorCode))
		{
			balanceResult = await cashClient.GameProviderGetBalanceAsync(criteria.PlayerCode, cancellationToken);
		}
		return balanceResult;
	}

	private async ValueTask<BalanceResponse> AddBalanceAsync(EndGameDataSet ticket, string playerCode, string gameProviderCode, decimal adjustAmount, string seq)
	{
		if (adjustAmount == 0)
		{
			return await cashClient.GameProviderGetBalanceAsync(playerCode);
		}
		var action = ActionConstants.EndGame;
		if (ticket.OtherBetInfo?.TryGetValue("IsResettle", out string isResettle) == true)
		{
			action = isResettle.ToLowerInvariant() == "true" ? ActionConstants.Resettle : ActionConstants.EndGame;
		}
		if (action == ActionConstants.Resettle)
		{
			return await cashClient.GameProviderAdjustBalanceByActionAsync(
			playerCode,
			adjustAmount,
			new Tgs.CashService.ProviderInfo(gameProviderCode, ticket.TicketNo, seq, action));
		}
		return await cashClient.GameProviderAddOrDeductBalanceByActionAsync(
			playerCode,
			adjustAmount,
			new Tgs.CashService.ProviderInfo(gameProviderCode, ticket.TicketNo, seq, action));
	}

	private async ValueTask<GetAdjustBalanceTicketSPResult> GetAdjustBalanceAsync(EndGameDataSet ticket, string operatorCustId, string gameType)
	{
		var criteria = mapper.Map<GetAdjustBalanceTicketCriteria>(ticket);
		criteria.GroupAction = "settle";
		criteria.OperatorCustId = operatorCustId;
		criteria.GameType = gameType;
		return await gameProviderRepository.CallStoredProcedureAsync<GetAdjustBalanceTicketCriteria, GetAdjustBalanceTicketSPResult>(criteria, StoreProcedureMapper.GetAdjustBalanceTicketSP);
	}

	private EndGameCriteria GetEndGameCriteria(EndGameRequest request, EndGameDataSet ticket, string gameProviderCode)
	{
		var criteria = mapper.Map<EndGameCriteria>(ticket);
		criteria.Seq = request.Seq;
		criteria.OperatorCustId = request.Payload.OperatorCustId;
		criteria.GameType = request.Payload.GameType;
		criteria.GameProviderCode = gameProviderCode;
		criteria.Action = request.Payload.Action;
		return criteria;
	}
}