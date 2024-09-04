using SabaPlus.API.Messages;
using SabaPlus.API.Models.FreeSpin;
using SabaPlus.Common;
using Tgs.ServiceUtility;

namespace SabaPlus.API.Services;

public class FreeSpinService(ISabaPlusApiMessagePublishHandler messagePublisher) : IGetService<FreeSpinRequest, FreeSpinResult>
{
	public async ValueTask<FreeSpinResult> GetAsync(FreeSpinRequest query, CancellationToken cancellationToken = default)
	{
		try
		{
			var res = new FreeSpinResult
			{
				Seq = Guid.NewGuid().ToString(),
				TimeStamp = DateTime.Now,
				ErrorCode = 0,
				ErrorMessage = "Success",
				Payload = new FreeSpinPayload("11111", 123, new[] {
				new FreeSpinResponseDataSet {
					TicketNo = "2323",
					RefNo ="abde"
				} })
			};

			await messagePublisher.LogApi(new ApiLogMessage()
			{
				Action = "FreeSpin",
				Request = query,
				Response = res
			}, cancellationToken);

			return res;
		}
		catch (Exception ex)
		{
			Console.Write(ex.ToString());
			return new FreeSpinResult
			{
				Seq = Guid.NewGuid().ToString(),
				ErrorCode = 1,
				ErrorMessage = "Account does not exist"
			};
		}
	}
}
