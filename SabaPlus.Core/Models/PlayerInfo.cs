namespace SabaPlus.Core.Models;

public class PlayerInfo
{
	public string PlayerCode { get; set; }
	public string Username { get; set; }
	public string CurrencyCode { get; set; }
	public decimal MinBet { get; set; }
	public decimal MaxBet { get; set; }
}