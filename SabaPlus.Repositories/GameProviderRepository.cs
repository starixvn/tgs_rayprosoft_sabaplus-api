using System.Data;
using Dapper;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Npgsql;
using SabaPlus.Core;
using SabaPlus.Core.Models;

namespace SabaPlus.Repositories;

internal class GameProviderRepository : IGameProviderRepository
{
	private readonly string connectionString;
	private readonly IMemoryCache memoryCache;
	private readonly CacheSettingsOptions cacheSettings;

	public GameProviderRepository(IConfiguration configuration, IMemoryCache memoryCache)
	{
		connectionString = configuration.GetConnectionString("Tgs")!;
		this.memoryCache = memoryCache;
		cacheSettings = new CacheSettingsOptions(60);
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

	public async Task<string?> GetGameProviderCodeByGameIdAsync(string gameId)
	{
		string key = $"{typeof(GameProviderRepository).Namespace!.Replace(".", "-").ToLower()}:GameProviderCode:{gameId}";
		if (!memoryCache.TryGetValue(key, out string? providerCode))
		{
			string sql = @"SELECT gp.code
						FROM dbo.games as g
						INNER JOIN dbo.game_providers as gp
						ON g.game_provider_id = gp.id
						WHERE g.external_id = @gameId
						AND gp.code LIKE 'SABAPLUS%'";
			var parameters = new { gameId };
			using (IDbConnection db = new NpgsqlConnection(connectionString))
			{
				providerCode = await db.QueryFirstOrDefaultAsync<string>(sql, parameters);
			}

			if (providerCode is not null)
			{
				return memoryCache.Set(key, providerCode, TimeSpan.FromMinutes(cacheSettings.ExpiredTimeInMinute));
			}
		}

		return providerCode;
	}
}