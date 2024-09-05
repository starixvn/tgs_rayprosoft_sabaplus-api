namespace SabaPlus.API.Models.Settle;

public class EndGameResult : GameProviderResponse<EndGameResponsePayload>;

public record EndGameResponsePayload(string OperatorCustId, decimal Balance);