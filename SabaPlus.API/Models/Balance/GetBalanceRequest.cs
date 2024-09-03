namespace SabaPlus.API.Models.Balance;

public record GetBalanceRequest(
	   string key,
	   GetBalanceData message
	   );