using AutoMapper;
using MediatR;
using SabaPlus.API.Messages;
using SabaPlus.API.Models;
using SabaPlus.API.Models.Bonus;
using SabaPlus.API.Services.Shared;
using SabaPlus.Common;
using SabaPlus.Core;
using SabaPlus.Repositories.Helpers;
using SabaPlus.Repositories.Models;
using Tgs.CashService.Client;

namespace SabaPlus.API.Services;

public class BonusHandler(
	ISabaPlusApiMessagePublishHandler messagePublisher,
	ICashServiceClient cashClient,
	IGameProviderRepository gameProviderRepository,
	IMapper mapper,
	ISentryClient sentryClient
	) : IRequestHandler<BonusRequest, BonusResult>
{
	public async Task<BonusResult> Handle(BonusRequest request, CancellationToken cancellationToken = default)
	{
		try
		{
			var gameProviderCode = await gameProviderRepository.GetGameProviderCodeByGameTypeAsync(request.Payload.GameType);

			var criteriaList = GetBonusCriterias(request, gameProviderCode);
			var spResults = await InsertTransactionsAsync(criteriaList);
			var failedSp = spResults.Find(spResult => !ErrorMapper.IsSuccess(spResult.StatusCode!));
			if (failedSp != default)
			{
				return ResultHelper.GenerateFailedResult<BonusResult, BonusPayload>(failedSp.StatusCode!, failedSp.ErrMsg);
			}

			var balanceResult = await AddBalanceAsync(request, gameProviderCode, cancellationToken);
			if (!ErrorMapper.IsSuccess(balanceResult.ErrorCode))
			{
				return ResultHelper.GenerateFailedResult<BonusResult, BonusPayload>(balanceResult.ErrorCode, balanceResult.ErrorMessage);
			}

			var refNo = spResults.FirstOrDefault().RefNo;
			var res = new BonusResult
			{
				Payload = new BonusPayload(request.Payload.OperatorCustId, balanceResult.Balance, refNo.ToString())
			};

			await PublishBetMessageAsync(criteriaList.FirstOrDefault(), refNo.Value, cancellationToken);
			return res;
		}
		catch (Exception ex)
		{
			sentryClient.CaptureException(ex);

			return new BonusResult
			{
				ErrorCode = ErrorMapper.UnspecifiedError
			};
		}
	}

	private async Task<List<SPResult>> InsertTransactionsAsync(IEnumerable<BonusCriteria> criteriaList)
	{
		var result = new List<SPResult>();
		foreach (var criteria in criteriaList)
		{
			var spResult = await gameProviderRepository.CallStoredProcedureAsync<BonusCriteria, SPResult>(criteria, StoreProcedureMapper.InsertSignalSP);
			result.Add(spResult);
		}
		return result;
	}

	private IEnumerable<BonusCriteria> GetBonusCriterias(BonusRequest request, string gameProviderCode)
	{
		return request.Payload.BetDataSet.Select(betData =>
		{
			var criteria = mapper.Map<BonusCriteria>(betData!);
			criteria.TransDate = DateTime.Parse(request.TimeStamp).ToUniversalTime();
			criteria.Seq = request.Seq;
			criteria.OperatorCustId = request.Payload.OperatorCustId;
			criteria.GameType = request.Payload.GameType;
			criteria.GameProviderCode = gameProviderCode;
			return criteria;
		});
	}

	private async ValueTask<BalanceResponse> AddBalanceAsync(BonusRequest request, string gameProviderCode, CancellationToken cancellationToken)
	{
		var amount = request.Payload.BetDataSet.Sum(data => data.Payout);
		var refId = string.Join('@', request.Payload.BetDataSet.Select(data => data.TicketNo));
		if (amount > 0)
		{
			return await cashClient.GameProviderAddBalanceAsync(
				request.Payload.OperatorCustId!.ToPlayerCode(),
				amount,
				new Tgs.CashService.ProviderInfo(gameProviderCode!, refId, request.Seq, ActionConstants.EndGame),
			string.Empty,
				cancellationToken);
		}
		return await cashClient.GameProviderGetBalanceAsync(request.Payload.OperatorCustId!.ToPlayerCode(), cancellationToken);
	}

	private async ValueTask PublishBetMessageAsync(BonusCriteria criteria, long? refNo, CancellationToken cancellationToken)
	{
		await messagePublisher.HandleBetAsync(HandleMessageMapper.Bonus(criteria, refNo.Value), cancellationToken);
	}
}