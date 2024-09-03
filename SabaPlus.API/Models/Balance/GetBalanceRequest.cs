namespace SabaPlus.API.Models.Balance;

public class GetBalanceRequest : OperatorRequest<GetBalanceRequestPayload>;

public record GetBalanceRequestPayload (string OperatorCustId, string WalletSideToken, string GameType);