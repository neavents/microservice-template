using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using TemporaryName.Infrastructure;
using TemporaryName.Infrastructure.Hosting.Extensions;
using TemporaryName.Infrastructure.Persistence.Seeding;

Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();
Log.Information("Starting Seeding Tool");

try
{
    IHostBuilder hostBuilder = Host.CreateDefaultBuilder(args)
        .ConfigureStandardAppConfiguration(args);

    hostBuilder.UseServiceProviderFactory(new AutofacServiceProviderFactory());


    hostBuilder.ConfigureContainer<ContainerBuilder>(autofacBuilder =>
    {
        autofacBuilder.RegisterModule<InfrastructureModule>();
    });

    hostBuilder.ConfigureServices((hostContext, services) =>
    {
        //services.AddApplicationLayer();
        //services.AddInfrastructureLayer();
        //services.AddHostedService<Worker>();
    });

    using IHost host = hostBuilder.Build();

    Log.Information("Executing database seed...");

    // Resolve and run the seeder service
    using IServiceScope scope = host.Services.CreateScope();
    //IDatabaseSeeder seeder = scope.ServiceProvider.GetRequiredService<IDatabaseSeeder>();
    //await seeder.SeedAsync(CancellationToken.None);

    Log.Information("Database seeding completed.");
    return 0;
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Unhandled exception in Seeding Tool");
    return 1;
}
finally
{
    Log.Information("Stopping Seeding Tool");
    await Log.CloseAndFlushAsync();
}