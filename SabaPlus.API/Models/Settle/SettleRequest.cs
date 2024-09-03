namespace SabaPlus.API.Models.Settle;

public record SettleRequest(
	   string key,
	   SettleData message
	   );