using MediatR;
using SabaPlus.Common;

namespace SabaPlus.API.Models.VoidGame;

public class VoidGameRequest : GameProviderRequest<VoidGameRequestPayload>, IRequest<VoidGameResult>;

public record VoidGameRequestPayload(string OperatorCustId, string GameType, VoidGameRequestDataSet[] BetDataSet)
{
	public string Action { get; set; } = ActionConstants.VoidGame;
}

public class VoidGameRequestDataSet
{
	public int Status { get; set; }

	public string TicketNo { get; set; }

	public string GameId { get; set; }

	public string GameRoundId { get; set; }

	public string? TransactionId { get; set; }
}