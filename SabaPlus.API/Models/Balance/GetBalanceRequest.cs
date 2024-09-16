using MediatR;

namespace SabaPlus.API.Models.Balance;

public class GetBalanceRequest : GameProviderRequest<GetBalanceRequestPayload>, IRequest<GetBalanceResult>;

public record GetBalanceRequestPayload(string OperatorCustId, string WalletSideToken, string GameType);