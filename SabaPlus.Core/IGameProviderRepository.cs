namespace SabaPlus.Core;

public interface IGameProviderRepository
{
	Task<TResult> CallStoredProcedureAsync<TCriteria, TResult>(TCriteria criteria, string spName);

	Task<string?> GetGameProviderCodeByGameTypeAsync(string gameType);
}