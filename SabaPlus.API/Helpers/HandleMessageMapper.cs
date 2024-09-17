using SabaPlus.Common;
using SabaPlus.Repositories.Models;

namespace SabaPlus.API;

public static class HandleMessageMapper
{
	public static HandleBetMessage PlaceBet(PlaceBetCriteria criteria, long refNo)
	{
		return new HandleBetMessage()
		{
			GameProviderCode = criteria.GameProviderCode,
			GameExternalId = criteria.ExternalGameId,
			Action = "ConfirmBet",
			PlayerCode = criteria.OperatorCustId,
			TxId = criteria.TicketNo,
			LicenseeTxid = refNo,
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

	public static HandleBetMessage PlaceBetAndSettle(PlaceBetAndSettleCriteria criteria, long refNo)
	{
		return new HandleBetMessage()
		{
			GameProviderCode = criteria.GameProviderCode,
			GameExternalId = criteria.ExternalGameId,
			Action = "PlaceBetAndSettle",
			PlayerCode = criteria.OperatorCustId,
			TxId = criteria.GameRoundId,
			LicenseeTxid = refNo,
			Payout = criteria.Payout ?? 0,
			WinlostDate = criteria.SettleDate,
			ExtraStatus = ActionConstants.PlaceBetAndSettle,
			TransactionTime = criteria.TransDate,
		};
	}

	public static HandleBetMessage FreeSpin(FreeSpinCriteria criteria, long refNo)
	{
		return new HandleBetMessage()
		{
			GameProviderCode = criteria.GameProviderCode,
			GameExternalId = criteria.ExternalGameId,
			Action = "PlaceBetAndSettle",
			PlayerCode = criteria.OperatorCustId,
			TxId = criteria.GameRoundId,
			LicenseeTxid = refNo,
			Payout = criteria.Payout ?? 0,
			WinlostDate = criteria.SettleDate,
			ExtraStatus = ActionConstants.FreeSpin,
			TransactionTime = criteria.TransDate,
		};
	}

	public static HandleBetMessage Bonus(BonusCriteria criteria, long refNo)
	{
		return new HandleBetMessage()
		{
			GameProviderCode = criteria.GameProviderCode,
			GameExternalId = criteria.ExternalGameId,
			Action = "PlaceBetAndSettle",
			PlayerCode = criteria.OperatorCustId,
			TxId = criteria.GameRoundId,
			LicenseeTxid = refNo,
			Payout = criteria.Payout ?? 0,
			WinlostDate = criteria.SettleDate,
			ExtraStatus = ActionConstants.Bonus,
			TransactionTime = criteria.TransDate,
		};
	}
}