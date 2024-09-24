namespace SabaPlus.Repositories.Helpers;

public static class StoreProcedureMapper
{
	public static readonly string InsertSignalSP = "dbo.bet_bettrans_sabaplus_bet";
	public static readonly string GetTicketAmountSP = "dbo.bet_bettrans_sabaplus_get";
	public static readonly string InsertBetRoundSP = "dbo.bet_rounds_sabaplus_set";
}