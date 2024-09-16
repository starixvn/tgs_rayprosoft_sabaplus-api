using MediatR;
using SabaPlus.API.Messages;
using SabaPlus.API.Models.FreeSpin;
using SabaPlus.Common;

namespace SabaPlus.API.Services;

public class FreeSpinHandler(ISabaPlusApiMessagePublishHandler messagePublisher) : IRequestHandler<FreeSpinRequest, FreeSpinResult>
{
	public async Task<FreeSpinResult> Handle(FreeSpinRequest query, CancellationToken cancellationToken = default)
	{
		try
		{
			var res = new FreeSpinResult
			{
				ErrorCode = 0,
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
				ErrorCode = 1
			};
		}
	}
}