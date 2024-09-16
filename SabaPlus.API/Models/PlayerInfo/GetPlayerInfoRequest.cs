using MediatR;

namespace SabaPlus.API.Models.PlayerInfo;

public class GetPlayerInfoRequest : GameProviderRequest<GetPlayerInfoRequestPayload>, IRequest<GetPlayerInfoResult>;

public record GetPlayerInfoRequestPayload(string OperatorCustId, string GameType, string GameId, string WalletSideToken);