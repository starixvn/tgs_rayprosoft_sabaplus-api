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
			PlayerCode = criteria.OperatorCustId.ToPlayerCode(),
			TxId = criteria.TicketNo,
			LicenseeTxid = refNo,
			TransactionTime = criteria.TransDate,
			ExtraStatus = ActionConstants.PlaceBet,
		};
	}

	public static HandleBetMessage EndGame(EndGameCriteria criteria, long refNo)
	{
		return new HandleBetMessage()
		{
			GameProviderCode = criteria.GameProviderCode,
			GameExternalId = criteria.ExternalGameId,
			Action = "Settle",
			PlayerCode = criteria.OperatorCustId.ToPlayerCode(),
			TxId = criteria.TicketNo,
			RefId = refNo.ToString(),
			Payout = criteria.Payout,
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
			PlayerCode = criteria.OperatorCustId.ToPlayerCode(),
			TxId = criteria.TicketNo,
			LicenseeTxid = refNo,
			RefId = refNo.ToString(),
			Payout = criteria.Payout,
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
			PlayerCode = criteria.OperatorCustId.ToPlayerCode(),
			TxId = criteria.TicketNo,
			LicenseeTxid = refNo,
			RefId = refNo.ToString(),
			Payout = criteria.Payout,
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
			PlayerCode = criteria.OperatorCustId.ToPlayerCode(),
			TxId = criteria.TicketNo,
			LicenseeTxid = refNo,
			RefId = refNo.ToString(),
			Payout = criteria.Payout,
			WinlostDate = criteria.SettleDate,
			ExtraStatus = ActionConstants.Bonus,
			TransactionTime = criteria.TransDate,
		};
	}

	public static HandleBetMessage VoidGame(VoidGameCriteria criteria, long refNo)
	{
		return new HandleBetMessage()
		{
			GameProviderCode = criteria.GameProviderCode,
			GameExternalId = criteria.ExternalGameId,
			Action = "Settle",
			PlayerCode = criteria.OperatorCustId.ToPlayerCode(),
			TxId = criteria.TicketNo,
			RefId = refNo.ToString(),
			Payout = 0,
			WinlostDate = criteria.SettleDate,
			ExtraStatus = ActionConstants.VoidGame,
		};
	}
}