using System.ComponentModel.DataAnnotations;

namespace SabaPlus.Common;
public record ApiLogMessage
{
	[Required]
	public string Action { get; init; } = default!;

	public object Request { get; init; } = default!;

	public object Response { get; init; } = default!;
}