using SabaPlus.API.Models.PlaceBet;

namespace SabaPlus.API.Models.Settle;

public class EndGameRequest : OperatorRequest<EndGameRequestPayload>;

public record EndGameRequestPayload(string OperatorCustId, string GameType, PlaceBetDataSet[] BetDataSet);

public class EndGameDataSet : PlaceBetDataSet
{
	public decimal NetStake { get; set; }

	public decimal Payout { get; set; }

	public string GameResult { get; set; }

	public DateTime SettleDate { get; set; }

	public string ResultId { get; set; }
}