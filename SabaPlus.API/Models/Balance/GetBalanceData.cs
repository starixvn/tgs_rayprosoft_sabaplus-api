namespace SabaPlus.API.Models.Balance;

public record GetBalanceData
{
	public string Action { get; set; } = "GetBalance"; // Default value for action

	public required string UserId { get; set; }
}