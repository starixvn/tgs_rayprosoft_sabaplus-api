
namespace SabaPlus.API.Models.PlaceBetAndSettle;

public class PlaceBetAndSettleResult: OperatorResponse<PlaceBetAndSettlePayload>;

public record PlaceBetAndSettlePayload(string OperatorCustId, decimal Balance, PlaceBetAndSettleResponseDataSet[] BetDataSet);

public class PlaceBetAndSettleResponseDataSet
{ 
 public	string TicketNo { get; set;  }

 public string RefNo { get; set; }
}

