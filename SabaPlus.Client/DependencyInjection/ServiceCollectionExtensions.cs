using Microsoft.Extensions.Options;
using SabaPlus.Client;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddGameInterfaceSabaPlusClient(
		this IServiceCollection services,
		Action<SabaPlusClientOptions, IServiceProvider> configure)
		 => services
				.AddOptions<SabaPlusClientOptions>()
				.Configure(configure)
				.Services
				.AddHttpClient<ISabaPlusClient, SabaPlusClient>()
				.ConfigureHttpClient((sp, client) =>
				{
					var options = sp.GetRequiredService<IOptions<SabaPlusClientOptions>>().Value;

					client.BaseAddress = options.BaseUrl;
				})
				.Services;
}