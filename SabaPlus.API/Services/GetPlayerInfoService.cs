using SabaPlus.API.Messages;
using SabaPlus.API.Models.PlayerInfo;
using SabaPlus.Common;
using SabaPlus.Core;
using Tgs.CashService.Client;
using Tgs.ServiceUtility;

namespace SabaPlus.API.Services;

public class GetPlayerInfoService(
	ISabaPlusApiMessagePublishHandler messagePublisher,
	IPlayerRepository playerRepository,
	ICashServiceClient cashClient
	) : IGetService<GetPlayerInfoRequest, GetPlayerInfoResult>
{
	public async ValueTask<GetPlayerInfoResult> GetAsync(GetPlayerInfoRequest query, CancellationToken cancellationToken = default)
	{
		try
		{
			var playerCode = query.Payload.OperatorCustId;
			var playerInfo = await playerRepository.GetPlayerInfoByPlayerCodeAsync(playerCode, cancellationToken).ConfigureAwait(false);
			var balance = (await cashClient.GameProviderGetBalanceAsync(playerCode, cancellationToken).ConfigureAwait(false)).Balance;
			var res = new GetPlayerInfoResult
			{
				Payload = new GetPlayerInfoResponsePayload
				{
					OperatorCustId = playerInfo!.PlayerCode,
					UserName = playerInfo.Username,
					CurrencyISO = playerInfo.CurrencyCode,
					Currency = playerInfo.CurrencyCode,
					Balance = balance,
					WalletSideToken = query.Payload.WalletSideToken,
					// TODO: correct min, max & group bet
					MinBet = 1m,
					MaxBet = 100_000,
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
			SentrySdk.CaptureException(ex);
			return new GetPlayerInfoResult { ErrorCode = 1000, ErrorMessage = "Unspecified Error" };
		}
	}
}