using MediatR;

namespace SabaPlus.API.Models.BetResultCheck;

public record BetResultCheckRequest : IRequest<BetResultCheckResult>
{
	public int DiffInMinute { get; set; }
}