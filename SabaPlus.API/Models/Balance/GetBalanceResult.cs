namespace SabaPlus.API.Models.Balance;

public class GetBalanceResult : OperatorResponse<GetBalanceResponsePayload>;

public record GetBalanceResponsePayload(decimal Balance);