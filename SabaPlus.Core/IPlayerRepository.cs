using SabaPlus.Core.Models;

namespace SabaPlus.Core;

public interface IPlayerRepository
{
	ValueTask<PlayerInfo?> GetPlayerInfoByPlayerCodeAsync(string playerCode, string gameProviderCode, CancellationToken cancellationToken = default);

	ValueTask<(decimal, decimal)> GetMinAndMaxBetAsync(string playerCode, string gameProviderCode, CancellationToken cancellationToken = default);
}