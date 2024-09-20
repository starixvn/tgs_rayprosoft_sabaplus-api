namespace SabaPlus.API.Models.PlaceBet
{
	public class PlaceBetResult : GameProviderResponse<PlaceBetResponsePayload>;

	public record PlaceBetResponsePayload(string OperatorCustId, decimal Balance, PlaceBetResultDataSet[] BetDataSet);

	public class PlaceBetResultDataSet
	{
		public string TicketNo { get; set; }

		public string RefNo { get; set; }
	}
}