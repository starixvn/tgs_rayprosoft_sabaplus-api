namespace SabaPlus.API.Models.PlaceBetAndSettle;

public class PlaceBetAndSettleRequest : GameProviderRequest<PlaceBetAndSettleRequestPayload>;

public record PlaceBetAndSettleRequestPayload(string OperatorCustId, string WalletSideToken, string GameType, string IpAddress, PlaceBetAndSettleRequestDataSet[] BetDataSet);

public class PlaceBetAndSettleRequestDataSet
{
	public string TicketNo { get; set; }

	public string GameId { get; set; }

	public string GameRoundId { get; set; }

	public string TransactionId { get; set; }

	public decimal Stake { get; set; }

	public decimal ActualStake { get; set; }

	public decimal NetStake { get; set; }

	public decimal Payout { get;set; }

	public string SettleDate { get; set; }

	public string BetChoice { get; set; }
}
