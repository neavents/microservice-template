using System;
using System.Reflection;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using TemporaryName.Infrastructure.Observability.Settings;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace TemporaryName.Infrastructure.Observability.Models;

public class InfrastructureModel
{
    public required IConfiguration Configuration;
    public required ILogger Logger;
    public required Assembly[] MassTransitConsumerAssemblies;
    public Action<IBusRegistrationContext, IRabbitMqBusFactoryConfigurator>? ConfigureExtraRabbitMqBusFeatures;
    public required ObservabilityOptions ObservabilityOptions;
    
}