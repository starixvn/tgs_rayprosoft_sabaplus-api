namespace SabaPlus.API.Models.PlaceBet
{
    public class PlaceBetResult : GameProviderResponse<PlaceBetResponsePayload>;

	public record PlaceBetResponsePayload(string OperatorCustId, decimal Balance, PlaceBetDataSet[] BetDataSet);
}
