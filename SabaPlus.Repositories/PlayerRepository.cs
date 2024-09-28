using System.Data;
using Dapper;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Npgsql;
using SabaPlus.Core;
using SabaPlus.Core.Models;

namespace SabaPlus.Repositories;

internal class PlayerRepository : IPlayerRepository
{
	private readonly string connectionString;
	private readonly IMemoryCache memoryCache;
	private readonly CacheSettingsOptions cacheSettings;

	public PlayerRepository(IConfiguration configuration, IMemoryCache memoryCache, IOptions<CacheSettingsOptions> cacheSettings)
	{
		connectionString = configuration.GetConnectionString("Tgs")!;
		this.memoryCache = memoryCache;
		this.cacheSettings = cacheSettings.Value;
	}

	public async ValueTask<PlayerInfo?> GetPlayerInfoByPlayerCodeAsync(string playerCode, CancellationToken cancellationToken = default)
	{
		string key = $"PlayerInfo:{playerCode}";
		if (!memoryCache.TryGetValue(key, out PlayerInfo? playerInfo))
		{
			using (IDbConnection db = new NpgsqlConnection(connectionString))
			{
				playerInfo = await db.QueryFirstOrDefaultAsync<PlayerInfo>(
					"SELECT * FROM dbo.sabaplus_player_get_player_info(@playerCode)",
					 new { playerCode });
			}

			if (playerInfo is not null)
			{
				return memoryCache.Set(key, playerInfo, TimeSpan.FromMinutes(cacheSettings.ExpiredTimeInMinute));
			}
		}

		return playerInfo;
	}

	public async ValueTask<BetLimit?> GetBrandBetLimitAsync(Guid brandId, string currencyCode)
	{
		string key = $"BrandBetLimit:{brandId}:{currencyCode}";
		if (!memoryCache.TryGetValue(key, out BetLimit? betLimit))
		{
			using (IDbConnection db = new NpgsqlConnection(connectionString))
			{
				betLimit = await db.QueryFirstOrDefaultAsync<BetLimit>(
					"SELECT * FROM dbo.brand_get_bet_limit_by_currency(@brandId, @currencyCode)",
					new { brandId, currencyCode });
			}

			if (betLimit is not null)
			{
				return memoryCache.Set(key, betLimit, TimeSpan.FromMinutes(cacheSettings.ExpiredTimeInMinute));
			}
		}

		return betLimit;
	}
}