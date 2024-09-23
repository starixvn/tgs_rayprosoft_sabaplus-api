using MediatR;
using SabaPlus.API.Models.BetResultCheck;
using Tgs.GameProviders.Client;

namespace SabaPlus.API.Services;

public class BetResultCheckHandler(IGameProvidersClient gameProvidersClient) : IRequestHandler<BetResultCheckRequest, BetResultCheckResult>
{
	public async Task<BetResultCheckResult> Handle(BetResultCheckRequest request, CancellationToken cancellationToken)
	{
		var result = await gameProvidersClient.BetResultCheck<BetResultCheckFromProvider>("SABAPLUS-MAXPLAY", "5403", "5403|$|4unxqjozv9ev_test|$|5403_30306643", "4unxqjozv9ev", cancellationToken).ConfigureAwait(false);

		return new BetResultCheckResult();
	}
}