using SabaPlus.Common;

namespace SabaPlus.Repositories.Models;

public class EndGameCriteria
{
	public string? Seq { get; set; }
	public string? OperatorCustId { get; set; }
	public string? TicketNo { get; set; }
	public string? Action => ActionConstants.EndGame;
	public string? GameProviderCode { get; set; }
	public string? GameType { get; set; }
	public string? GameRoundId { get; set; }
	public string? ExternalGameId { get; set; }
	public DateTime SettleDate { get; set; }
	public string? TransactionId { get; set; }
	public decimal? NetStake { get; set; }
	public decimal? ActualStake { get; set; }
	public decimal? Odds { get; set; }
	public decimal? Payout { get; set; }
	public string? GameResult { get; set; }
	public string? ResultId { get; set; }
	public string? ExtraInfo { get; set; }
}