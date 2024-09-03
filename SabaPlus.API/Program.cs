using Microsoft.AspNetCore.HttpLogging;
using NATS.Client.Core;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using SabaPlus.API;
using SabaPlus.API.Messages;
using SabaPlus.API.Middlewares;
using SabaPlus.Core.DependencyInjection;
using Tgs.GameRecord.Saba.Common;
using Tgs.MessageQueue.Nats.Serializers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services
	.AddOptions<MongoDBOptions>()
	.Configure((MongoDBOptions options, IServiceProvider sp) =>
	{
		var configuration = sp.GetRequiredService<IConfiguration>();
		options.ConnectionString = configuration.GetConnectionString("Mongo")!;
	})
	.Services
	.AddSingleton<MongoDBConnectionManager>()
	.AddSabaPlusApiCore()
	.AddApiLogsRepositories(mongoClientResolver: (sp) => sp.GetRequiredService<MongoDBConnectionManager>().MongoClient);

builder.Services
	.AddMessageQueue()
	.AddInProcessMessageQueue(configuration => 
		configuration.AddHandler<ApiLogsMessageHandler>(Constants.ApiLogsExchange))
	.AddInProcessGlobPatternExchange(Constants.InprocessGlobExchange);

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddGameInterfaceCore();
builder.Services
	.AddHttpLogging(logging => logging.LoggingFields = HttpLoggingFields.All)
	.AddHealthChecks();
builder.WebHost.UseSentry();

var excludeExtensions = new[] { "html", "ico", "png", "jpg" };

builder.Services
	.AddOpenTelemetry()
	.ConfigureResource(_ => _.AddService(builder.Configuration.GetValue<string>("Otel:ServiceName") ?? ""))
	.WithTracing(tracerProviderBuilder => tracerProviderBuilder
		.AddSource("Tgs.*")
		.AddSource("TGS.*")
		.AddSource("Valhalla.*")
		.AddAspNetCoreInstrumentation(options => options.Filter =
			httpContext => !httpContext.Request.Path.HasValue
				|| !httpContext.Request.Path.StartsWithSegments("/swagger")
				&& !httpContext.Request.Path.StartsWithSegments("/health")
				&& !httpContext.Request.Path.StartsWithSegments("/healthcheck")
				&& !httpContext.Request.Path.StartsWithSegments("/profiler/")
				&& !httpContext.Request.Path.StartsWithSegments("/metrics")
				&& !excludeExtensions.Any(
					ext => httpContext.Request.Path.Value.EndsWith($".{ext}")))
		.AddHttpClientInstrumentation()
		.AddEntityFrameworkCoreInstrumentation(options => options.SetDbStatementForText = true)
		.AddOtlpExporter(options => options.Endpoint = builder.Configuration.GetValue<Uri>("Otel:Collector") ?? new Uri("")))
	.WithMetrics(meterBuiler => meterBuiler
		.SetResourceBuilder(ResourceBuilder.CreateDefault()
			.AddEnvironmentVariableDetector())
			.AddRuntimeInstrumentation()
			.AddAspNetCoreInstrumentation()
			.AddOtlpExporter())
	.Services
	.AddProfileViewer()
	.AddInterceptionService()
	.AddServiceUtiltiyInterceptors();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (builder.Configuration.GetValue<string>("Env") == "LOCAL")
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health");

app.MapHealthChecks("/healthcheck");

app.MapHealthChecks("/");

app.Run();