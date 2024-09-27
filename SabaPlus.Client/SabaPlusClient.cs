using System.Net.Http.Json;
using System.Text.Json;

namespace SabaPlus.Client;

internal class SabaPlusClient(HttpClient client) : ISabaPlusClient
{
	private static readonly string _BetResultCheck = "/api/bet/ResultCheck";

	private static readonly JsonSerializerOptions? _SerializerOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase
	};

	public async Task BetResultCheck(int diff, CancellationToken cancellationToken = default)
	{
		var request = new
		{
			DiffInMinute = diff
		};

		var content = JsonContent.Create(request, options: _SerializerOptions);

		_ = await client.PostAsync(_BetResultCheck, content, cancellationToken).ConfigureAwait(false);
	}
}