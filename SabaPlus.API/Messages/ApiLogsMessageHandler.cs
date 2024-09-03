using System.Text;
using System.Text.Json;
using SabaPlus.Common;
using SabaPlus.Core;
using SabaPlus.Core.ApplicationServices;
using Valhalla.MessageQueue;

namespace SabaPlus.API.Messages;

public class ApiLogsMessageHandler(
	ILogger<ApiLogsMessageHandler> logger,
	IApiLogsDispatcher dispatcher) : IMessageHandler<ReadOnlyMemory<byte>>
{
	public ValueTask HandleAsync(string subject, ReadOnlyMemory<byte> data, IEnumerable<MessageHeaderValue>? headerValues, CancellationToken cancellationToken = default)
	{
		var json = Encoding.UTF8.GetString(data.Span);
		var message = JsonSerializer.Deserialize<ApiLogMessage>(json)
			?? throw new NullReferenceException("The request data resolves to a null value");

		logger.LogInformation("{logInformation}",
			JsonSerializer.Serialize(new
			{
				LogOn = DateTime.Now.ToString("u"),
				Subject = subject,
				Data = new
				{
					message.Key,
					message.Action,
					message.Request,
					message.Response,
				},
				Message = "This message receiving handler."
			}));

		return dispatcher.PushCommandAsync(
			command: new ApiLogsCommand(message.Key, message.Action, message.Request, message.Response),
			cancellationToken: cancellationToken);
	}
}