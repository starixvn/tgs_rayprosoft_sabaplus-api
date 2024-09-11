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
				TimeStamp = DateTime.UtcNow.ToString(),
				Payload = new GetPlayerInfoResponsePayload {
					OperatorCustId = "123456798",
					UserName = "123456798",
					CurrencyISO = "THB",
					Currency = "THB",
					Balance = 789.00m,
					MinBet = 0m,
					MaxBet = 1000m,
					BetGroup = "BetGroup",
					WalletSideToken = "3nvkjdleenvjeoi3lfmwl"
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
