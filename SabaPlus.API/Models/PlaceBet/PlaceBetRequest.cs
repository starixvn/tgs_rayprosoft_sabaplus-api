using MediatR;

namespace SabaPlus.API.Models.PlaceBet;

public class PlaceBetRequest : GameProviderRequest<PlaceBetRequestPayload>, IRequest<PlaceBetResult>;

public record PlaceBetRequestPayload(string? OperatorCustId, string? WalletSideToken, string? GameType, string? IpAddress, PlaceBetDataSet[] BetDataSet)
{
	public string? GameProviderCode => WalletSideToken?.Split('@')[0];
	public string? S1GameId => WalletSideToken?.Split('@')[1];
};

public class PlaceBetDataSet
{
	public string? TicketNo { get; set; }

	public string? GameId { get; set; }

	public string? GameRoundId { get; set; }

	public string? TransactionId { get; set; }

	public decimal? Stake { get; set; }

	public decimal? ActualStake { get; set; }

	public decimal? Odds { get; set; }

	public string? BetChoice { get; set; }

	public string? TableCode { get; set; }

	public Dictionary<string, string>? OtherBetInfo { get; set; }
}