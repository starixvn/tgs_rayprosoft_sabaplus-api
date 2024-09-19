using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SabaPlus.Core.Models;
using System.Reflection;

namespace SabaPlus.Core.DependencyInjection;

public static class ServiceCollecitonExtensions
{
	public static IServiceCollection AddSabaPlusApiCore(
	this IServiceCollection services)
	=> services
	.AddOptions<CacheSettingsOptions>()
	.Configure((CacheSettingsOptions options, IServiceProvider sp) =>
	{
		var configuration = sp.GetRequiredService<IConfiguration>();
		options.ExpiredTimeInMinute = configuration.GetValue<int>("MemoryCacheSettings:ExpiredTimeInMinute");
		configuration.GetSection("CacheSettings").Bind(options);
	})
	.Services
	.RegisterAllServiceUtilityTypes(Assembly.GetExecutingAssembly(), ServiceLifetime.Transient);
}