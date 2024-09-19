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
								gpc.game_provider_currency_code AS CurrencyCode,
								GREATEST(bl.min_bet, gpbl.min_bet) AS MinBet,
								LEAST(bl.max_bet, gpbl.max_bet) AS MaxBet
							FROM dbo.players as p
							INNER JOIN dbo.brand_bet_limits as bbl
								ON p.brand_id = bbl.brand_id
							INNER JOIN dbo.bet_limits as bl
								ON bbl.bet_limit_id = bl.id
								AND p.default_wallet_currency_code = bl.currency_code
							INNER JOIN dbo.game_provider_currencies as gpc
								ON gpc.currency_code = p.default_wallet_currency_code
							INNER JOIN dbo.game_providers as gp
								ON gp.id = gpc.game_provider_id
							INNER JOIN dbo.game_provider_bet_limits as gpbl
								ON gp.id = gpbl.game_provider_id
								AND gpbl.currency_code = gpc.currency_code
							WHERE p.player_code = @playerCode
								AND gp.code = @gameProviderCode
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

	public async ValueTask<(decimal, decimal)> GetMinAndMaxBetAsync(string playerCode, string gameProviderCode, CancellationToken cancellationToken = default)
	{
		var playerInfo = await GetPlayerInfoByPlayerCodeAsync(playerCode, gameProviderCode, cancellationToken);
		return (playerInfo.MinBet, playerInfo.MaxBet);
	}
}