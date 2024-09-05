

namespace SabaPlus.API.Models.VoidGame;

public class VoidGameRequest : GameProviderRequest<VoidGameRequestPayload>;

public record VoidGameRequestPayload(string OperatorCustId, string GameType, VoidGameRequestDataSet[] BetDataSet);

public class VoidGameRequestDataSet
{
	public string TicketNo { get; set; }

	public string GameId { get; set; }

	public string GameRoundId { get; set; }

	public string TransactionId { get; set; }
}