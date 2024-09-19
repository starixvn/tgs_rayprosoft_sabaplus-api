namespace SabaPlus.Core.Models;
public record CacheSettingsOptions
{
	public int ExpiredTimeInMinute { get; set; } = 10;
}