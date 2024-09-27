namespace SabaPlus.Client;

public interface ISabaPlusClient
{
	Task BetResultCheck(int diff, CancellationToken cancellationToken = default);
}