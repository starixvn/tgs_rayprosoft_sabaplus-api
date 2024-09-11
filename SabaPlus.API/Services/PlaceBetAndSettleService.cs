using SabaPlus.API.Messages;
using SabaPlus.API.Models.PlaceBetAndSettle;
using SabaPlus.Common;
using Tgs.ServiceUtility;

namespace SabaPlus.API.Services;

public class PlaceBetAndSettleService(ISabaPlusApiMessagePublishHandler messagePublisher) : IGetService<PlaceBetAndSettleRequest, PlaceBetAndSettleResult>
{
	public async ValueTask<PlaceBetAndSettleResult> GetAsync(PlaceBetAndSettleRequest query, CancellationToken cancellationToken = default)
	{
		try
		{
			var res = new PlaceBetAndSettleResult
			{
				Seq = Guid.NewGuid().ToString(),
				TimeStamp = DateTime.Now.ToString(),
				ErrorCode = 0,
				ErrorMessage = "Success",
				Payload = new PlaceBetAndSettlePayload("123456798", 123, new [] {
				new PlaceBetAndSettleResponseDataSet {
					TicketNo = Guid.NewGuid().ToString(),
					RefNo ="abde"
				} })
			};

			await messagePublisher.LogApi(new ApiLogMessage()
			{
				Action = "PlaceBetAndSettle",
				Request = query,
				Response = res
			}, cancellationToken);

			return res;
		}
		catch (Exception ex)
		{
			Console.Write(ex.ToString());
			return new PlaceBetAndSettleResult { ErrorCode = 1, ErrorMessage = "Place bet and settle failed" };
		}
	}
}