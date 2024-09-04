using SabaPlus.API.Messages;
using SabaPlus.API.Models.PlayerInfo;
using SabaPlus.Common;
using Tgs.ServiceUtility;

namespace SabaPlus.API.Services;

public class GetPlayerInfoService(ISabaPlusApiMessagePublishHandler messagePublisher) : IGetService<GetPlayerInfoRequest, GetPlayerInfoResult>
{
	public async ValueTask<GetPlayerInfoResult> GetAsync(GetPlayerInfoRequest query, CancellationToken cancellationToken = default)
	{
		try
		{
			var res = new GetPlayerInfoResult
			{
				Seq = Guid.NewGuid().ToString(),
				TimeStamp = DateTime.Now,
				Payload = new GetPlayerInfoResponsePayload {
					OperatorCustId = "123456798",
					UserName = "Player123456789",
					CurrencyISO = "USD",
					Currency = "USD",
					Balance = 100.00m,
					MinBet = 123.45m,
					MaxBet = 123.45m,
					BetGroup = "Bet ",
					WalletSideToken = "Wallet token"
				}

			};

			await messagePublisher.LogApi(new ApiLogMessage()
			{
				Action = "GetPlayerInfo",
				Request = query,
				Response = res
			}, cancellationToken);

			return res;
		}
		catch (Exception ex)
		{
			Console.Write(ex.ToString());
			return new GetPlayerInfoResult { ErrorCode = 1, ErrorMessage = "Get Player info failed" };
		}
	}
}
