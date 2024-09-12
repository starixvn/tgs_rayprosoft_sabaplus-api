using Newtonsoft.Json;
using SabaPlus.API.Messages;
using SabaPlus.API.Models.PlaceBetAndSettle;
using SabaPlus.Common;
using Tgs.ServiceUtility;

namespace SabaPlus.API.Services;

public class PlaceBetAndSettleService(ISabaPlusApiMessagePublishHandler messagePublisher) : IGetService<PlaceBetAndSettleRequest, PlaceBetAndSettleResult>
{
	public async ValueTask<PlaceBetAndSettleResult> GetAsync(PlaceBetAndSettleRequest request, CancellationToken cancellationToken = default)
	{
		try
		{
			var betDataSets = JsonConvert.DeserializeObject<PlaceBetAndSettleResponseDataSet[]>(JsonConvert.SerializeObject(request.Payload.BetDataSet));

			foreach (var betData in betDataSets)
			{
				betData.RefNo = (new Random()).Next(10000000).ToString();
			}

			var response = new PlaceBetAndSettleResult
			{
				ErrorCode = 0,
				Payload = new PlaceBetAndSettlePayload(request.Payload.OperatorCustId, 1555.5m, betDataSets)
			};

			await messagePublisher.LogApi(new ApiLogMessage()
			{
				Action = "PlaceBetAndSettle",
				Request = request,
				Response = response
			}, cancellationToken);

			return response;
		}
		catch (Exception ex)
		{
			Console.Write(ex.ToString());
			return new PlaceBetAndSettleResult { ErrorCode = 1 };
		}
	}
}