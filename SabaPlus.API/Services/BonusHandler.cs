using AutoMapper;
using MediatR;
using SabaPlus.API.Messages;
using SabaPlus.API.Models;
using SabaPlus.API.Models.Bonus;
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
	IMapper mapper
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
				return await GenerateResponseAsync(request, GenerateFailResponse(failedSp.StatusCode!), cancellationToken);
			}

			var balanceResult = await AddBalanceAsync(request, gameProviderCode, cancellationToken);
			if (!ErrorMapper.IsSuccess(balanceResult.ErrorCode))
			{
				return await GenerateResponseAsync(request, GenerateFailResponse(balanceResult.ErrorCode), cancellationToken);
			}

			var refNo = spResults.FirstOrDefault().RefNo;
			var res = new BonusResult
			{
				Payload = new BonusPayload(request.Payload.OperatorCustId, balanceResult.Balance, refNo.ToString())
			};

			await PublishBetMessageAsync(criteriaList.FirstOrDefault(), refNo.Value, cancellationToken);
			return await GenerateResponseAsync(request, res, cancellationToken);
		}
		catch (Exception ex)
		{
			Console.Write(ex.ToString());
			return new BonusResult
			{
				ErrorCode = 1,
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
			//criteria.GameId = request.Payload.S1GameId;
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
				request.Payload.OperatorCustId!,
				amount,
				new Tgs.CashService.ProviderInfo(gameProviderCode!, refId, request.Seq, ActionConstants.EndGame),
			string.Empty,
				cancellationToken);
		}
		return await cashClient.GameProviderGetBalanceAsync(request.Payload.OperatorCustId!, cancellationToken);
	}

	private async ValueTask PublishBetMessageAsync(BonusCriteria criteria, long? refNo, CancellationToken cancellationToken)
	{
		await messagePublisher.HandleBetAsync(HandleMessageMapper.Bonus(criteria, refNo.Value), cancellationToken);
	}

	private BonusResult GenerateFailResponse(string errorCode)
	{
		return new BonusResult { ErrorCode = ErrorMapper.GetErrorCode(errorCode) };
	}

	private async ValueTask<BonusResult> GenerateResponseAsync(BonusRequest request, BonusResult result, CancellationToken cancellationToken)
	{
		var apiLogMessage = new ApiLogMessage()
		{
			Action = ActionConstants.Bonus,
			Request = request,
			Response = result
		};
		await messagePublisher.LogApi(apiLogMessage, cancellationToken);

		return result;
	}
}