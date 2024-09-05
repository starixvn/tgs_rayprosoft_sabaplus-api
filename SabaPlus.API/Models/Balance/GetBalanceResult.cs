namespace SabaPlus.API.Models.Balance;

public class GetBalanceResult : GameProviderResponse<GetBalanceResponsePayload>;

public record GetBalanceResponsePayload(decimal Balance);