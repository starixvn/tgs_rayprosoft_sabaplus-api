using SabaPlus.Core.Models;

namespace SabaPlus.Core;

public interface IPlayerRepository
{
	ValueTask<PlayerInfo?> GetPlayerInfoByPlayerCodeAsync(string playerCode, CancellationToken cancellationToken = default);

	ValueTask<BetLimit?> GetBrandBetLimitAsync(Guid brandId, string currencyCode);
}