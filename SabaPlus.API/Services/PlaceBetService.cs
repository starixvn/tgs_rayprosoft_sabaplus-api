using SabaPlus.API.Messages;
using SabaPlus.API.Models.PlaceBet;
using SabaPlus.Common;
using Tgs.ServiceUtility;

namespace SabaPlus.API.Services;

public class PlaceBetGetService(ISabaPlusApiMessagePublishHandler messagePublisher) : IGetService<PlaceBetRequest, PlaceBetResult>
{
	public async ValueTask<PlaceBetResult> GetAsync(PlaceBetRequest query, CancellationToken cancellationToken = default)
	{
		try
		{
			var res = new PlaceBetResult("0", "4200500_2331", "1988754", "");

			var apiLogMessage = new ApiLogMessage()
			{
				Key = query.key,
				Action = query.message.Action,
				Request = query,
				Response = res
			};

			await messagePublisher.LogApi(new ApiLogMessage()
			{
				Key = query.key,
				Action = query.message.Action,
				Request = query,
				Response = res
			}, cancellationToken);

			//TO DO
			//#1. Check Valid Request
			//#2. Check Valid Bet Amount
			//#3. Check Valid Balance
			//#4. Define PlaceBet Error Codes
			//#5. Schedule to trigger CheckTicketStatus if not receive ConfirmBet after 10 mins

			return res;
		}
		catch (Exception ex)
		{
			Console.Write(ex.ToString());
			return new PlaceBetResult("1", "", "", "");
		}
	}
}