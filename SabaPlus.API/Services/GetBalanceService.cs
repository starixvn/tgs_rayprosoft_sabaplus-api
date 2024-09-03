using SabaPlus.API.Messages;
using SabaPlus.API.Models.Balance;
using SabaPlus.Common;
using Tgs.ServiceUtility;

namespace SabaPlus.API.Services;

public class GetBalanceService(ISabaPlusApiMessagePublishHandler messagePublisher) : IGetService<GetBalanceRequest, GetBalanceResult>
{
	public async ValueTask<GetBalanceResult> GetAsync(GetBalanceRequest query, CancellationToken cancellationToken = default)
	{
		try
		{
			var res = new GetBalanceResult
			{
				Seq = Guid.NewGuid().ToString(),
				Payload = new GetBalanceResponsePayload(100),
				TimeStamp = DateTime.UtcNow,
			};

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
			Console.Write(ex.ToString());
			return new GetBalanceResult
			{
				Seq = Guid.NewGuid().ToString(),
				ErrorCode = 1,
				ErrorMessage = "Account does not exist"
			};
		}
	}
}