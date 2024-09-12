using SabaPlus.API.Messages;
using SabaPlus.API.Models.EndGame;
using SabaPlus.Common;
using Tgs.ServiceUtility;

namespace SabaPlus.API.Services;

public class EndGameService(ISabaPlusApiMessagePublishHandler messagePublisher) : IGetService<EndGameRequest, EndGameResult>
{
	public async ValueTask<EndGameResult> GetAsync(EndGameRequest request, CancellationToken cancellationToken = default)
	{
		try
		{
			var response = new EndGameResult
			{
				Payload = new EndGameResponsePayload(request.Payload.OperatorCustId, 1212m)
			};

			await messagePublisher.LogApi(new ApiLogMessage()
			{
				Action = "Endgame",
				Request = request,
				Response = response
			}, cancellationToken);

			return response;
		}
		catch (Exception ex)
		{
			Console.Write(ex.ToString());
			return new EndGameResult { ErrorCode = 1 };
		}
	}
}