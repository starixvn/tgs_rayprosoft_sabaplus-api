

namespace SabaPlus.API.Models.Bonus;

public class BonusRequest: OperatorRequest<BonusRequestPayload>;

public record BonusRequestPayload(string OperatorCustId, string GameType, BonusDataSet[] BetDataSet);

public class BonusDataSet 
{
	public string TicketNo { get; set; }

	public string GameId { get; set; }

	public string BonusId { get; set; }

	public string TransactionId { get; set; }

	public string GameRoundId { get; set; }

	public string BonusName { get; set; }

	public decimal Payout { get; set; }

	public string CurrencyISO { get; set; }

	public string PrizeName { get; set; }

	public string SettleDate { get; set; }
}
