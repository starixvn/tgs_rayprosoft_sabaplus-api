using MediatR;

namespace SabaPlus.API.Models.BetResultCheck;

public record BetResultCheckScheduleRequest : IRequest<BetResultCheckResult>
{
	public int DiffInMinute { get; set; }
}