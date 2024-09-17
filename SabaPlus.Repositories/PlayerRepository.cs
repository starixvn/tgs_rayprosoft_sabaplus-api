using System.Data;
using Dapper;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using Npgsql;
using SabaPlus.Core;
using SabaPlus.Core.Models;

namespace SabaPlus.Repositories;

internal class PlayerRepository : IPlayerRepository
{
	private readonly string connectionString;
	private readonly IMemoryCache memoryCache;
	private readonly CacheSettingsOptions cacheSettings;

	public PlayerRepository(IConfiguration configuration, IMemoryCache memoryCache)
	{
		connectionString = configuration.GetConnectionString("Tgs")!;
		this.memoryCache = memoryCache;
		cacheSettings = new CacheSettingsOptions(60);
	}

	public async ValueTask<PlayerInfo?> GetPlayerInfoByPlayerCodeAsync(string playerCode, string gameProviderCode, CancellationToken cancellationToken = default)
	{
		string key = $"{typeof(PlayerRepository).Namespace!.Replace(".", "-").ToLower()}:PlayerInfo:{playerCode}";
		if (!memoryCache.TryGetValue(key, out PlayerInfo? playerInfo))
		{
			using (IDbConnection db = new NpgsqlConnection(connectionString))
			{
				string sql = @"
							SELECT
								p.player_code AS PlayerCode,
								p.username AS Username,
								gpc.game_provider_currency_code AS CurrencyCode
							FROM dbo.Players as p
							INNER JOIN dbo.game_providers as gp
							ON gp.code = @gameProviderCode
							INNER JOIN dbo.game_provider_currencies as gpc
							ON gp.id = gpc.game_provider_id
							AND gpc.currency_code = p.default_wallet_currency_code
							WHERE p.player_code = @playerCode;";

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
}