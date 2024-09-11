using MongoDB.Driver;
using SabaPlus.Core;
using SabaPlus.Repositories;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddRepositories(
		this IServiceCollection services,
		Func<IServiceProvider, IMongoClient> mongoClientResolver)
		=> services
		.AddTransient<IApiLogsRepository>((sp) => ActivatorUtilities.CreateInstance<ApiLogsRepository>(sp, mongoClientResolver(sp)))
		.AddScoped<IPlayerRepository, PlayerRepository>();
}