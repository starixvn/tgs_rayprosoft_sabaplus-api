using System.Data;
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
		return await QueryAsync<TCriteria, TResult>(criteria, spName);
	}

	public async Task<TResult> SelectFunctionAsync<TCriteria, TResult>(TCriteria criteria, string functionName)
	{
		return await QueryAsync<TCriteria, TResult>(criteria, functionName, "SELECT");
	}

	private async Task<TResult> QueryAsync<TCriteria, TResult>(TCriteria criteria, string spName, string action = "CALL")
	{
		using (IDbConnection db = new NpgsqlConnection(connectionString))
		{
			var parameters = new DynamicParameters();

			foreach (var property in typeof(TCriteria).GetProperties())
			{
				var value = property.GetValue(criteria);
				parameters.Add(property.Name.ToLower(), value);
			}
			string sqlQuery = $"{action} {spName}({string.Join(", ", typeof(TCriteria).GetProperties().Select(p => $"{p.Name.ToLower()} => @{p.Name.ToLower()}"))})";

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
			using (IDbConnection db = new NpgsqlConnection(connectionString))
			{
				gameProviderCode = await db.QueryFirstOrDefaultAsync<string>(
					"SELECT dbo.sabaplus_get_provider_code_by_game_type(@game_type)"
					, new { game_type = gameType });
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
			using (IDbConnection db = new NpgsqlConnection(connectionString))
			{
				betLimit = await db.QueryFirstOrDefaultAsync<BetLimit>(
					"SELECT * FROM dbo.game_provider_get_bet_limit_by_currency(@gameProviderCode, @currencyCode)",
					new { gameProviderCode, currencyCode });
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
			using (IDbConnection db = new NpgsqlConnection(connectionString))
			{
				gameProviderCurrencyCode = await db.QueryFirstOrDefaultAsync<string>(
					"SELECT dbo.game_provider_get_currency_code_by_internal_code(@game_provider_code, @internal_code)",
					new { game_provider_code = gameProviderCode, internal_code = currencyCode });
			}

			if (gameProviderCurrencyCode is not null)
			{
				return memoryCache.Set(key, gameProviderCurrencyCode, TimeSpan.FromMinutes(cacheSettings.ExpiredTimeInMinute));
			}
		}

		return gameProviderCurrencyCode;
	}
}