using System.Diagnostics;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SabaPlus.Core.ApplicationServices;
using Tgs.ServiceUtility;

namespace SabaPlus.Core;

internal class ApiLogsChannel(
	ILogger<ApiLogsChannel> logger,
	IServiceProvider serviceProvider,
	string key) : IDisposable, IAsyncDisposable
{
	private static readonly ActivitySource _ActivitySource = new(name: typeof(ApiLogsCommand).FullName!);

	private readonly Channel<ApiLogsCommand> _channel = Channel.CreateUnbounded<ApiLogsCommand>(
		new()
		{
			SingleReader = true
		});

	private readonly SemaphoreSlim _locker = new(1, 1);
	private readonly CancellationTokenSource _cancellationTokenSource = new();

	private Task? _backgroundTask;
	private Timer? _timer;
	private bool _disposedValue;

	public event EventHandler? Disposed;

	public string Key { get; } = key;

	~ApiLogsChannel()
	{
		Dispose(disposing: false);
	}

	public async ValueTask PrepareAsync(CancellationToken cancellationToken = default)
	{
		using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancellationTokenSource.Token);

		if (_backgroundTask is not null)
			return;

		await _locker.WaitAsync(cts.Token).ConfigureAwait(false);

		try
		{
			if (_backgroundTask is not null)
				return;

			_backgroundTask = Task.Run(
				() => ProcessCommandLoopAsync(_cancellationTokenSource.Token),
				_cancellationTokenSource.Token);
		}
		finally
		{
			_ = _locker.Release();
		}
	}

	public async ValueTask PushCommandAsync(ApiLogsCommand command, CancellationToken cancellationToken = default)
	{
		using var cts = CancellationTokenSource.CreateLinkedTokenSource(
			cancellationToken,
			_cancellationTokenSource.Token);

		await _channel.Writer.WriteAsync(
			command,
			cts.Token).ConfigureAwait(false);
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
				_timer?.Dispose();
				_timer = null;

				Disposed?.Invoke(this, EventArgs.Empty);
				_channel.Writer.Complete();
				_cancellationTokenSource.Cancel();
			}

			_cancellationTokenSource.Dispose();
			_disposedValue = true;
		}
	}

	private async Task ProcessCommandLoopAsync(CancellationToken cancellationToken = default)
	{
		await Task.Yield();

		var reader = _channel.Reader;
		while (await reader
			.WaitToReadAsync(cancellationToken)
			.ConfigureAwait(false))
		{
			if (reader.TryRead(out var command))
			{
				using var activity = _ActivitySource.StartActivity(
					"ApiLogsChannel Handle Exchange",
					 ActivityKind.Internal,
					parentContext: default);
				try
				{
					logger.LogInformation("{logInformation}",
						JsonSerializer.Serialize(new
						{
							LogOn = DateTime.Now.ToString("u"),
							Data = new
							{
								command.Action,
								command.Req,
								command.Res,
							},
							Message = "This pull api log command."
						}));
					using var scope = serviceProvider.CreateScope();
					var service = scope.ServiceProvider.GetRequiredService<ICommandService<ApiLogsCommand>>();
					await service.ExecuteAsync(
						command: command,
						cancellationToken: cancellationToken)
						.ConfigureAwait(false);
				}
				catch (Exception ex)
				{
					logger.LogError(ex, "{logError}",
						JsonSerializer.Serialize(new
						{
							LogOn = DateTime.Now.ToString("u"),
							Data = new
							{
								command.Action,
								command.Req,
								command.Res,
							},
							Message = "This pull api log command exception occurs."
						}));
				}
			}
		}
	}
}