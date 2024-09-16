using SabaPlus.Common;
using SabaPlus.Repositories.Models;

namespace SabaPlus.API;

public static class HandleMessageMapper
{
	public static HandleBetMessage PlaceBet(PlaceBetCriteria criteria)
	{
		return new HandleBetMessage()
		{
			GameProviderCode = criteria.GameProviderCode,
			GameExternalId = criteria.ExternalGameId,
			Action = "ConfirmBet",
			PlayerCode = criteria.OperatorCustId,
			TxId = criteria.GameRoundId,
			LicenseeTxid = 0, // todo: LicenseeTxid
			TransactionTime = criteria.TransDate
		};
	}

	public static HandleBetMessage EndGame(EndGameCriteria criteria)
	{
		return new HandleBetMessage()
		{
			GameProviderCode = criteria.GameProviderCode,
			GameExternalId = criteria.ExternalGameId,
			Action = "Settle",
			PlayerCode = criteria.OperatorCustId,
			TxId = criteria.GameRoundId,
			RefId = "", // todo: RefId
			Payout = criteria.Payout ?? 0,
			ExtraStatus = "",
			WinlostDate = criteria.SettleDate,
		};
	}
}