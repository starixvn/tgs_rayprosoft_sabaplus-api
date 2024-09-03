using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using SabaPlus.Core.ApplicationServices;

namespace SabaPlus.Core;

internal class ApiLogsDispatcher(IServiceProvider serviceProvider) : IApiLogsDispatcher, IDisposable, IAsyncDisposable
{
	private readonly ConcurrentDictionary<string, ApiLogsChannel> _counters = new();
	private readonly CancellationTokenSource _cancellationTokenSource = new();

	private bool _disposedValue;

	~ApiLogsDispatcher()
	{
		Dispose(false);
	}

	public async ValueTask PushCommandAsync(ApiLogsCommand command, CancellationToken cancellationToken = default)
	{
		using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancellationTokenSource.Token);
		// TODO: Remove DateTime
		var counter = _counters.GetOrAdd(
			string.Concat(command.Key, "-", command.Action, "-", DateTime.Now.ToString()),
			key =>
			{
				var scope = serviceProvider.CreateScope();
				var newCounter = ActivatorUtilities.CreateInstance<ApiLogsChannel>(
					scope.ServiceProvider,
					key);

				newCounter.Disposed += (_, _) =>
				{
					_ = _counters.TryRemove(newCounter.Key, out _);
					scope.Dispose();
				};

				return newCounter;
			});

		await counter.PrepareAsync(cts.Token).ConfigureAwait(false);
		await counter.PushCommandAsync(command, cts.Token).ConfigureAwait(false);
	}

	public ValueTask DisposeAsync()
	{
		Dispose(true);
		GC.SuppressFinalize(this);

		return ValueTask.CompletedTask;
	}

	public void Dispose()
	{
		// 請勿變更此程式碼。請將清除程式碼放入 'Dispose(bool disposing)' 方法
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!_disposedValue)
		{
			if (disposing)
			{
				_cancellationTokenSource.Cancel();
			}

			_cancellationTokenSource.Dispose();
			_disposedValue = true;
		}
	}
}