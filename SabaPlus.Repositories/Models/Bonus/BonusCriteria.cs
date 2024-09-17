using SabaPlus.Common;

namespace SabaPlus.Repositories.Models;

public class BonusCriteria
{
	public string? Seq { get; set; }
	public string? OperatorCustId { get; set; }
	public string? TicketNo { get; set; }
	public string? Action => ActionConstants.FreeSpin;
	public string? GameProviderCode { get; set; }

	//public string? GameId { get; set; }
	public string? GameType { get; set; }

	public string? GameRoundId { get; set; }
	public string? ExternalGameId { get; set; }
	public DateTime TransDate { get; set; }
	public DateTime SettleDate { get; set; }
	public string? TransactionId { get; set; }
	public decimal? Payout { get; set; }
	public string? CurrencyISO { get; set; }
	public string? BonusId { get; set; }
	public string? BoNusName { get; set; }
	public string? PrizeName { get; set; }
}