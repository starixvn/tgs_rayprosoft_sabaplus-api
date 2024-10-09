namespace SabaPlus.Repositories.Models;

public class GetAdjustBalanceTicketSPResult
{
	public decimal BalanceAdjust { get; set; }
	public string? StatusCode { get; set; }
	public string? ErrMsg { get; set; }
}