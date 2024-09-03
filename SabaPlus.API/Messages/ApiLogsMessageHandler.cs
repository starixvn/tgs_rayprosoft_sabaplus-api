using System.Text;
using System.Text.Json;
using SabaPlus.Common;
using SabaPlus.Core.ApplicationServices;
using Tgs.ServiceUtility;
using Valhalla.MessageQueue;

namespace SabaPlus.API.Messages;

public class ApiLogsMessageHandler(
	ILogger<ApiLogsMessageHandler> logger,
	ICommandService<ApiLogsCommand> logCommand) : IMessageHandler<string>
{
	public ValueTask HandleAsync(string subject, string data, IEnumerable<MessageHeaderValue>? headerValues, CancellationToken cancellationToken = default)
	{
		var message = JsonSerializer.Deserialize<ApiLogMessage>(data)
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

		return logCommand.ExecuteAsync(new ApiLogsCommand(message.Key, message.Action, message.Request, message.Response));
	}
}