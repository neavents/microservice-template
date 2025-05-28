using TemporaryName.WebApi;
using TemporaryName.Infrastructure.Hosting.Extensions;
using Serilog;
using Autofac.Extensions.DependencyInjection;
using Autofac;
using TemporaryName.Application;
using TemporaryName.Infrastructure;
using TemporaryName.Domain;
using TemporaryName.Infrastructure.Caching.Redis;
using TemporaryName.Infrastructure.ChangeDataCapture.Debezium;
using TemporaryName.Infrastructure.HttpClient;
using TemporaryName.Infrastructure.Messaging.MassTransit;
using TemporaryName.Infrastructure.Observability;
using TemporaryName.Infrastructure.Persistence.Hybrid.Sql.PostgreSQL;
using TemporaryName.Infrastructure.Security.Authorization.Extensions;
using TemporaryName.WebApi.Configurators;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using HealthChecks.UI.Client;

Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();
Log.Information("Starting WebApi");

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
    builder.Host.ConfigureStandardAppConfiguration(args);

    builder.LoadAndConfigureSettings();

    builder.Host.ConfigureContainer<ContainerBuilder>(autofacBuilder =>
    {
        autofacBuilder.RegisterModule<ApplicationServicesModule>()
            .RegisterModule<InfrastructureModule>()
            .RegisterModule<DomainModule>()
            .RegisterModule<RedisCacheModule>()
            .RegisterModule<DebeziumCdcModule>()
            .RegisterModule<HttpClientModule>()
            .RegisterModule<MassTransitModule>()
            .RegisterModule<ObservabilityModule>()
            .RegisterModule<PostgreSqlApplicationDataModule>()
            .RegisterModule<AuthorizationModule>();
    });
    builder.Services.AddOpenApi();
    //builder.Services.AddLayers();

    var app = builder.Build();


    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    app.UseHttpsRedirection();

    app.MapHealthChecks("/healthz", new HealthCheckOptions
    {
        Predicate = _ => true, // Include all registered health checks
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    });

    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Unhandled exception in WebApi");
}
finally
{
    Log.Information("Stopping WebApi");
    await Log.CloseAndFlushAsync().ConfigureAwait(false);
}