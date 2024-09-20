using MediatR;

namespace SabaPlus.API.Models.PlaceBetAndSettle;

public class PlaceBetAndSettleRequest : GameProviderRequest<PlaceBetAndSettleRequestPayload>, IRequest<PlaceBetAndSettleResult>;

public record PlaceBetAndSettleRequestPayload(string OperatorCustId, string WalletSideToken, string GameType, string? IpAddress, PlaceBetAndSettleDataSet[] BetDataSet)
{
	public string GameProviderCode => WalletSideToken.Split('@')[0];
};

public class PlaceBetAndSettleDataSet
{
	public string TicketNo { get; set; }

	public string GameId { get; set; }

	public string GameRoundId { get; set; }

	public string? TransactionId { get; set; }

	public decimal Stake { get; set; }

	public decimal ActualStake { get; set; }

	public decimal NetStake { get; set; }

	public decimal Payout { get; set; }

	public string SettleDate { get; set; }

	public string SettleDateWithTz { get; set; }

	public string? BetChoice { get; set; }
}