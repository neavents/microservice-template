using System;
using System.Reflection;
using Elastic.Apm.NetCoreAll;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Formatting.Elasticsearch;
using Serilog.Sinks.Elasticsearch;
using TemporaryName.Infrastructure.Observability.Settings;
using ElasticsearchSinkOptions = Serilog.Sinks.Elasticsearch.ElasticsearchSinkOptions;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using UserDefinedElasticsearchSinkOptions = TemporaryName.Infrastructure.Observability.Settings.ElasticsearchSinkOptions;
namespace TemporaryName.Infrastructure.Observability;

public static partial class DependencyInjection // Made partial to link with DependencyInjection.Log.cs
{
    private static string _resolvedServiceName = "UnknownService";
    private static string? _resolvedServiceVersion = "0.0.0";
    private static string _resolvedDeploymentEnvironment = "Undefined";

    /// <summary>
    /// Configures Serilog for structured logging, including console and Elasticsearch sinks.
    /// This method is intended to be used with <c>builder.Host.UseSerilog(...)</c>.
    /// </summary>
    /// <param name="hostContext">The host builder context, providing access to configuration and environment.</param>
    /// <param name="services">The service provider, useful for resolving services needed during configuration.</param>
    /// <param name="loggerConfiguration">The Serilog logger configuration to be built upon.</param>
    /// <param name="observabilitySettings">Pre-resolved observability settings.</param>
    /// <remarks>
    /// This modular approach allows Serilog to be configured with all necessary context
    /// and settings before the host is fully built.
    /// </remarks>
    public static void ConfigureSerilogForElk(
        HostBuilderContext hostContext,
        IServiceProvider services, // Can be used to resolve services if needed during logging setup
        LoggerConfiguration loggerConfiguration,
        ObservabilityOptions observabilitySettings)
    {
        // Resolve a logger for the setup process itself.
        // Since Serilog isn't fully configured yet, use a simple console logger for these DI logs.
        // Or, if an ILoggerFactory is already available in `services`, use that.
        var diLogger = services.GetService<ILoggerFactory>()?.CreateLogger(typeof(DependencyInjection).FullName!)
                       ?? new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();

        // Determine service info for enrichers
        var serviceInfo = observabilitySettings.ServiceInfo;
        var entryAssembly = Assembly.GetEntryAssembly();
        _resolvedServiceName = serviceInfo.ServiceName ?? entryAssembly?.GetName().Name ?? "UnknownService";
        _resolvedServiceVersion = serviceInfo.ServiceVersion ?? entryAssembly?.GetName().Version?.ToString();
        _resolvedDeploymentEnvironment = serviceInfo.DeploymentEnvironment ?? hostContext.HostingEnvironment.EnvironmentName;

        LogObservabilitySetupStarting(diLogger, _resolvedServiceName);

        if (observabilitySettings.Serilog.EnableSelfLog)
        {
            SelfLog.Enable(Console.Error);
            LogSelfLogEnabled(diLogger);
        }

        loggerConfiguration
            .ReadFrom.Configuration(hostContext.Configuration) // Allows appsettings.json overrides for Serilog
            .Enrich.FromLogContext()
            .Enrich.WithProperty("ApplicationName", _resolvedServiceName)
            .Enrich.WithProperty("ApplicationVersion", _resolvedServiceVersion)
            .Enrich.WithProperty("Environment", _resolvedDeploymentEnvironment)
            .Enrich.WithProperty("MachineName", Environment.MachineName) // More reliable than Serilog.Enrichers.Environment
            .Enrich.WithProcessId()
            .Enrich.WithThreadId()
            .Enrich.WithExceptionDetails(); // Serilog.Exceptions for detailed exception logging
            // Consider .Enrich.WithCorrelationIdHeader() if you have middleware setting it.

        // Default properties from settings
        if (observabilitySettings.Serilog.DefaultLogProperties != null)
        {
            foreach (var prop in observabilitySettings.Serilog.DefaultLogProperties)
            {
                loggerConfiguration.Enrich.WithProperty(prop.Key, prop.Value);
            }
        }

        ConfigureSerilogMinimumLevels(loggerConfiguration, observabilitySettings.Serilog.MinimumLevel);

        if (observabilitySettings.Serilog.WriteToConsole)
        {
            // Using a structured JSON formatter for console in dev can be useful for consistency with ES
            // Or a simpler text template for readability.
            loggerConfiguration.WriteTo.Async(wt => wt.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] ({SourceContext}) {Message:lj} {Properties:j}{NewLine}{Exception}"
            ));
        }
        LogSerilogConfigured(diLogger, observabilitySettings.Serilog.WriteToConsole, observabilitySettings.Serilog.MinimumLevel.Default);

        AddElasticsearchSinkInternal(loggerConfiguration, observabilitySettings.ElasticsearchSink, diLogger);
    }

    private static void ConfigureSerilogMinimumLevels(LoggerConfiguration loggerConfiguration, MinimumLevelOptions minLevelSettings)
    {
        if (Enum.TryParse<LogEventLevel>(minLevelSettings.Default, true, out var defaultLevel))
        {
            loggerConfiguration.MinimumLevel.Is(defaultLevel);
        }

        if (minLevelSettings.Override != null)
        {
            foreach (var (source, levelString) in minLevelSettings.Override)
            {
                if (Enum.TryParse<LogEventLevel>(levelString, true, out var level))
                {
                    loggerConfiguration.MinimumLevel.Override(source, level);
                }
            }
        }
    }

    private static void AddElasticsearchSinkInternal(
        LoggerConfiguration loggerConfiguration,
        UserDefinedElasticsearchSinkOptions esSinkSettings,
        ILogger diLogger) 
    {
        if (esSinkSettings.Enabled && esSinkSettings.NodeUris?.Any() == true)
        {
            if (!Enum.TryParse<LogEventLevel>(esSinkSettings.MinimumLogEventLevel, true, out var sinkMinLevel))
            {
                sinkMinLevel = LogEventLevel.Information; // Default if parsing fails
                diLogger.LogWarning("Could not parse MinimumLogEventLevel '{SinkMinLevelString}' for Elasticsearch sink. Defaulting to '{DefaultSinkMinLevel}'.",
                    esSinkSettings.MinimumLogEventLevel, sinkMinLevel);
            }

            var sinkOptions = new ElasticsearchSinkOptions(esSinkSettings.NodeUris.Select(uri => new Uri(uri)))
            {
                IndexFormat = esSinkSettings.IndexFormat,
                MinimumLogEventLevel = sinkMinLevel,
                BatchPostingLimit = esSinkSettings.BatchPostingLimit,
                Period = esSinkSettings.Period,
                ConnectionTimeout = esSinkSettings.ConnectionTimeout,
                // Using ElasticsearchJsonFormatter for better compatibility and structured data
                CustomFormatter = new ElasticsearchJsonFormatter(
                    omitEnclosingObject: false, // Required for bulk API
                    renderMessage: true,       // Render the message template
                    formatStackTraceAsArray: true // Better for Kibana stack trace visualization
                ),
                // Example for durable file buffer - configure paths appropriately for your environment
                // BufferBaseFilename = "./logs/serilog-es-buffer", // Ensure this path is writable
                // BufferFileSizeLimitBytes = 100 * 1024 * 1024, // 100MB
                // BufferFileCountLimit = 31, // Max 31 files
                // BufferLogShippingInterval = TimeSpan.FromSeconds(10),
                ModifyConnectionSettings = connectionSettings =>
                {
                    if (!string.IsNullOrWhiteSpace(esSinkSettings.ApiKey))
                    {
                        // Ensure ID is unique if multiple sinks/clients use API keys in the same process
                        connectionSettings.ApiKeyAuthentication(Guid.NewGuid().ToString(), esSinkSettings.ApiKey);
                    }
                    else if (!string.IsNullOrWhiteSpace(esSinkSettings.Username) && !string.IsNullOrWhiteSpace(esSinkSettings.Password))
                    {
                        connectionSettings.BasicAuthentication(esSinkSettings.Username, esSinkSettings.Password);
                    }
                    return connectionSettings;
                }
            };

            loggerConfiguration.WriteTo.Async(wt => wt.Elasticsearch(sinkOptions));
            LogElasticsearchSinkConfigured(diLogger, string.Join(",", esSinkSettings.NodeUris), esSinkSettings.IndexFormat, sinkMinLevel.ToString());
        }
        else
        {
            LogElasticsearchSinkDisabled(diLogger);
        }
    }

    /// <summary>
    /// Adds and configures the Elastic APM agent for .NET services.
    /// This method should be called in <c>ConfigureServices</c> (or <c>builder.Services</c>) in <c>Program.cs</c>.
    /// The APM agent will automatically instrument ASP.NET Core, HttpClient, EF Core, and MassTransit if <c>Elastic.Apm.NetCoreAll</c> is used.
    /// Configuration is primarily read from the "ElasticApm" section of <c>appsettings.json</c> or environment variables.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="configuration">The application's <see cref="IConfiguration"/>.</param>
    /// <param name="observabilitySettings">Pre-resolved observability settings to check for APM enablement.</param>
    /// <param name="diLogger">A logger for the DI setup process.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddAppElasticApm(
        this IServiceCollection services,
        IConfiguration configuration,
        ObservabilityOptions observabilitySettings,
        ILogger diLogger)
    {
        var apmSettings = observabilitySettings.ElasticApm;

        if (apmSettings.Enabled)
        {
            // `AddAllElasticApm` registers all available listeners.
            // It reads configuration from the "ElasticApm" section of IConfiguration
            // or environment variables (e.g., ELASTIC_APM_SERVICE_NAME).
            // Our ObservabilitySettings.ElasticApm section provides these values to the config.
            services.AddAllElasticApm();

            LogElasticApmConfigured(diLogger,
                _resolvedServiceName, // Use the service name resolved during Serilog setup
                configuration["ElasticApm:ServerUrl"] ?? apmSettings.ServerUrl, // Show what APM agent will likely use
                configuration["ElasticApm:TransactionSampleRate"] ?? apmSettings.TransactionSampleRate,
                configuration["ElasticApm:Environment"] ?? apmSettings.Environment ?? _resolvedDeploymentEnvironment);
        }
        else
        {
            LogElasticApmDisabled(diLogger, _resolvedServiceName);
        }
        return services;
    }

    /// <summary>
    /// Registers the Elastic APM middleware.
    /// This should be called early in the ASP.NET Core request pipeline (in <c>Configure</c> method or after <c>app = builder.Build()</c>).
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="observabilitySettings">Pre-resolved observability settings to check for APM enablement.</param>
    /// <param name="logger">A logger for the DI setup process.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection ConfigureAppElasticApm(
        this IServiceCollection services,
        ObservabilityOptions observabilitySettings,
        ILogger logger)
    {
        if (observabilitySettings.ElasticApm.Enabled)
        {
            services.AddAllElasticApm();
            LogElasticApmMiddlewareRegistered(logger, _resolvedServiceName);
        }
        return services;
    }
}
