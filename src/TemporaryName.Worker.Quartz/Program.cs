using TemporaryName.Application;
using TemporaryName.Infrastructure;
using TemporaryName.Infrastructure.Hosting.Extensions;
using TemporaryName.Worker.Quartz;
using Serilog;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using TemporaryName.Domain;

Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();
Log.Information("Starting Worker.Quartz");

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
    Log.Fatal(ex, "Unhandled exception in Worker.Quartz");
}
finally
{
    Log.Information("Stopping Worker.Quartz");
    await Log.CloseAndFlushAsync();
}