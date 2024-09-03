using SabaPlus.Core.ApplicationServices;

namespace SabaPlus.Core;

public interface IApiLogsDispatcher
{
	ValueTask PushCommandAsync(ApiLogsCommand command, CancellationToken cancellationToken = default);
}