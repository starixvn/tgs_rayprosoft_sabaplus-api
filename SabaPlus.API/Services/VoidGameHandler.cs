using AutoMapper;
using MediatR;
using SabaPlus.API.Messages;
using SabaPlus.API.Models;
using SabaPlus.API.Models.VoidGame;
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
			var errorCode = 0;
			var balance = 0M;
			foreach (var ticket in request.Payload.BetDataSet)
			{
				var (error, balanceRes) = await ProcessVoidTicketAsync(request, ticket, gameProviderCode, cancellationToken);
				balance = balanceRes?.Balance ?? 0;
				if (error != ErrorMapper.Success)
				{
					errorCode = error;
				}
			}
			return new VoidGameResult
			{
				ErrorCode = errorCode,
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

	private async Task<(int, BalanceResponse?)> ProcessVoidTicketAsync(
		VoidGameRequest request,
		VoidGameRequestDataSet ticket,
		string gameProviderCode,
		CancellationToken cancellationToken)
	{
		var amount = await GetAmountFromTicketsAsync(request, ticket);
		var criteria = GetCriteria(request, ticket, gameProviderCode);
		var sp = await VoidTicketsAsync(criteria);
		if (!ErrorMapper.IsSuccess(sp.StatusCode))
		{
			sentryClient.CaptureException(new Exception($"{sp.StatusCode} - {sp.ErrMsg}"));
			return (ErrorMapper.GetErrorCode(sp.StatusCode), null);
		}
		var balanceResult = await AdjustBalanceAsync(request, amount, ticket.TicketNo, gameProviderCode, cancellationToken);
		if (!ErrorMapper.IsSuccess(balanceResult.ErrorCode))
		{
			sentryClient.CaptureException(new Exception($"{balanceResult.ErrorCode} - {balanceResult.ErrorMessage}"));
			return (ErrorMapper.GetErrorCode(balanceResult.ErrorCode), balanceResult);
		}
		await PublishMessageAsync(criteria, cancellationToken);
		return (ErrorMapper.Success, balanceResult);
	}

	private async Task<BalanceResponse> AdjustBalanceAsync(
		VoidGameRequest request,
		decimal amount,
		string ticketNo,
		string gameProviderCode,
		CancellationToken cancellationToken)
	{
		var playerCode = request.Payload.OperatorCustId.ToPlayerCode();
		if (await cashClient.IsExistGameProviderTransactionLogAsync(
			playerCode,
			new Tgs.CashService.ProviderInfo(gameProviderCode, ticketNo, ActionConstants.VoidGame),
			cancellationToken))
		{
			return await cashClient.GameProviderGetBalanceAsync(playerCode, cancellationToken);
		}
		return await cashClient.GameProviderAdjustBalanceAsync(
			playerCode,
			amount > 0 ? amount : 0,
			amount < 0 ? amount : 0,
			new Tgs.CashService.ProviderInfo(gameProviderCode, ticketNo, request.Seq, ActionConstants.VoidGame),
			string.Empty,
			cancellationToken);
	}

	private async Task<decimal> GetAmountFromTicketsAsync(VoidGameRequest request, VoidGameRequestDataSet ticket)
	{
		// TODO: correct function
		//var criteria = new GetAmountVoidGameCriteria();
		//return await gameProviderRepository.CallStoredProcedureAsync<GetAmountVoidGameCriteria, decimal>(criteria, StoreProcedureMapper.GetTicketAmountSP);
		return 1;
	}

	private async Task<SPResult> VoidTicketsAsync(VoidGameCriteria criteria)
	{
		return await gameProviderRepository.CallStoredProcedureAsync<VoidGameCriteria, SPResult>(criteria, StoreProcedureMapper.InsertSignalSP);
	}

	private VoidGameCriteria GetCriteria(VoidGameRequest request, VoidGameRequestDataSet ticket, string gameProviderCode)
	{
		var criteria = mapper.Map<VoidGameCriteria>(ticket);
		criteria.Seq = request.Seq;
		criteria.OperatorCustId = request.Payload.OperatorCustId;
		criteria.GameType = request.Payload.GameType;
		criteria.GameProviderCode = gameProviderCode;
		criteria.TransDate = DateTime.Parse(request.TimeStamp).ToUniversalTime();
		return criteria;
	}

	private async Task PublishMessageAsync(VoidGameCriteria criteria, CancellationToken cancellationToken)
	{
		await messagePublisher.HandleBetAsync(HandleMessageMapper.VoidGame(criteria), cancellationToken);
	}
}