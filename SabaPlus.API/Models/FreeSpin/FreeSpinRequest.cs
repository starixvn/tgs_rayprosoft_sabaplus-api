namespace SabaPlus.API.Models.FreeSpin;

public class FreeSpinRequest: GameProviderRequest<FreeSpinRequestPayload>;

public record FreeSpinRequestPayload(string OperatorCustId, string GameType, string IpAddress, FreeSpinRequestDataSet[] BetDataSet);

public class FreeSpinRequestDataSet
{
	public string TicketNo { get; set; }

	public string GameId { get; set; }

	public string GameRoundId { get; set; }

	public string TransactionId { get; set; }

	public decimal Stake { get; set; }

	public decimal ActualStake { get; set; }

	public decimal NetStake { get; set; }

	public decimal Payout { get; set; }

	public string SettleDate { get; set; }

}
