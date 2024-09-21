using MediatR;
using SabaPlus.API.Models;
using SabaPlus.API.Models.Balance;
using SabaPlus.Common;
using Tgs.CashService.Client;

namespace SabaPlus.API.Services;

public class GetBalanceHandler(
	ICashServiceClient cashClient,
	ISentryClient sentryClient
	) : IRequestHandler<GetBalanceRequest, GetBalanceResult>
{
	public async Task<GetBalanceResult> Handle(GetBalanceRequest query, CancellationToken cancellationToken = default)
	{
		try
		{
			var playerCode = query.Payload.OperatorCustId.ToPlayerCode();
			var balanceResult = await cashClient.GameProviderGetBalanceAsync(playerCode, cancellationToken).ConfigureAwait(false);
			var res = new GetBalanceResult
			{
				Payload = new GetBalanceResponsePayload(balanceResult.Balance),
			};
			if (!ErrorMapper.IsSuccess(balanceResult.ErrorCode))
			{
				res.ErrorCode = ErrorMapper.GetErrorCode(balanceResult.ErrorCode);
			}

			return res;
		}
		catch (Exception ex)
		{
			sentryClient.CaptureException(ex);
			return new GetBalanceResult { ErrorCode = ErrorMapper.UnspecifiedError };
		}
	}
}