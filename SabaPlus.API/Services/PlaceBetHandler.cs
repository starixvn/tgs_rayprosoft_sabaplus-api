using AutoMapper;
using MediatR;
using SabaPlus.API.Messages;
using SabaPlus.API.Models;
using SabaPlus.API.Models.PlaceBet;
using SabaPlus.API.Services.Shared;
using SabaPlus.Common;
using SabaPlus.Core;
using SabaPlus.Repositories.Helpers;
using SabaPlus.Repositories.Models;
using Tgs.CashService;
using Tgs.CashService.Client;

namespace SabaPlus.API.Services;

public class PlaceBetGetHandler(
	ISabaPlusApiMessagePublishHandler messagePublisher,
	IGameProviderRepository gameProviderRepository,
	ICashServiceClient cashClient,
	IMapper mapper,
	ISentryClient sentryClient
	) : IRequestHandler<PlaceBetRequest, PlaceBetResult>
{
	public async Task<PlaceBetResult> Handle(PlaceBetRequest request, CancellationToken cancellationToken = default)
	{
		try
		{
			var balanceResult = await DeductBalanceAsync(request, cancellationToken);
			if (!ErrorMapper.IsSuccess(balanceResult.ErrorCode))
			{
				await RefundBalanceAsync(request, cancellationToken);
				return ResultHelper.GenerateFailedResult<PlaceBetResult, PlaceBetResponsePayload>(balanceResult.ErrorCode, balanceResult.ErrorMessage);
			}

			var criteriaList = GetPlaceBetCriterias(request);
			var spResults = await InsertTransactionsAsync(criteriaList);
			var failedSp = spResults.Find(spResult => !ErrorMapper.IsSuccess(spResult.StatusCode!));
			if (failedSp != default)
			{
				await RefundBalanceAsync(request, cancellationToken);
				return ResultHelper.GenerateFailedResult<PlaceBetResult, PlaceBetResponsePayload>(failedSp.StatusCode, failedSp.ErrMsg);
			}

			var betDataSet = spResults.Select((result, index) =>
								new PlaceBetResultDataSet { RefNo = result.RefNo.ToString(), TicketNo = request.Payload.BetDataSet[index].TicketNo });
			var res = new PlaceBetResult
			{
				Payload = new PlaceBetResponsePayload(request.Payload.OperatorCustId, balanceResult.Balance, betDataSet.ToArray())
			};

			await PublishBetAsync(criteriaList, spResults, cancellationToken);
			return res;
		}
		catch (Exception ex)
		{
			sentryClient.CaptureException(ex);
			return new PlaceBetResult { ErrorCode = ErrorMapper.UnspecifiedError };
		}
	}

	private async ValueTask<BalanceResponse> DeductBalanceAsync(PlaceBetRequest request, CancellationToken cancellationToken)
	{
		var playerCode = request.Payload.OperatorCustId!.ToPlayerCode();
		var res = new BalanceResponse();
		foreach (var bet in request.Payload.BetDataSet)
		{
			res = await cashClient.GameProviderDeductBalanceAsync(
				request.Payload.OperatorCustId!.ToPlayerCode(),
				bet.ActualStake,
				new ProviderInfo(request.Payload.GameProviderCode!, bet.TicketNo, request.Seq, ActionConstants.PlaceBet),
				string.Empty,
				cancellationToken
				).ConfigureAwait(false);
			if (!ErrorMapper.IsSuccess(res.ErrorCode))
			{
				return res;
			}
		}
		return res;
	}

	private async ValueTask RefundBalanceAsync(PlaceBetRequest request, CancellationToken cancellationToken)
	{
		var playerCode = request.Payload.OperatorCustId!.ToPlayerCode();
		foreach (var bet in request.Payload.BetDataSet)
		{
			if (await cashClient.IsExistGameProviderTransactionLogAsync(playerCode,
				new ProviderInfo(request.Payload.GameProviderCode!, bet.TicketNo, request.Seq, ActionConstants.PlaceBet)))
			{
				await cashClient.GameProviderAddBalanceAsync(
				playerCode,
				bet.ActualStake,
				new ProviderInfo(request.Payload.GameProviderCode!, bet.TicketNo, request.Seq, ActionConstants.PlaceBetFail),
				string.Empty,
				cancellationToken
				).ConfigureAwait(false);
			}
			else
			{
				break;
			}
		}
	}

	private IEnumerable<PlaceBetCriteria> GetPlaceBetCriterias(PlaceBetRequest request)
	{
		return request.Payload.BetDataSet.Select(bet =>
		{
			var criteria = mapper.Map<PlaceBetCriteria>(bet);
			criteria.TransDate = DateTime.Parse(request.TimeStamp).ToUniversalTime();
			criteria.Seq = request.Seq;
			criteria.OperatorCustId = request.Payload.OperatorCustId;
			criteria.GameType = request.Payload.GameType;
			criteria.IpAddress = request.Payload.IpAddress;
			criteria.GameProviderCode = request.Payload.GameProviderCode;
			return criteria;
		});
	}

	private async Task<List<SPResult>> InsertTransactionsAsync(IEnumerable<PlaceBetCriteria> criterias)
	{
		var results = new List<SPResult>();
		foreach (var criteria in criterias)
		{
			var result = await gameProviderRepository.CallStoredProcedureAsync<PlaceBetCriteria, SPResult>(criteria, StoreProcedureMapper.InsertSignalSP);
			results.Add(result);
		}
		return results;
	}

	private async Task PublishBetAsync(IEnumerable<PlaceBetCriteria> criteriaList, List<SPResult> spResults, CancellationToken cancellationToken)
	{
		foreach (var item in criteriaList.Select((criteria, i) => (criteria, i)))
		{
			await messagePublisher.HandleBetAsync(HandleMessageMapper.PlaceBet(item.criteria, spResults[item.i].RefNo!.Value), cancellationToken);
		}
	}
}