using MediatR;

namespace SabaPlus.API.Models.FreeSpin;

public class FreeSpinRequest : GameProviderRequest<FreeSpinRequestPayload>, IRequest<FreeSpinResult>;

public record FreeSpinRequestPayload(string OperatorCustId, string GameType, string IpAddress, FreeSpinDataSet[] BetDataSet);

public class FreeSpinDataSet
{
	public string TicketNo { get; set; }

	public string GameId { get; set; }

	public string GameRoundId { get; set; }

	public string? TransactionId { get; set; }

	public decimal Stake { get; set; } = 0;

	public decimal ActualStake { get; set; } = 0;

	public decimal NetStake { get; set; } = 0;

	public decimal Payout { get; set; }

	public string SettleDate { get; set; }

	public string? SettleDateWithTz { get; set; }

	public Dictionary<string, string>? OtherBetInfo { get; set; }
}