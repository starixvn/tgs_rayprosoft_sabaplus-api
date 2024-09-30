namespace SabaPlus.API.Models;

public static class ErrorMapper
{
	public static int Success = 0;
	public static int UnspecifiedError = 1000;
	public static int ParameterIncorrect = 1007;
	public static int AccountNotExist = 2003;
	public static int AlreadyProcessed = 3001;
	public static int InsufficientFunds = 3002;
	public static int PlayerExceededGameLimit = 3003;
	public static int NoSuchTicket = 3016;
	public static int DuplicateTransaction = 3017;

	public static Dictionary<string, int> InternalErrorCodes = new()
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

	public static Dictionary<int, string> ErrorMessage = new()
	{
		[UnspecifiedError] = "Unspecified Error",
		[ParameterIncorrect] = "Parameter(s) Incorrect",
		[AccountNotExist] = "Account does not exist",
		[AlreadyProcessed] = "Already processed with different details",
		[InsufficientFunds] = "Player has insufficient funds",
		[PlayerExceededGameLimit] = "The player exceeded their game limit",
		[DuplicateTransaction] = "Duplicate Transaction"
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