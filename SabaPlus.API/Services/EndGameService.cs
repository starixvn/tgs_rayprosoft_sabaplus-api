using SabaPlus.API.Messages;
using SabaPlus.API.Models.EndGame;
using SabaPlus.Common;
using Tgs.ServiceUtility;

namespace SabaPlus.API.Services;

public class EndGameService(ISabaPlusApiMessagePublishHandler messagePublisher) : IGetService<EndGameRequest, EndGameResult>
{
	public async ValueTask<EndGameResult> GetAsync(EndGameRequest query, CancellationToken cancellationToken = default)
	{
		try
		{
			var res = new EndGameResult
			{
				TimeStamp	 = "2023/08/01 07:48:24.040",
				Payload = new EndGameResponsePayload("cust id", 1212)
			};

			await messagePublisher.LogApi(new ApiLogMessage()
			{
				
				Action = "Endgame",
				Request = query,
				Response = res
			}, cancellationToken);

			return res;
		}
		catch (Exception ex)
		{
			Console.Write(ex.ToString());
			return new EndGameResult { ErrorCode = 1, ErrorMessage = "Endgame failed"};
		}
	}
}