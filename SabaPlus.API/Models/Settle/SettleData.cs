namespace SabaPlus.API.Models.Settle;

public record SettleData
{
	public required string Action { get; set; } = "Settle"; // Default value for action

	public required string OperationId { get; set; }

	public required SettlementTransaction[] Txns { get; set; }
}

public record SettlementTransaction
{
	public required string UserId { get; set; }

	public required string RefId { get; set; }

	public required long TxId { get; set; }

	public required string UpdateTime { get; set; }

	public required string WinlostDate { get; set; }

	public required string Status { get; set; }

	public required decimal Payout { get; set; }

	public required decimal CreditAmount { get; set; }

	public required decimal DebitAmount { get; set; }

	public required string SettlementTime { get; set; }

	public string? ExtraStatus { get; set; } //cashout || void ticket || refund match || void cashout
}