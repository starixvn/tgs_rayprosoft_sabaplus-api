using System.Data;
using System.Security.AccessControl;
using Dapper;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Npgsql;
using SabaPlus.Core;
using SabaPlus.Core.Models;

namespace SabaPlus.Repositories;

internal class GameProviderRepository : IGameProviderRepository
{
	private readonly string connectionString;
	private readonly IMemoryCache memoryCache;
	private readonly CacheSettingsOptions cacheSettings;

	public GameProviderRepository(IConfiguration configuration, IMemoryCache memoryCache, IOptions<CacheSettingsOptions> cacheSettings)
	{
		connectionString = configuration.GetConnectionString("Tgs")!;
		this.memoryCache = memoryCache;
		this.cacheSettings = cacheSettings.Value;
	}

	public async Task<TResult> CallStoredProcedureAsync<TCriteria, TResult>(TCriteria criteria, string spName)
	{
		using (IDbConnection db = new NpgsqlConnection(connectionString))
		{
			var parameters = new DynamicParameters();

			foreach (var property in typeof(TCriteria).GetProperties())
			{
				var value = property.GetValue(criteria);
				parameters.Add(property.Name.ToLower(), value);
			}
			string sqlQuery = $"CALL {spName}({string.Join(", ", typeof(TCriteria).GetProperties().Select(p => $"{p.Name.ToLower()} => @{p.Name.ToLower()}"))})";

			var result = await db.QueryFirstAsync<TResult>(sqlQuery, parameters);

			return result;
		}
	}

	public async Task<IEnumerable<BetResultCheckTicket>> GetRunningTicketsAsync(int diffInMinute)
	{
		using (IDbConnection db = new NpgsqlConnection(connectionString))
		{
			string sqlQuery = $"select * from dbo.bet_bettrans_sabaplus_running_get(diff => {diffInMinute})";
			var result = await db.QueryAsync<BetResultCheckTicket>(sqlQuery);

			return result;
		}
	}

	public async Task<string?> GetGameProviderCodeByGameTypeAsync(string gameType)
	{
		string key = $"{typeof(GameProviderRepository).Namespace!.Replace(".", "-").ToLower()}:GameProviderCode:{gameType}";
		if (!memoryCache.TryGetValue(key, out string? gameProviderCode))
		{
			string sql = @"SELECT gp.code
							FROM dbo.sub_game_providers as sgp
							INNER JOIN dbo.game_providers as gp
							ON sgp.game_provider_id = gp.id
							WHERE UPPER(sgp.code) = UPPER(@gameType)
							AND gp.code LIKE 'SABAPLUS%'
							LIMIT 1;";
			var parameters = new { gameType };
			using (IDbConnection db = new NpgsqlConnection(connectionString))
			{
				gameProviderCode = await db.QueryFirstOrDefaultAsync<string>(sql, parameters);
			}

			if (gameProviderCode is not null)
			{
				return memoryCache.Set(key, gameProviderCode, TimeSpan.FromMinutes(cacheSettings.ExpiredTimeInMinute));
			}
		}

		return gameProviderCode;
	}

	public async Task<BetLimit?> GetBetLimitAsync(string gameProviderCode, string currencyCode)
	{
		string key = $"{typeof(GameProviderRepository).Namespace!.Replace(".", "-").ToLower()}:GameProviderBetLimit:{gameProviderCode}:{currencyCode}";
		if (!memoryCache.TryGetValue(key, out BetLimit? betLimit))
		{
			var sql = @"SELECT gpbl.min_bet AS MinBet,
						gpbl.max_bet AS MaxBet
					FROM dbo.game_provider_bet_limits as gpbl
					INNER JOIN dbo.game_providers as gp
						ON gp.id = gpbl.game_provider_id
					WHERE gpbl.currency_code = @currencyCode
						AND gp.code = @gameProviderCode
					LIMIT 1;";
			var parameters = new { gameProviderCode, currencyCode };
			using (IDbConnection db = new NpgsqlConnection(connectionString))
			{
				betLimit = await db.QueryFirstOrDefaultAsync<BetLimit>(sql, parameters);
			}

			if (betLimit is not null)
			{
				return memoryCache.Set(key, betLimit, TimeSpan.FromMinutes(cacheSettings.ExpiredTimeInMinute));
			}
		}

		return betLimit;
	}

	public async Task<string?> GetCurrencyCodeAsync(string gameProviderCode, string currencyCode)
	{
		string key = $"{typeof(GameProviderRepository).Namespace!.Replace(".", "-").ToLower()}:GameProviderCurrencyCode:{gameProviderCode}:{currencyCode}";
		if (!memoryCache.TryGetValue(key, out string? gameProviderCurrencyCode))
		{
			var sql = @"SELECT gpc.game_provider_currency_code
					FROM dbo.game_provider_currencies as gpc
					INNER JOIN dbo.game_providers as gp
						ON gp.id = gpc.game_provider_id
					WHERE gpc.currency_code = @currencyCode
						AND gp.code = @gameProviderCode
					LIMIT 1;";
			var parameters = new { gameProviderCode, currencyCode };
			using (IDbConnection db = new NpgsqlConnection(connectionString))
			{
				gameProviderCurrencyCode = await db.QueryFirstOrDefaultAsync<string>(sql, parameters);
			}

			if (gameProviderCurrencyCode is not null)
			{
				return memoryCache.Set(key, gameProviderCurrencyCode, TimeSpan.FromMinutes(cacheSettings.ExpiredTimeInMinute));
			}
		}

		return gameProviderCurrencyCode;
	}
}