using System.Text.Json;
using SabaPlus.Common;
using Tgs.GameRecord.Saba.Common;
using Valhalla.MessageQueue;

namespace SabaPlus.API.Messages;

public class SabaPlusMessagePublishHandler(IMessageSender messageSender) : ISabaPlusApiMessagePublishHandler
{
	public async ValueTask LogApi(ApiLogMessage mesage, CancellationToken cancellationToken = default)
	{
		await messageSender.PublishAsync(subject: SabaNatsConst.ApiLogsPublish, data: JsonSerializer.Serialize(mesage), cancellationToken);
	}
}