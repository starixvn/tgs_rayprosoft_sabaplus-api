using SabaPlus.API.Messages;
using SabaPlus.API.Models.VoidGame;
using SabaPlus.Common;
using Tgs.ServiceUtility;

namespace SabaPlus.API.Services;

public class VoidGameService(ISabaPlusApiMessagePublishHandler messagePublisher) : IGetService<VoidGameRequest, VoidGameResult>
{
	public async ValueTask<VoidGameResult> GetAsync(VoidGameRequest query, CancellationToken cancellationToken = default)
	{
		try
		{
			var res = new VoidGameResult
			{
				ErrorCode = 0,
				Payload = new VoidGameResponsePayload
				{
					Balance = 100.00m,
					OperatorCustId = "123456789"
				}
			};

			await messagePublisher.LogApi(new ApiLogMessage()
			{
				Action = "Void Game",
				Request = query,
				Response = res
			}, cancellationToken);

			return res;
		}
		catch (Exception ex)
		{
			Console.Write(ex.ToString());
			return new VoidGameResult
			{
				ErrorCode = 1,
			};
		}
	}
}