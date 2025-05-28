using MassTransit.Logging; 
using OpenTelemetry.Trace;
using OpenTelemetry.Resources; 
using TemporaryName.Infrastructure.Messaging.MassTransit.Settings;
using System.Reflection; 

namespace TemporaryName.WebApi.Configurators;

public static partial class OpenTelemetryConfigurator
{
    public static void ConfigureOpenTelemetry(this IServiceCollection services, MassTransitOptions mtGlobalOptions, ILogger logger)
    {
        LogMethodCalled(logger, nameof(ConfigureOpenTelemetry));

        if (mtGlobalOptions is null)
        {
            LogOpenTelemetryOptionsMissing(logger, MassTransitOptions.SectionName);
            throw new InvalidOperationException("MassTransitOptions is not configured");
            return;
        }

        if (mtGlobalOptions.EnableOpenTelemetry)
        {
            LogConfiguringOpenTelemetry(logger, mtGlobalOptions.ServiceName ?? "UnknownService");
            
            services.AddOpenTelemetry().WithTracing(builder =>
            {
                string serviceName = mtGlobalOptions.ServiceName ?? Assembly.GetEntryAssembly()?.GetName().Name ?? "UnnamedMassTransitService";
                string serviceVersion = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "1.0.0";

                builder
                    .SetResourceBuilder(ResourceBuilder.CreateDefault()
                        .AddService(serviceName: serviceName, serviceVersion: serviceVersion))
                    .AddSource(DiagnosticHeaders.DefaultListenerName) // MassTransit main diagnostic source
                    .AddSource("MassTransit.Transport.RabbitMQ")      // Specific transport traces for RabbitMQ
                    .AddSource("MassTransit.Transport.Kafka");         // Specific transport traces for Kafka
                // Add other OpenTelemetry instrumentation sources as needed for your application:
                // .AddAspNetCoreInstrumentation()
                // .AddHttpClientInstrumentation()
                // .AddEntityFrameworkCoreInstrumentation()

                // Example: Configure exporter (e.g., Jaeger, OTLP)
                // builder.AddOtlpExporter(otlpOptions =>
                // {
                //    otlpOptions.Endpoint = new Uri(configuration["Otel:ExporterEndpoint"]);
                // });
                LogOpenTelemetrySourcesAdded(logger, serviceName, DiagnosticHeaders.DefaultListenerName);
            });
            LogOpenTelemetrySuccessfullyConfigured(logger);
        }
        else
        {
            LogOpenTelemetryDisabledByConfiguration(logger);
        }
    }
}