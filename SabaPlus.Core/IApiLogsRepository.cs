namespace SabaPlus.Core;

public interface IApiLogsRepository
{
	ValueTask<bool> AddAsync(string action, object req, object res, CancellationToken cancellationToken = default);
}