using System.Reflection;
using SabaPlus.API.Messages;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddGameInterfaceCore(
		this IServiceCollection services)
		=> services
		.RegisterAllServiceUtilityTypes(
			Assembly.GetExecutingAssembly(),
			ServiceLifetime.Transient)
		.AddTransient<ISabaPlusApiMessagePublishHandler, SabaPlusMessagePublishHandler>();
}