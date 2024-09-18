using System.Text.Json;
using SabaPlus.Common;
using Tgs.GameRecord.Saba.Common;
using Valhalla.MessageQueue;

namespace SabaPlus.API.Messages;

public class SabaPlusMessagePublishHandler(IMessageSender messageSender, IConfiguration configuration) : ISabaPlusApiMessagePublishHandler
{
	public async ValueTask LogApi(ApiLogMessage mesage, CancellationToken cancellationToken = default)
	{
		await messageSender.PublishAsync(subject: Constants.ApiLogsExchange, data: JsonSerializer.Serialize(mesage), cancellationToken);
	}

	public async Task HandleBetAsync(HandleBetMessage mesage, CancellationToken cancellationToken = default)
	{
		var env = configuration.GetValue<string>("env");
		await messageSender.PublishAsync(subject: $"{Constants.HandleBetPublish}.{env}", data: JsonSerializer.Serialize(mesage), cancellationToken);
	}
}