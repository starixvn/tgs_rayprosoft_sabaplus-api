using MediatR;
using SabaPlus.API.Messages;
using SabaPlus.API.Models;
using SabaPlus.API.Models.PlayerInfo;
using SabaPlus.Common;
using SabaPlus.Core;
using Tgs.CashService.Client;

namespace SabaPlus.API.Services;

public class GetPlayerInfoHandler(
	ISabaPlusApiMessagePublishHandler messagePublisher,
	IPlayerRepository playerRepository,
	ICashServiceClient cashClient
	) : IRequestHandler<GetPlayerInfoRequest, GetPlayerInfoResult>
{
	public async Task<GetPlayerInfoResult> Handle(GetPlayerInfoRequest query, CancellationToken cancellationToken = default)
	{
		try
		{
			var playerCode = query.Payload.OperatorCustId;
			var balanceResult = (await cashClient.GameProviderGetBalanceAsync(playerCode, cancellationToken).ConfigureAwait(false));
			var res = new GetPlayerInfoResult();

			if (!string.IsNullOrEmpty(balanceResult.ErrorMessage))
			{
				res.ErrorCode = ErrorMapper.GetErrorCode(balanceResult.ErrorCode);
			}
			else
			{
				var gameProviderCode = query.Payload.WalletSideToken.Split('@')[0];
				var playerInfo = await playerRepository.GetPlayerInfoByPlayerCodeAsync(playerCode, gameProviderCode, cancellationToken).ConfigureAwait(false);

				res.Payload = new GetPlayerInfoResponsePayload
				{
					OperatorCustId = playerInfo!.PlayerCode,
					UserName = playerInfo.Username,
					CurrencyISO = playerInfo.CurrencyCode,
					Currency = playerInfo.CurrencyCode,
					Balance = balanceResult.Balance,
					WalletSideToken = query.Payload.WalletSideToken,
					// TODO: correct min, max & group bet
					MinBet = 1m,
					MaxBet = 100_000,
					//BetGroup = "BetGroup"
				};
			}

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
			return new GetPlayerInfoResult { ErrorCode = ErrorMapper.UnspecifiedError };
		}
	}
}