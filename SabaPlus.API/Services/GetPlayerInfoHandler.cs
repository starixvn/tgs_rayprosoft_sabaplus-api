using MediatR;
using SabaPlus.API.Models;
using SabaPlus.API.Models.PlayerInfo;
using SabaPlus.API.Services.Shared;
using SabaPlus.Common;
using SabaPlus.Core;
using Tgs.CashService.Client;

namespace SabaPlus.API.Services;

public class GetPlayerInfoHandler(
	IPlayerRepository playerRepository,
	IGameProviderRepository gameProviderRepository,
	ICashServiceClient cashClient,
	ISentryClient sentryClient
	) : IRequestHandler<GetPlayerInfoRequest, GetPlayerInfoResult>
{
	public async Task<GetPlayerInfoResult> Handle(GetPlayerInfoRequest query, CancellationToken cancellationToken = default)
	{
		try
		{
			var playerCode = query.Payload.OperatorCustId.ToPlayerCode();
			var balanceResult = (await cashClient.GameProviderGetBalanceAsync(playerCode, cancellationToken).ConfigureAwait(false));

			if (!string.IsNullOrEmpty(balanceResult.ErrorMessage))
			{
				return ResultHelper.GenerateFailedResult<GetPlayerInfoResult, GetPlayerInfoResponsePayload>(balanceResult.ErrorCode, balanceResult.ErrorMessage);
			}

			var gameProviderCode = query.Payload.WalletSideToken.Split('@')[0];
			var playerInfo = await playerRepository.GetPlayerInfoByPlayerCodeAsync(playerCode, cancellationToken).ConfigureAwait(false);
			if (playerInfo == null)
			{
				return ResultHelper.GenerateFailedResult<GetPlayerInfoResult, GetPlayerInfoResponsePayload>(
					ErrorMapper.AccountNotExist,
					ErrorMapper.GetErrorMessage(ErrorMapper.AccountNotExist));
			}
			var gameProviderCurrencyCode = await gameProviderRepository.GetCurrencyCodeAsync(gameProviderCode, playerInfo.CurrencyCode).ConfigureAwait(false);
			var gameProviderBetLimit = await gameProviderRepository.GetBetLimitAsync(gameProviderCode, playerInfo.CurrencyCode).ConfigureAwait(false);
			var brandBetLimit = await playerRepository.GetBrandBetLimitAsync(playerInfo.BrandId, playerInfo.CurrencyCode).ConfigureAwait(false);
			var res = new GetPlayerInfoResult()
			{
				Payload = new GetPlayerInfoResponsePayload
				{
					OperatorCustId = query.Payload.OperatorCustId,
					UserName = playerInfo.Username,
					CurrencyISO = gameProviderCurrencyCode,
					Currency = gameProviderCurrencyCode,
					Balance = balanceResult.Balance,
					WalletSideToken = query.Payload.WalletSideToken,
					MinBet = Math.Max(gameProviderBetLimit.MinBet, brandBetLimit.MinBet),
					MaxBet = Math.Min(gameProviderBetLimit.MaxBet, brandBetLimit.MaxBet),
					// TODO: group bet
					//BetGroup = "BetGroup"
				}
			};
			return res;
		}
		catch (Exception ex)
		{
			sentryClient.CaptureException(ex);
			return new GetPlayerInfoResult { ErrorCode = ErrorMapper.UnspecifiedError };
		}
	}
}