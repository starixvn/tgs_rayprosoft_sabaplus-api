using Tgs.GameInterface.SabaPlus.Domains.BetResultCheck;

namespace SabaPlus.API.Models.BetResultCheck;

public record BetResultCheckData
{
	public string TicketNo { get; set; }

	public string OperatorCustId { get; set; }

	public string GameRoundId { get; set; }

	public string? TransactionId { get; set; }

	public short Status { get; set; }

	public decimal Stake { get; set; }

	public decimal ActualStake { get; set; }

	public decimal NetStake { get; set; }

	public decimal Payout { get; set; }

	public string? SettleDate { get; set; }

	public string? TransDate { get; set; }

	public string? GameResult { get; set; }

	public decimal? Odds { get; set; }

	public Dictionary<string, string>? OtherBetInfo { get; set; }
}

public record BetResultCheckFromProvider
{
	public BetResultCheckData Result { get; set; } = default!;

	public BetResultCheckQuery Query { get; set; } = default!;
}