using TemporaryName.WebApi;
using TemporaryName.Infrastructure.Hosting.Extensions;
using Serilog;
using Autofac.Extensions.DependencyInjection;
using Autofac;
using TemporaryName.Application;
using TemporaryName.Infrastructure;
using TemporaryName.Domain;

Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();
Log.Information("Starting WebApi");

try
{

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
    builder.Host.ConfigureStandardAppConfiguration(args);


    builder.Host.ConfigureContainer<ContainerBuilder>(autofacBuilder =>
    {
        autofacBuilder.RegisterModule<ApplicationServicesModule>()
            .RegisterModule<InfrastructureModule>()
            .RegisterModule<DomainModule>();
    });
    builder.Services.AddOpenApi();
    //builder.Services.AddLayers();

    var app = builder.Build();


    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    app.UseHttpsRedirection();


    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Unhandled exception in WebApi");
}
finally
{
    Log.Information("Stopping WebApi");
    await Log.CloseAndFlushAsync();
}

public partial class Program { }