namespace SabaPlus.API;

public static class StringHelper
{
	public static string ToPlayerCode(this string operatorCustId)
	{
		return operatorCustId.Replace("_test", string.Empty);
	}
}