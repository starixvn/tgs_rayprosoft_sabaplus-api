using AutoMapper;
using MediatR;
using SabaPlus.API.Messages;
using SabaPlus.API.Models;
using SabaPlus.API.Models.FreeSpin;
using SabaPlus.API.Services.Shared;
using SabaPlus.Common;
using SabaPlus.Core;
using SabaPlus.Repositories.Helpers;
using SabaPlus.Repositories.Models;
using Tgs.CashService;
using Tgs.CashService.Client;

namespace SabaPlus.API.Services;

public class FreeSpinHandler(
	ISabaPlusApiMessagePublishHandler messagePublisher,
	ICashServiceClient cashClient,
	IGameProviderRepository gameProviderRepository,
	IMapper mapper,
	ISentryClient sentryClient
	) : IRequestHandler<FreeSpinRequest, FreeSpinResult>
{
	public async Task<FreeSpinResult> Handle(FreeSpinRequest request, CancellationToken cancellationToken)
	{
		try
		{
			var gameProviderCode = await gameProviderRepository.GetGameProviderCodeByGameTypeAsync(request.Payload.GameType);

			if (string.IsNullOrWhiteSpace(gameProviderCode))
			{
				return ResultHelper.GenerateFailedResult<FreeSpinResult, FreeSpinPayload>(
					ErrorMapper.InvalidVendor,
					ErrorMapper.GetErrorMessage(ErrorMapper.InvalidVendor));
			}

			var (criteria, res, refNo) = await ProcessFreeSpinAsync(request, gameProviderCode, cancellationToken);

			if (criteria != null)
			{
				await messagePublisher.HandleBetAsync(HandleMessageMapper.FreeSpin(criteria, refNo), cancellationToken);
			}

			return res;
		}
		catch (Exception ex)
		{
			_ = sentryClient.CaptureException(ex);
			return new FreeSpinResult { ErrorCode = ErrorMapper.UnspecifiedError };
		}
	}

	private async Task<(FreeSpinCriteria?, FreeSpinResult, long refNo)> ProcessFreeSpinAsync(FreeSpinRequest request, string gameProviderCode, CancellationToken cancellationToken)
	{
		var criteria = GetFreeSpinCriteria(request, gameProviderCode);

		if (criteria == null)
		{
			return (criteria,
				ResultHelper.GenerateFailedResult<FreeSpinResult, FreeSpinPayload>(
					ErrorMapper.ParameterIncorrect,
					ErrorMapper.GetErrorMessage(ErrorMapper.ParameterIncorrect)),
				0);
		}

		var spResult = await gameProviderRepository.CallStoredProcedureAsync<FreeSpinCriteria, SPResult>(
			criteria,
			StoreProcedureMapper.InsertSignalSP);

		if (!ErrorMapper.IsSuccess(spResult.StatusCode!))
		{
			return (
				criteria,
				ResultHelper.GenerateFailedResult<FreeSpinResult, FreeSpinPayload>(spResult.StatusCode!, spResult.ErrMsg ?? string.Empty),
				0);
		}

		var balanceResult = await AddOrGetBalanceAsync(request, gameProviderCode, cancellationToken);

		if (!ErrorMapper.IsSuccess(balanceResult.ErrorCode))
		{
			return (
				criteria,
				ResultHelper.GenerateFailedResult<FreeSpinResult, FreeSpinPayload>(balanceResult.ErrorCode, balanceResult.ErrorMessage),
				0);
		}

		var betDataSet = new FreeSpinResponseDataSet
		{
			RefNo = spResult.RefNo.ToString() ?? string.Empty,
			TicketNo = request.Payload.BetDataSet.First().TicketNo
		};

		var res = new FreeSpinResult
		{
			Payload = new FreeSpinPayload(request.Payload.OperatorCustId, balanceResult.Balance, [betDataSet])
		};

		return (criteria, res, spResult.RefNo ?? 0);
	}

	private FreeSpinCriteria? GetFreeSpinCriteria(FreeSpinRequest request, string gameProviderCode)
	{
		var bet = request.Payload.BetDataSet.First();

		if (bet == null)
		{
			return null;
		}

		var criteria = mapper.Map<FreeSpinCriteria>(bet!);
		criteria.TransDate = DateTime.Parse(request.TimeStamp).ToUniversalTime();
		criteria.Seq = request.Seq;
		criteria.OperatorCustId = request.Payload.OperatorCustId;
		criteria.GameType = request.Payload.GameType;
		criteria.IpAddress = request.Payload.IpAddress;
		criteria.GameProviderCode = gameProviderCode;

		return criteria;
	}

	private async ValueTask<BalanceResponse> AddOrGetBalanceAsync(FreeSpinRequest request, string gameProviderCode, CancellationToken cancellationToken)
	{
		var bet = request.Payload.BetDataSet.First();
		var amount = bet.Payout;
		var refId = string.Join('@', request.Payload.BetDataSet.Select(data => data.TicketNo));
		var playerCode = request.Payload.OperatorCustId!.ToPlayerCode();

		return amount > 0
			? await cashClient.GameProviderAddBalanceAsync(
				playerCode,
				amount,
				new ProviderInfo(gameProviderCode!, refId, request.Seq, ActionConstants.FreeSpin),
				cancellationToken: cancellationToken)
			: await cashClient.GameProviderGetBalanceAsync(playerCode, cancellationToken);
	}
}