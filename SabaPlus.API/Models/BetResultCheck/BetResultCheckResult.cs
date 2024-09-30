namespace SabaPlus.API.Models.BetResultCheck;

public record BetResultCheckTicketResult
{
	public string TicketNo { get; set; } = string.Empty;

	public string GameProviderCode { get; set; } = string.Empty;

	public bool Success { get; set; } = true;
}

public record BetResultCheckResult
{
	public IList<BetResultCheckTicketResult> Tickets { get; set; } = [];
}