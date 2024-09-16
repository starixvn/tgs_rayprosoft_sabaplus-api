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
			TxId = criteria.TicketNo,
			TransactionTime = criteria.TransDate,
			ExtraStatus = ActionConstants.PlaceBet,
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
			Payout = criteria.Payout ?? 0,
			WinlostDate = criteria.SettleDate,
			ExtraStatus = ActionConstants.EndGame,
		};
	}

	public static HandleBetMessage PlaceBetAndSettle(PlaceBetAndSettleCriteria criteria)
	{
		return new HandleBetMessage()
		{
			GameProviderCode = criteria.GameProviderCode,
			GameExternalId = criteria.ExternalGameId,
			Action = "Settle",
			PlayerCode = criteria.OperatorCustId,
			TxId = criteria.GameRoundId,
			Payout = criteria.Payout ?? 0,
			WinlostDate = criteria.SettleDate,
			ExtraStatus = ActionConstants.PlaceBetAndSettle,
		};
	}
}