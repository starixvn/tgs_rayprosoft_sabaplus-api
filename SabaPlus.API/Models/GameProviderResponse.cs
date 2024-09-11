namespace SabaPlus.API.Models;

public class GameProviderResponse<TPayload>
{
	public string TimeStamp { get; set; } = DateTime.UtcNow.ToString();

	public string Seq { get; set; } = string.Empty;

	public int ErrorCode { get; set; } = 0;

	public string ErrorMessage { get; set; } = string.Empty;

	public TPayload Payload { get; set; }
}
