namespace SabaPlus.API.Models.Bonus;

public class BonusResult : OperatorResponse<BonusPayload>;

public record BonusPayload(string OperatorCustId, decimal Balance, string RefNo);
