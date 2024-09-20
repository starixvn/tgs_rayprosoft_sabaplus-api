using SabaPlus.API.Models;

namespace SabaPlus.API.Services.Shared;

public static class ResultHelper
{
	public static TResponse GenerateFailedResult<TResponse, TPayload>(string internalErrorCode, string errorMessage) where TResponse : GameProviderResponse<TPayload>, new()
	{
		SentrySdk.CaptureException(new Exception($"{internalErrorCode} - {errorMessage}"));
		return new TResponse { ErrorCode = ErrorMapper.GetErrorCode(internalErrorCode) };
	}

	public static TResponse GenerateFailedResult<TResponse, TPayload>(int errorCode, string errorMessage) where TResponse : GameProviderResponse<TPayload>, new()
	{
		SentrySdk.CaptureException(new Exception($"{errorCode} - {errorMessage}"));
		return new TResponse { ErrorCode = errorCode };
	}
}