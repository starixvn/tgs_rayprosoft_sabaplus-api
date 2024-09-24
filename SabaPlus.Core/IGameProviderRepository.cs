using SabaPlus.Core.Models;

namespace SabaPlus.Core;

public interface IGameProviderRepository
{
	Task<TResult> CallStoredProcedureAsync<TCriteria, TResult>(TCriteria criteria, string spName);

	Task<string?> GetGameProviderCodeByGameTypeAsync(string gameType);

	public Task<IEnumerable<BetResultCheckTicket>> GetRunningTicketsAsync(int diffInMinute);

	Task<BetLimit?> GetBetLimitAsync(string gameProviderCode, string currencyCode);

	Task<string?> GetCurrencyCodeAsync(string gameProviderCode, string currencyCode);
}