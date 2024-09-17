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
}