using System.Security.Cryptography;
using System.Transactions;
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
			var res = new PlaceBetResult
			{
				Seq = Guid.NewGuid().ToString(),
				TimeStamp = "2023/08/01 07:48:24.040",
				Payload = new PlaceBetResponsePayload("11111", 123, new PlaceBetDataSet[] {
				new PlaceBetDataSet {
					TicketNo = "2323",
					GameId = "game id",
					GameRoundId = " game round id",
					TransactionId = "transaction id",
					Stake = 12,
					ActualStake = 11,
					Odds = 0.98m,
					BetChoice = "choice 1",
					TableCode = " table 1"
				} })
			};

			var apiLogMessage = new ApiLogMessage()
			{
				Action = "PlaceBet",
				Request = query,
				Response = res
			};

			await messagePublisher.LogApi(new ApiLogMessage()
			{
				Action = apiLogMessage.Action,
				Request = query,
				Response = res
			}, cancellationToken);

			return res;
		}
		catch (Exception ex)
		{
			Console.Write(ex.ToString());
			return new PlaceBetResult { ErrorCode = 1, ErrorMessage= "Place bet failed"};
		}
	}
}