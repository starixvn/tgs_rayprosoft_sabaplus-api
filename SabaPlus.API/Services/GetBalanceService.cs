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
			Console.WriteLine(query.message);
			var res = new GetBalanceResult("0", query.message.UserId, 100, DateTime.UtcNow.ToString());

			await messagePublisher.LogApi(new ApiLogMessage()
			{
				Key = query.key,
				Action = query.message.Action,
				Request = query,
				Response = res
			}, cancellationToken);

			//TO DO
			//#1. Check Valid UserId
			//#2. Get Actual Balance
			//#3. Define GetBalance Error Codes

			return res;
		}
		catch (Exception ex)
		{
			Console.Write(ex.ToString());
			return new GetBalanceResult("203", null, null, null, "Account not existed");
		}
	}
}