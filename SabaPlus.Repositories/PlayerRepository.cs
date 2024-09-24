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

	public async ValueTask<PlayerInfo?> GetPlayerInfoByPlayerCodeAsync(string playerCode, string gameProviderCode, CancellationToken cancellationToken = default)
	{
		string key = $"PlayerInfo:{playerCode}:{gameProviderCode}";
		if (!memoryCache.TryGetValue(key, out PlayerInfo? playerInfo))
		{
			using (IDbConnection db = new NpgsqlConnection(connectionString))
			{
				string sql = @"
							SELECT p.player_code AS PlayerCode,
								p.username AS Username,
								p.default_wallet_currency_code AS CurrencyCode,
								p.brand_id AS BrandId
							FROM dbo.players as p
							WHERE p.player_code = @playerCode
							LIMIT 1;";

				var parameters = new { playerCode, gameProviderCode };
				playerInfo = await db.QueryFirstOrDefaultAsync<PlayerInfo>(sql, parameters);
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
				string sql = @"
							SELECT bl.min_bet AS MinBet,
								bl.max_bet AS MaxBet
							FROM dbo.brand_bet_limits as bbl
							INNER JOIN dbo.bet_limits as bl
								ON bbl.bet_limit_id = bl.id
							WHERE bl.currency_code = @currencyCode
								AND bbl.brand_id = @brandId
							LIMIT 1;";

				var parameters = new { brandId, currencyCode };
				betLimit = await db.QueryFirstOrDefaultAsync<BetLimit>(sql, parameters);
			}

			if (betLimit is not null)
			{
				return memoryCache.Set(key, betLimit, TimeSpan.FromMinutes(cacheSettings.ExpiredTimeInMinute));
			}
		}

		return betLimit;
	}
}