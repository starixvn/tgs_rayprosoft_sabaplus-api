using SabaPlus.Core.Models;

namespace SabaPlus.Core;

public interface IPlayerRepository
{
	ValueTask<PlayerInfo?> GetPlayerInfoByPlayerCodeAsync(string playerCode, CancellationToken cancellationToken = default);
}