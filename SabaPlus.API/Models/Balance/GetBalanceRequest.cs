using MediatR;
using SabaPlus.API.Services._Shared;

namespace SabaPlus.API.Models.Balance;

[IgnoreLogging]
public class GetBalanceRequest : GameProviderRequest<GetBalanceRequestPayload>, IRequest<GetBalanceResult>;

public record GetBalanceRequestPayload(string OperatorCustId, string WalletSideToken, string GameType);