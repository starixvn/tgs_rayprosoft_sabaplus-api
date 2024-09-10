namespace SabaPlus.API.Models;

public class GameProviderRequest<TPayload>
{
	public string TimeStamp { get; set; }

	public string Seq { get; set; }

	public TPayload Payload { get; set; }
}
