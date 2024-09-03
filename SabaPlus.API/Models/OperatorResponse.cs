namespace SabaPlus.API.Models;

public class OperatorResponse<TPayload>
{
	public DateTime TimeStamp { get; set; }

	public string Seq { get; set; } = string.Empty;

	public decimal ErrorCode { get; set; } = 0;

	public string ErrorMessage { get; set; } = string.Empty;

	public TPayload Payload { get; set; }
}
