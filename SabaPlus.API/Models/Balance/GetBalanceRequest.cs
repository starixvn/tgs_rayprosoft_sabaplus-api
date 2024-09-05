namespace SabaPlus.API.Models.Balance;

public class GetBalanceRequest : GameProviderRequest<GetBalanceRequestPayload>;

public record GetBalanceRequestPayload (string OperatorCustId, string WalletSideToken, string GameType);