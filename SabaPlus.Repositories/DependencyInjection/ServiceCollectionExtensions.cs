using SabaPlus.Core;
using SabaPlus.Repositories;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddRepositories(
		this IServiceCollection services)
		=> services
		.AddScoped<IGameProviderRepository, GameProviderRepository>()
		.AddScoped<IPlayerRepository, PlayerRepository>();
}