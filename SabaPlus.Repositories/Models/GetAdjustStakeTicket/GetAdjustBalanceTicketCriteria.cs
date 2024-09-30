namespace SabaPlus.Repositories.Models;

public class GetAdjustBalanceTicketCriteria
{
	public string OperatorCustId { get; set; }
	public string TicketNo { get; set; }
	public string GroupAction { get; set; }
	public decimal? ActualStake { get; set; }
	public decimal? Payout { get; set; }
}