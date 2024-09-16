using AutoMapper;
using SabaPlus.API.Messages;
using SabaPlus.API.Models;
using SabaPlus.API.Models.PlaceBet;
using SabaPlus.Common;
using SabaPlus.Core;
using SabaPlus.Repositories.Helpers;
using SabaPlus.Repositories.Models;
using Tgs.CashService;
using Tgs.CashService.Client;
using Tgs.ServiceUtility;

namespace SabaPlus.API.Services;

public class PlaceBetGetService(
	ISabaPlusApiMessagePublishHandler messagePublisher,
	IGameProviderRepository gameProviderRepository,
	ICashServiceClient cashClient,
	IMapper mapper
	) : IGetService<PlaceBetRequest, PlaceBetResult>
{
	public async ValueTask<PlaceBetResult> GetAsync(PlaceBetRequest request, CancellationToken cancellationToken = default)
	{
		try
		{
			var playerCode = request.Payload.OperatorCustId;
			var betAmount = request.Payload.BetDataSet.Sum(betData => betData.ActualStake)!.Value;
			var refId = string.Join('@', request.Payload.BetDataSet.Select(betData => betData.TicketNo));
			var seq = request.Seq;

			var balanceResult = await DeductBalanceAsync(request, cancellationToken);
			if (!ErrorMapper.IsSuccess(balanceResult.ErrorCode))
			{
				return await GenerateResponseAsync(request, GenerateFailResponse(balanceResult.ErrorCode), cancellationToken);
			}

			var criteriaList = GetPlaceBetCriterias(request);
			var spResults = await InsertTransactionsAsync(criteriaList);
			var failedSp = spResults.Find(spResult => !ErrorMapper.IsSuccess(spResult.StatusCode!));
			if (failedSp != default)
			{
				await RefundBalanceAsync(request, cancellationToken);
				return await GenerateResponseAsync(request, GenerateFailResponse(failedSp.StatusCode!), cancellationToken);
			}

			var betDataSet = spResults.Select((spResult, index) =>
								new PlaceBetResultDataSet { RefNo = spResult.RefNo.ToString(), TicketNo = request.Payload.BetDataSet[index].TicketNo });
			var res = new PlaceBetResult
			{
				Payload = new PlaceBetResponsePayload(playerCode, balanceResult.Balance, betDataSet.ToArray())
			};

			PublishBetAsync(criteriaList, cancellationToken);
			return await GenerateResponseAsync(request, res, cancellationToken);
		}
		catch (Exception ex)
		{
			SentrySdk.CaptureException(ex);
			return await GenerateResponseAsync(request, new PlaceBetResult { ErrorCode = ErrorMapper.UnspecifiedError }, cancellationToken);
		}
	}

	private async ValueTask<BalanceResponse> DeductBalanceAsync(PlaceBetRequest request, CancellationToken cancellationToken)
	{
		var betAmount = request.Payload.BetDataSet.Sum(betData => betData.ActualStake)!.Value;
		var refId = string.Join('@', request.Payload.BetDataSet.Select(betData => betData.TicketNo));
		return await cashClient.GameProviderDeductBalanceAsync(
			request.Payload.OperatorCustId!,
			betAmount,
			new ProviderInfo(request.Payload.GameProviderCode!, refId, request.Seq, ActionConstants.PlaceBet),
			string.Empty,
			cancellationToken
			).ConfigureAwait(false);
	}

	private async ValueTask RefundBalanceAsync(PlaceBetRequest request, CancellationToken cancellationToken)
	{
		var betAmount = request.Payload.BetDataSet.Sum(betData => betData.ActualStake)!.Value;
		var refId = string.Join('@', request.Payload.BetDataSet.Select(betData => betData.TicketNo));
		await cashClient.GameProviderAddBalanceAsync(
			request.Payload.OperatorCustId!,
			betAmount,
			new ProviderInfo(request.Payload.GameProviderCode!, refId, request.Seq, ActionConstants.PlaceBetFail),
			string.Empty,
			cancellationToken
			).ConfigureAwait(false);
	}

	private async Task<List<SPResult>> InsertTransactionsAsync(IEnumerable<PlaceBetCriteria> criteriaList)
	{
		var result = new List<SPResult>();
		foreach (var criteria in criteriaList)
		{
			var spResult = await gameProviderRepository.CallStoredProcedureAsync<PlaceBetCriteria, SPResult>(criteria, StoreProcedureMapper.InsertSignalSP);
			result.Add(spResult);
		}
		return result;
	}

	private IEnumerable<PlaceBetCriteria> GetPlaceBetCriterias(PlaceBetRequest request)
	{
		return request.Payload.BetDataSet.Select(betData =>
		{
			var criteria = mapper.Map<PlaceBetCriteria>(betData!);
			criteria.TransDate = DateTime.Parse(request.TimeStamp).ToUniversalTime();
			criteria.Seq = request.Seq;
			criteria.OperatorCustId = request.Payload.OperatorCustId;
			criteria.GameType = request.Payload.GameType;
			criteria.IpAddress = request.Payload.IpAddress;
			criteria.GameProviderCode = request.Payload.GameProviderCode;
			//criteria.GameId = request.Payload.S1GameId;
			return criteria;
		});
	}

	private async Task PublishBetAsync(IEnumerable<PlaceBetCriteria> criteriaList, CancellationToken cancellationToken)
	{
		foreach (var criteria in criteriaList)
		{
			await messagePublisher.HandleBetAsync(HandleMessageMapper.PlaceBet(criteria), cancellationToken);
		}
	}

	private PlaceBetResult GenerateFailResponse(string errorCode)
	{
		return new PlaceBetResult { ErrorCode = ErrorMapper.GetErrorCode(errorCode) };
	}

	private async ValueTask<PlaceBetResult> GenerateResponseAsync(PlaceBetRequest request, PlaceBetResult result, CancellationToken cancellationToken)
	{
		// TODO: insert into bet.rounds
		var apiLogMessage = new ApiLogMessage()
		{
			Action = ActionConstants.PlaceBet,
			Request = request,
			Response = result
		};
		await messagePublisher.LogApi(apiLogMessage, cancellationToken);

		return result;
	}
}