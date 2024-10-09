using AutoMapper;
using MediatR;
using SabaPlus.API.Messages;
using SabaPlus.API.Models;
using SabaPlus.API.Models.VoidGame;
using SabaPlus.API.Services.Shared;
using SabaPlus.Common;
using SabaPlus.Core;
using SabaPlus.Repositories.Helpers;
using SabaPlus.Repositories.Models;
using Tgs.CashService.Client;

namespace SabaPlus.API.Services;

public class VoidGameHandler(
	ISabaPlusApiMessagePublishHandler messagePublisher,
	IGameProviderRepository gameProviderRepository,
	ICashServiceClient cashClient,
	ISentryClient sentryClient,
	IMapper mapper
	) : IRequestHandler<VoidGameRequest, VoidGameResult>
{
	public async Task<VoidGameResult> Handle(VoidGameRequest request, CancellationToken cancellationToken = default)
	{
		try
		{
			var gameProviderCode = await gameProviderRepository.GetGameProviderCodeByGameTypeAsync(request.Payload.GameType);
			var errorCode = ErrorMapper.Success.ToString();
			var balance = 0M;
			foreach (var ticket in request.Payload.BetDataSet)
			{
				var balanceResponse = await ProcessVoidTicketAsync(request, ticket, gameProviderCode, cancellationToken);
				balance = balanceResponse?.Balance ?? 0;
				if (!ErrorMapper.IsSuccess(balanceResponse.ErrorCode) && !ErrorMapper.IsDulicateTransactionByAction(balanceResponse.ErrorCode))
				{
					errorCode = balanceResponse.ErrorCode;
					sentryClient.CaptureMessage($"{errorCode} - {balanceResponse.ErrorMessage}", ResultHelper.GetSentryLevelFromConfig(errorCode));
				}
			}

			return new VoidGameResult
			{
				ErrorCode = ErrorMapper.GetErrorCode(errorCode),
				Payload = new VoidGameResponsePayload
				{
					Balance = balance,
					OperatorCustId = request.Payload.OperatorCustId
				}
			};
		}
		catch (Exception ex)
		{
			sentryClient.CaptureException(ex);
			return new VoidGameResult { ErrorCode = ErrorMapper.UnspecifiedError };
		}
	}

	private async Task<BalanceResponse> ProcessVoidTicketAsync(
		VoidGameRequest request,
		VoidGameRequestDataSet ticket,
		string gameProviderCode,
		CancellationToken cancellationToken)
	{
		var criteria = GetCriteria(request, ticket, gameProviderCode);
		var sp = await gameProviderRepository.CallStoredProcedureAsync<VoidGameCriteria, SPResult>(criteria, StoreProcedureMapper.InsertSignalSP);
		if (!ErrorMapper.IsSuccess(sp.StatusCode))
		{
			return new BalanceResponse { Balance = 0, ErrorCode = sp.StatusCode, ErrorMessage = sp.ErrMsg };
		}

		var adjustBalanceResult = await GetAdjustBalanceAsync(ticket.TicketNo, request.Payload.OperatorCustId, request.Payload.GameType, sp.RefNo);
		if (!ErrorMapper.IsSuccess(adjustBalanceResult.StatusCode))
		{
			return new BalanceResponse { Balance = 0, ErrorCode = adjustBalanceResult.StatusCode, ErrorMessage = adjustBalanceResult.ErrMsg };
		}

		var balanceResult = await AdjustBalanceAsync(request, ticket.TicketNo, gameProviderCode, adjustBalanceResult.BalanceAdjust, cancellationToken);
		if (ErrorMapper.IsSuccess(balanceResult.ErrorCode) || ErrorMapper.IsDulicateTransactionByAction(balanceResult.ErrorCode))
		{
			await messagePublisher.HandleBetAsync(HandleMessageMapper.VoidGame(criteria, sp.RefNo.Value), cancellationToken);
		}
		if (ErrorMapper.IsDulicateTransactionByAction(balanceResult.ErrorCode))
		{
			balanceResult = await cashClient.GameProviderGetBalanceAsync(criteria.PlayerCode, cancellationToken);
		}
		return balanceResult;
	}

	private async Task<BalanceResponse> AdjustBalanceAsync(
		VoidGameRequest request,
		string ticketNo,
		string gameProviderCode,
		decimal amount,
		CancellationToken cancellationToken)
	{
		var playerCode = request.Payload.OperatorCustId.ToPlayerCode();
		if (amount == 0)
		{
			return await cashClient.GameProviderGetBalanceAsync(playerCode, cancellationToken);
		}

		return await cashClient.GameProviderAdjustBalanceByActionAsync(
				playerCode,
				amount,
				new Tgs.CashService.ProviderInfo(gameProviderCode, ticketNo, request.Seq, ActionConstants.VoidGame),
				true,
				string.Empty,
				cancellationToken);
	}

	private async Task<GetAdjustBalanceTicketSPResult?> GetAdjustBalanceAsync(string ticketNo, string operatorCustId, string gameType, long? refNo)
	{
		var criteria = new GetAdjustBalanceTicketCriteria()
		{
			TicketNo = ticketNo,
			OperatorCustId = operatorCustId,
			GroupAction = "void",
			GameType = gameType,
			RefNo = refNo
		};
		return await gameProviderRepository
			.CallStoredProcedureAsync<GetAdjustBalanceTicketCriteria, GetAdjustBalanceTicketSPResult?>(criteria, StoreProcedureMapper.GetAdjustBalanceTicketSP)
			.ConfigureAwait(false);
	}

	private VoidGameCriteria GetCriteria(VoidGameRequest request, VoidGameRequestDataSet ticket, string gameProviderCode)
	{
		var criteria = mapper.Map<VoidGameCriteria>(ticket);
		criteria.Seq = request.Seq;
		criteria.OperatorCustId = request.Payload.OperatorCustId;
		criteria.GameType = request.Payload.GameType;
		criteria.GameProviderCode = gameProviderCode;
		criteria.TransDate = DateTime.Parse(request.TimeStamp).ToUniversalTime();
		criteria.Action = request.Payload.Action;
		return criteria;
	}
}