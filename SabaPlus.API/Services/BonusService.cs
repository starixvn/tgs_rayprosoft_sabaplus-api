
using SabaPlus.API.Messages;
using SabaPlus.API.Models.Bonus;
using SabaPlus.Common;
using Tgs.ServiceUtility;

namespace SabaPlus.API.Services;

public class BonusServicee(ISabaPlusApiMessagePublishHandler messagePublisher) : IGetService<BonusRequest, BonusResult>
{

	public async ValueTask<BonusResult> GetAsync(BonusRequest query, CancellationToken cancellationToken = default)
	{
		try
		{
			var res = new BonusResult
			{
				Seq = Guid.NewGuid().ToString(),
				TimeStamp = DateTime.Now,
				ErrorCode = 0,
				ErrorMessage = "Success",
				Payload= new BonusPayload("cust id", 1212, string.Empty)
			};

			await messagePublisher.LogApi(new ApiLogMessage()
			{
				Action = "Bonus",
				Request = query,
				Response = res
			}, cancellationToken);

			return res;
		}
		catch (Exception ex)
		{
			Console.Write(ex.ToString());
			return new BonusResult
			{
				Seq = Guid.NewGuid().ToString(),
				ErrorCode = 1,
				ErrorMessage = "Account does not exist"
			};
		}
	}
}

