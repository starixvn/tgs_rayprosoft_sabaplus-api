namespace SabaPlus.API.Models.EndGame;

public class EndGameResult : GameProviderResponse<EndGameResponsePayload>;

public record EndGameResponsePayload(string? OperatorCustId, decimal? Balance);