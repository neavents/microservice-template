using TemporaryName.Application;
using TemporaryName.Infrastructure;
using TemporaryName.Infrastructure.Hosting.Extensions;
using TemporaryName.Worker.Hangfire;
using Serilog;
using Autofac.Extensions.DependencyInjection;
using Autofac;
using TemporaryName.Domain;

Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();
Log.Information("Starting Worker.Hangfire");

try
{
    IHostBuilder hostBuilder = Host.CreateDefaultBuilder(args)
        .ConfigureStandardAppConfiguration(args);

    hostBuilder.UseServiceProviderFactory(new AutofacServiceProviderFactory());

    hostBuilder.ConfigureContainer<ContainerBuilder>(autofacBuilder =>
    {
        autofacBuilder.RegisterModule<ApplicationServicesModule>()
            .RegisterModule<InfrastructureModule>()
            .RegisterModule<DomainModule>();
    });

    hostBuilder.ConfigureServices((hostContext, services) =>
    {
        //services.AddApplicationLayer();
        //services.AddInfrastructureLayer();
        services.AddHostedService<Worker>();
    });

    using IHost host = hostBuilder.Build();
    await host.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Unhandled exception in Worker.Hangfire");
}
finally
{
    Log.Information("Stopping Worker.Hangfire");
    await Log.CloseAndFlushAsync();
}