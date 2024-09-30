namespace SabaPlus.API.Models;

public static class ErrorMapper
{
	public const int Success = 0;
	public const int UnspecifiedError = 1000;
	public const int ParameterIncorrect = 1007;
	public const int AccountNotExist = 2003;
	public static int AlreadyProcessed = 3001;
	public const int InsufficientFunds = 3002;
	public const int PlayerExceededGameLimit = 3003;
	public const int DuplicateTransaction = 3017;
	public static int NoSuchTicket = 3016;
	public const int InvalidVendor = 1004;

	private static Dictionary<string, int> InternalErrorCodes = new()
	{
		["0"] = Success,
		["101"] = DuplicateTransaction,
		["102"] = NoSuchTicket,
		["103"] = NoSuchTicket,
		["201"] = UnspecifiedError,
		["202"] = DuplicateTransaction,
		["203"] = AccountNotExist,
		["204"] = ParameterIncorrect,
		["205"] = InsufficientFunds,
		["206"] = ParameterIncorrect,
		["207"] = AlreadyProcessed,
		["3001"] = AlreadyProcessed
	};

	private static Dictionary<int, string> ErrorMessage = new()
	{
		[UnspecifiedError] = "Unspecified Error",
		[ParameterIncorrect] = "Parameter(s) Incorrect",
		[AccountNotExist] = "Account does not exist",
		[AlreadyProcessed] = "Already processed with different details",
		[InsufficientFunds] = "Player has insufficient funds",
		[PlayerExceededGameLimit] = "The player exceeded their game limit",
		[DuplicateTransaction] = "Duplicate Transaction"
		[InvalidVendor] = "The Vendor Id is invalid"
	};

	public static int GetErrorCode(string internalErrorCode)
	{
		return InternalErrorCodes.TryGetValue(internalErrorCode, out int errorCode) ? errorCode : UnspecifiedError;
	}

	public static string GetErrorMessage(int errorCode)
	{
		return ErrorMessage.TryGetValue(errorCode, out string? message) ? message : string.Empty;
	}

	public static bool IsSuccess(string internalErrorCode)
	{
		return string.Equals(internalErrorCode, Success.ToString());
	}

	public static bool IsDulicateTransactionByAction(string internalErrorCode)
	{
		return internalErrorCode == "207";
	}
}