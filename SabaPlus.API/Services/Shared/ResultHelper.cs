using SabaPlus.API.Models;

namespace SabaPlus.API.Services.Shared;

public static class ResultHelper
{
	private static IConfiguration _configuration;

	public static void Initialize(IConfiguration configuration)
	{
		_configuration = configuration;
	}

	public static TResponse GenerateFailedResult<TResponse, TPayload>(string internalErrorCode, string errorMessage) where TResponse : GameProviderResponse<TPayload>, new()
	{
		var sentryLevel = GetSentryLevelFromConfig(internalErrorCode);
		SentrySdk.CaptureMessage($"{internalErrorCode} - {errorMessage}", sentryLevel);
		return new TResponse { ErrorCode = ErrorMapper.GetErrorCode(internalErrorCode) };
	}

	public static TResponse GenerateFailedResult<TResponse, TPayload>(int errorCode, string errorMessage) where TResponse : GameProviderResponse<TPayload>, new()
	{
		var sentryLevel = GetSentryLevelFromConfig(errorCode.ToString());
		SentrySdk.CaptureMessage($"{errorCode} - {errorMessage}", sentryLevel);
		return new TResponse { ErrorCode = errorCode };
	}

	public static SentryLevel GetSentryLevelFromConfig(string errorCode)
	{
		return _configuration.GetSection("ErrorCodesSentryLevel").GetValue<SentryLevel?>(errorCode) ?? SentryLevel.Error;
	}
}