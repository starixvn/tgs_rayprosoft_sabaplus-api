using SabaPlus.Common;

namespace SabaPlus.Repositories.Models;

public class PlaceBetAndSettleCriteria
{
	public string Seq { get; set; }
	public string OperatorCustId { get; set; }
	public string PlayerCode => OperatorCustId.ToPlayerCode();
	public string TicketNo { get; set; }
	public string Action => ActionConstants.PlaceBetAndSettle;
	public string GameProviderCode { get; set; }
	public string GameType { get; set; }
	public string GameRoundId { get; set; }
	public string ExternalGameId { get; set; }
	public string? IpAddress { get; set; }
	public DateTime TransDate { get; set; }
	public DateTime SettleDate { get; set; }
	public string? TransactionId { get; set; }
	public decimal? Stake { get; set; }
	public decimal ActualStake { get; set; }
	public decimal NetStake { get; set; }
	public decimal Payout { get; set; }
	public string? BetChoice { get; set; }
}