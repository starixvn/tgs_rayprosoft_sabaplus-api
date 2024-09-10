namespace SabaPlus.API.Models;

public class GameProviderResponse<TPayload>
{
	public string TimeStamp { get; set; }

	public string Seq { get; set; } = string.Empty;

	public decimal ErrorCode { get; set; } = 0;

	public string ErrorMessage { get; set; } = string.Empty;

	public TPayload Payload { get; set; }
}
