namespace SabaPlus.API.Models.PlaceBet
{
    public class PlaceBetResult : OperatorResponse<PlaceBetResponsePayload>;

	public record PlaceBetResponsePayload(string OperatorCustId, decimal Balance, PlaceBetDataSet[] BetDataSet);
}
