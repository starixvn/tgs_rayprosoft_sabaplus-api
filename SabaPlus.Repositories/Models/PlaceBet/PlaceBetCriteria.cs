using SabaPlus.Common;

namespace SabaPlus.Repositories.Models;

public class PlaceBetCriteria
{
	public string? Seq { get; set; }
	public string? OperatorCustId { get; set; }
	public string? TicketNo { get; set; }
	public string? Action => ActionConstants.PlaceBet;
	public string? GameProviderCode { get; set; }

	//public string? GameId { get; set; }
	public string? GameType { get; set; }

	public string? GameRoundId { get; set; }
	public string? ExternalGameId { get; set; }
	public string? IpAddress { get; set; }
	public DateTime TransDate { get; set; }
	public string? TransactionId { get; set; }
	public decimal? Stake { get; set; }
	public decimal? ActualStake { get; set; }
	public decimal? Odds { get; set; }
	public string? BetChoice { get; set; }
	public string? TableCode { get; set; }
	public string? ExtraInfo { get; set; }
}