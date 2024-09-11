using System.Security.Cryptography;
using System.Transactions;
using Newtonsoft.Json;
using SabaPlus.API.Messages;
using SabaPlus.API.Models.PlaceBet;
using SabaPlus.Common;
using Tgs.ServiceUtility;

namespace SabaPlus.API.Services;

public class PlaceBetGetService(ISabaPlusApiMessagePublishHandler messagePublisher) : IGetService<PlaceBetRequest, PlaceBetResult>
{
	public async ValueTask<PlaceBetResult> GetAsync(PlaceBetRequest request, CancellationToken cancellationToken = default)
	{
		try
		{
			var betDataSets = JsonConvert.DeserializeObject<PlaceBetResultDataSet[]>(JsonConvert.SerializeObject(request.Payload.BetDataSet));

			foreach (var betData in betDataSets)
			{
				betData.RefNo = Guid.NewGuid().ToString();
			}

			var response = new PlaceBetResult
			{
				Payload = new PlaceBetResponsePayload("123456798", 123, betDataSets)
			};

			var apiLogMessage = new ApiLogMessage()
			{
				Action = "PlaceBet",
				Request = request,
				Response = response
			};

			await messagePublisher.LogApi(new ApiLogMessage()
			{
				Action = apiLogMessage.Action,
				Request = request,
				Response = response
			}, cancellationToken);

			return response;
		}
		catch (Exception ex)
		{
			Console.Write(ex.ToString());
			return new PlaceBetResult { ErrorCode = 1, ErrorMessage = "Place bet failed" };
		}
	}
}