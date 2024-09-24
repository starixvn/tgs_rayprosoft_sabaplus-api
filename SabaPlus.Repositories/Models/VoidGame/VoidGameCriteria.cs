using SabaPlus.Common;

namespace SabaPlus.Repositories.Models;

public class VoidGameCriteria
{
	public string Seq { get; set; }
	public string OperatorCustId { get; set; }
	public string PlayerCode => OperatorCustId.ToPlayerCode();
	public string TicketNo { get; set; }
	public string Action => ActionConstants.VoidGame;
	public string GameProviderCode { get; set; }
	public string GameType { get; set; }
	public string GameRoundId { get; set; }
	public string ExternalGameId { get; set; }
	public string? TransactionId { get; set; }
	public DateTime TransDate { get; set; }
	public DateTime SettleDate => TransDate;
	public int ProviderTicketStatus { get; set; }
}