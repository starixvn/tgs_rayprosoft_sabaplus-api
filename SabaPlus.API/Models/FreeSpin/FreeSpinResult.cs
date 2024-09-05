namespace SabaPlus.API.Models.FreeSpin;

public class FreeSpinResult: GameProviderResponse<FreeSpinPayload>;

public record FreeSpinPayload(string OperatorCustId, decimal Balance, FreeSpinResponseDataSet[] BetDataSet);

public class FreeSpinResponseDataSet
{
	public string TicketNo { get; set; }

	public string RefNo { get; set; }
}