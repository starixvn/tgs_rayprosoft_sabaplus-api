namespace SabaPlus.Repositories.Models;

public class GetAdjustStakeTicketCriteria
{
	public string TicketNo { get; set; }
	public string GroupAction { get; set; }
	public decimal? ActualStake { get; set; }
	public decimal? Payout { get; set; }
}