using SabaPlus.API.Messages;
using SabaPlus.API.Models;
using SabaPlus.API.Models.Balance;
using SabaPlus.Common;
using Tgs.CashService.Client;
using Tgs.ServiceUtility;

namespace SabaPlus.API.Services;

public class GetBalanceService(
	ISabaPlusApiMessagePublishHandler messagePublisher,
	ICashServiceClient cashClient
	) : IGetService<GetBalanceRequest, GetBalanceResult>
{
	public async ValueTask<GetBalanceResult> GetAsync(GetBalanceRequest query, CancellationToken cancellationToken = default)
	{
		try
		{
			var playerCode = query.Payload.OperatorCustId;
			var balanceResult = await cashClient.GameProviderGetBalanceAsync(playerCode, cancellationToken).ConfigureAwait(false);
			var res = new GetBalanceResult
			{
				Payload = new GetBalanceResponsePayload(balanceResult.Balance),
			};
			if (!ErrorMapper.IsSuccess(balanceResult.ErrorCode))
			{
				res.ErrorCode = ErrorMapper.GetErrorCode(balanceResult.ErrorCode);
			}
			await messagePublisher.LogApi(new ApiLogMessage()
			{
				Action = nameof(GetBalanceRequest),
				Request = query,
				Response = res
			}, cancellationToken);

			return res;
		}
		catch (Exception ex)
		{
			SentrySdk.CaptureException(ex);
			return new GetBalanceResult { ErrorCode = ErrorMapper.UnspecifiedError };
		}
	}
}