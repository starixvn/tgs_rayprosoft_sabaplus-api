using MediatR;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using NATS.Client.Core;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using SabaPlus.API;
using SabaPlus.API.Services;
using SabaPlus.Core.DependencyInjection;
using Tgs.GameRecord.Saba.Common;
using Tgs.MessageQueue.Nats.Serializers;

var builder = WebApplication.CreateBuilder(args);
// Add services to the container.
builder.Services
	.AddSabaPlusApiCore()
	.AddRepositories()
	.AddCashServiceClient((op, sp) =>
	{
		var config = sp.GetRequiredService<IConfiguration>();
		op.BaseUrl = config.GetValue<Uri>("ServicesUrl:CashClientBaseUrl")!;
	})
	.AddMemoryCache();

var env = builder.Configuration.GetValue<string>("Env");
builder.Services
	.AddKeyedSingleton("Nats", (sp, _) => new NatsConnection(NatsOpts.Default with
	{
		Url = builder.Configuration.GetConnectionString("Nats")!,
		SerializerRegistry = TgsNatsSerializerRegistration.Default
	}))
	.AddMessageQueue()
	.AddNatsMessageQueue(config => config.ConfigureResolveConnection(sp => sp.GetRequiredKeyedService<NatsConnection>("Nats")))
	.ClearAllExchanges()
	.AddNatsGlobPatternExchange($"{Constants.ApiLogsPublish}.{env}")
	.AddNatsGlobPatternExchange($"{Constants.HandleBetPublish}.{env}");

builder.Services.AddControllers().AddJsonOptions(options =>
{
	options.JsonSerializerOptions.PropertyNamingPolicy = null;
});
;
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
		.AddSource("SabaPlus.*")
		.AddSource("Npgsql")
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
		.SetSampler(new AlwaysOnSampler())
		.AddEntityFrameworkCoreInstrumentation(options => options.SetDbStatementForText = true)
		.AddProfileViewExporter()
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

builder.Services.AddAutoMapper(typeof(MappingProfile));
builder.Services
	.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(typeof(Program).Assembly))
	.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
builder.Services.Configure<KestrelServerOptions>(options =>
{
	options.AllowSynchronousIO = true;
});
builder.Services.Configure<IISServerOptions>(options =>
{
	options.AllowSynchronousIO = true;
});
var app = builder.Build();

// Configure the HTTP request pipeline.
if (builder.Configuration.GetValue<string>("Env") == "DEV")
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

SentrySdk.CaptureMessage($"Started SABA Plus API at {DateTime.Now.ToString()}");

app.Run();