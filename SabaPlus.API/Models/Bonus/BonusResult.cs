namespace SabaPlus.API.Models.Bonus;

public class BonusResult : GameProviderResponse<BonusPayload>;

public record BonusPayload(string OperatorCustId, decimal Balance, string RefNo);
