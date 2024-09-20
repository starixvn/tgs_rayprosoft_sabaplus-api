namespace SabaPlus.API.Models.PlaceBetAndSettle;

public class PlaceBetAndSettleResult : GameProviderResponse<PlaceBetAndSettlePayload>;

public record PlaceBetAndSettlePayload(string OperatorCustId, decimal Balance, PlaceBetAndSettleResultDataSet[] BetDataSet);

public class PlaceBetAndSettleResultDataSet
{
	public string TicketNo { get; set; }

	public string RefNo { get; set; }
}