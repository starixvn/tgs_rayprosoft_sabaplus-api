namespace SabaPlus.API.Models;

public class GameProviderRequestWithCode
{
	public string? GameProviderCode { get; set; } = default;
}

public class GameProviderRequest<TPayload> : GameProviderRequestWithCode
{
	public string TimeStamp { get; set; }

	public string Seq { get; set; }

	public TPayload Payload { get; set; }
}