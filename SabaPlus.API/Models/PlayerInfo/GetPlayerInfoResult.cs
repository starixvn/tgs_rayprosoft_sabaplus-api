
namespace SabaPlus.API.Models.PlayerInfo;

public class GetPlayerInfoResult : OperatorResponse<GetPlayerInfoResponsePayload>;

public record GetPlayerInfoResponsePayload
{
	public string OperatorCustId { get; set; }

	public string UserName { get; set;  }

	public string Currency { get; set; }

	public string CurrencyISO { get; set; }

	public decimal Balance { get; set;  }

	public decimal MinBet { get; set;  }

	public decimal MaxBet { get; set; }

	public string BetGroup { get; set;  }

	public string WalletSideToken { get; set;  }
}
