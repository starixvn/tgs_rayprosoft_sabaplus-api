namespace SabaPlus.API.Models;

public class OperatorRequest<TPayload>
{
	public DateTime TimeStamp { get; set; }

	public string Seq { get; set; }

	public TPayload Payload { get; set; }
}
