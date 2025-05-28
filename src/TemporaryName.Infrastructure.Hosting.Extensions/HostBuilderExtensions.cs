using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using System.Reflection;

namespace TemporaryName.Infrastructure.Hosting.Extensions;

public static class HostBuilderExtensions
{
    /// <summary>
    /// Configures the application configuration by loading settings from common, application-specific,
    /// assembly-specific, environment variables, and command line arguments in a standardized order.
    /// Also reconfigures the static Serilog.Log.Logger with the final configuration.
    /// </summary>
    /// <param name="hostBuilder">The host builder to configure.</param>
    /// <param name="args">Command line arguments.</param>
    /// <param name="commonConfigRelativePath">Relative path parts from output directory to the common config files (e.g., "..", "..", ".."). Defaults to three levels up.</param>
    /// <returns>The configured host builder.</returns>
    public static IHostBuilder ConfigureStandardAppConfiguration(
        this IHostBuilder hostBuilder,
        string[]? args = null,
        params string[] commonConfigRelativePath)
    {
        args ??= []; 
        string[] relativePathParts = commonConfigRelativePath.Length > 0 ? commonConfigRelativePath : ["..", "..", "..", "config"];

        hostBuilder.ConfigureAppConfiguration((context, configBuilder) =>
        {
            IHostEnvironment env = context.HostingEnvironment;
            string assemblyName = context.HostingEnvironment.ApplicationName ??
                                  Assembly.GetEntryAssembly()?.GetName().Name ?? "UnknownApp";

            string projectBasePath = env.ContentRootPath;

            string commonConfigPath = Path.GetFullPath(Path.Combine([projectBasePath, .. relativePathParts]));

            configBuilder
                // .Sources.Clear();
                .AddJsonFile(Path.Combine(commonConfigPath, "appsettings.common.json"), optional: true, reloadOnChange: env.IsDevelopment())
                .AddJsonFile(Path.Combine(commonConfigPath, $"appsettings.common.{env.EnvironmentName}.json"), optional: true, reloadOnChange: env.IsDevelopment())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: env.IsDevelopment())
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: env.IsDevelopment())
                .AddJsonFile($"appsettings.{assemblyName}.json", optional: true, reloadOnChange: env.IsDevelopment())
                .AddJsonFile($"appsettings.{assemblyName}.{env.EnvironmentName}.json", optional: true, reloadOnChange: env.IsDevelopment())
                .AddEnvironmentVariables()
                .AddCommandLine(args);

            IConfigurationRoot builtConfig = configBuilder.Build();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(builtConfig)
                .CreateLogger();

            Log.Information("Standard Configuration loaded for Environment: {Environment}, Assembly: {AssemblyName}", env.EnvironmentName, assemblyName);
            Log.Debug("Common config path evaluated as: {CommonConfigPath}", commonConfigPath);

        });

        hostBuilder.UseSerilog((context, services, loggerConfiguration) =>
        {
            loggerConfiguration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext();
        });


        return hostBuilder;
    }
}