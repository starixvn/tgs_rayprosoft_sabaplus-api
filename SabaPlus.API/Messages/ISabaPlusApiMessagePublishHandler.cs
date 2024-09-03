using SabaPlus.Common;

namespace SabaPlus.API.Messages;

public interface ISabaPlusApiMessagePublishHandler
{
	ValueTask LogApi(ApiLogMessage mesage, CancellationToken cancellationToken = default);
}