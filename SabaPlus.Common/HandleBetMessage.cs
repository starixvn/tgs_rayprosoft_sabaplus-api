namespace SabaPlus.Common;

public class HandleBetMessage
{
	public string GameProviderCode { get; set; } = "";
	public string GameExternalId { get; set; } = "";
	public string Action { get; set; } = "";
	public string PlayerCode { get; set; } = "";
	public string ExtraStatus { get; set; } = "";
	public string BetState { get; set; } = "";
	public DateTime? WinlostDate { get; set; }
	public DateTime? TransactionTime { get; set; }
	public decimal ActualAmount { get; set; } = 0;
	public decimal Payout { get; set; } = 0;
	public string TxId { get; set; } = "";
	public string? RefId { get; set; } = "";
	public long LicenseeTxid { get; set; }
	public bool IsOnlyWinlostDateChanged { get; set; } = false;
}