namespace SabaPlus.API.Models.Settle;

public class EndGameResult : OperatorResponse<EndGameResponsePayload>;

public record EndGameResponsePayload(string OperatorCustId, decimal Balance);