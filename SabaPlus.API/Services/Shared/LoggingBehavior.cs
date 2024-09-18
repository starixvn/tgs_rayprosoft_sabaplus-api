using MediatR;
using SabaPlus.API.Messages;
using SabaPlus.Common;

namespace SabaPlus.API.Services;

public class LoggingBehavior<TRequest, TResponse>(
		ISabaPlusApiMessagePublishHandler messagePublisher
		) : IPipelineBehavior<TRequest, TResponse>
{
	public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
	{
		var response = await next();

		_ = await GenerateResponseAsync(request, response, cancellationToken);

		return response;
	}

	private async Task<TResponse> GenerateResponseAsync(TRequest request, TResponse result, CancellationToken cancellationToken)
	{
		var action = request.GetType().Name.Replace("Request", string.Empty);
		var msg = new ApiLogMessage()
		{
			Action = action,
			Request = request,
			Response = result
		};

		await messagePublisher.LogApi(msg, cancellationToken);

		return result;
	}
}