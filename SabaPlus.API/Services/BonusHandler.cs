using MediatR;
using SabaPlus.API.Messages;
using SabaPlus.API.Models.Bonus;
using SabaPlus.Common;
using Tgs.ServiceUtility;

namespace SabaPlus.API.Services;

public class BonusHandler(ISabaPlusApiMessagePublishHandler messagePublisher) : IRequestHandler<BonusRequest, BonusResult>
{
	public async Task<BonusResult> Handle(BonusRequest request, CancellationToken cancellationToken = default)
	{
		try
		{
			var res = new BonusResult
			{
				ErrorCode = 0,
				Payload = new BonusPayload("cust id", 1212, string.Empty)
			};

			await messagePublisher.LogApi(new ApiLogMessage()
			{
				Action = "Bonus",
				Request = request,
				Response = res
			}, cancellationToken);

			return res;
		}
		catch (Exception ex)
		{
			Console.Write(ex.ToString());
			return new BonusResult
			{
				ErrorCode = 1,
			};
		}
	}
}