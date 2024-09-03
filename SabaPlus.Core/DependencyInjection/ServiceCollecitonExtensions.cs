using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace SabaPlus.Core.DependencyInjection;

public static class ServiceCollecitonExtensions
{
	public static IServiceCollection AddSabaPlusApiCore(
	this IServiceCollection services)
	=> services
	.RegisterAllServiceUtilityTypes(Assembly.GetExecutingAssembly(), ServiceLifetime.Transient);
}