using System.Globalization;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Formatting.Elasticsearch;
using TemporaryName.Infrastructure.Observability.Settings;
using ElasticsearchSinkOptions = Serilog.Sinks.Elasticsearch.ElasticsearchSinkOptions;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using UserDefinedElasticsearchSinkOptions = TemporaryName.Infrastructure.Observability.Settings.ElasticsearchSinkOptions;
namespace TemporaryName.Infrastructure.Observability;

public static partial class DependencyInjection
{
    private static string _resolvedServiceName = "UnknownService";
    private static string? _resolvedServiceVersion = "0.0.0";
    private static string _resolvedDeploymentEnvironment = "Undefined";

    /// <summary>
    /// An extension method for <c>LoggerConfiguration</c> configures Serilog for structured logging, including console and Elasticsearch sinks.
    /// This method is intended to be used with <c>builder.Host.UseSerilog(...)</c>.
    /// </summary>
    /// <param name="loggerConfiguration">The Serilog logger configuration to be built upon.</param>
    /// <param name="hostContext">The host builder context, providing access to configuration and environment.</param>
    /// <param name="observabilitySettings">Pre-resolved observability settings.</param>
    /// <remarks>
    /// This modular approach allows Serilog to be configured with all necessary context
    /// and settings before the host is fully built.
    /// </remarks>
    public static void ConfigureSerilogForElk(
        this LoggerConfiguration loggerConfiguration,
        HostBuilderContext hostContext,
        ObservabilityOptions observabilitySettings,
        ILogger logger,
        IConfiguration configuration)
    {
        var serviceInfo = observabilitySettings.ServiceInfo;
        var entryAssembly = Assembly.GetEntryAssembly();
        _resolvedServiceName = serviceInfo.ServiceName ?? entryAssembly?.GetName().Name ?? "UnknownService";
        _resolvedServiceVersion = serviceInfo.ServiceVersion ?? entryAssembly?.GetName().Version?.ToString();
        _resolvedDeploymentEnvironment = serviceInfo.DeploymentEnvironment ?? hostContext.HostingEnvironment.EnvironmentName; 

        LogObservabilitySetupStarting(logger, _resolvedServiceName);

        if (observabilitySettings.Serilog.EnableSelfLog)
        {
            SelfLog.Enable(Console.Error);
            LogSelfLogEnabled(logger);
        }

        loggerConfiguration
            .ReadFrom.Configuration(hostContext.Configuration)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("ApplicationName", _resolvedServiceName)
            .Enrich.WithProperty("ApplicationVersion", _resolvedServiceVersion)
            .Enrich.WithProperty("Environment", _resolvedDeploymentEnvironment)
            .Enrich.WithProperty("MachineName", Environment.MachineName)
            .Enrich.WithProcessId()
            .Enrich.WithThreadId()
            .Enrich.WithExceptionDetails();
        //.Enrich.WithCorrelationIdHeader();

        if (observabilitySettings.Serilog.DefaultLogProperties is not null)
        {
            foreach (var prop in observabilitySettings.Serilog.DefaultLogProperties)
            {
                loggerConfiguration.Enrich.WithProperty(prop.Key, prop.Value);
            }
        }

        ConfigureSerilogMinimumLevels(loggerConfiguration, observabilitySettings.Serilog.MinimumLevel);

        if (observabilitySettings.Serilog.WriteToConsole)
        {

            loggerConfiguration.WriteTo.Async(wt => wt.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] ({SourceContext}) {Message:lj} {Properties:j}{NewLine}{Exception}",
                formatProvider: CultureInfo.InvariantCulture
            ));
        }
        LogSerilogConfigured(logger, observabilitySettings.Serilog.WriteToConsole, observabilitySettings.Serilog.MinimumLevel.Default);

        AddElasticsearchSinkInternal(loggerConfiguration, observabilitySettings.ElasticsearchSink, logger);
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
                sinkMinLevel = LogEventLevel.Information;
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
    /// <param name="logger">A logger for the DI setup process.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddConfiguredAppElasticApm(
        this IServiceCollection services,
        IConfiguration configuration,
        ObservabilityOptions observabilitySettings,
        ILogger logger)
    {
        var apmSettings = observabilitySettings.ElasticApm;

        if (apmSettings.Enabled)
        {
            // `AddAllElasticApm` registers all available listeners.
            // It reads configuration from the "ElasticApm" section of IConfiguration
            // or environment variables (e.g., ELASTIC_APM_SERVICE_NAME).
            // Our ObservabilitySettings.ElasticApm section provides these values to the config.
            services.AddAllElasticApm();

            LogElasticApmConfigured(logger,
                _resolvedServiceName,
                configuration["ElasticApm:ServerUrl"] ?? apmSettings.ServerUrl,
                configuration["ElasticApm:TransactionSampleRate"] ?? apmSettings.TransactionSampleRate,
                configuration["ElasticApm:Environment"] ?? apmSettings.Environment ?? _resolvedDeploymentEnvironment);
        }
        else
        {
            LogElasticApmDisabled(logger, _resolvedServiceName);
        }
        return services;
    }

}
