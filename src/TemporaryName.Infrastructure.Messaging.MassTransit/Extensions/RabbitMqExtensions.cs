using System;
using System.Text;
using TemporaryName.Infrastructure.Messaging.MassTransit.Settings;

namespace TemporaryName.Infrastructure.Messaging.MassTransit.Extensions;

public static class RabbitMqExtensions
{
    public static string CreateAMQPConnectionString(RabbitMqConnectionOptions rabbitMqOpts, bool useSsl = false, string? customHost = null)
    {
        ArgumentNullException.ThrowIfNull(rabbitMqOpts);

        StringBuilder amqpStringBuilder = new StringBuilder();
        amqpStringBuilder.Append("amqp");

        if (useSsl) amqpStringBuilder.Append('s');

        amqpStringBuilder.Append("://");
        amqpStringBuilder.Append(Uri.EscapeDataString(rabbitMqOpts.Username));
        amqpStringBuilder.Append(':');
        amqpStringBuilder.Append(Uri.EscapeDataString(rabbitMqOpts.Password));
        amqpStringBuilder.Append('@');

        if (customHost is not null)
        {
            amqpStringBuilder.Append(customHost);
        }
        else
        {
            amqpStringBuilder.Append(rabbitMqOpts.Host);
        }

        amqpStringBuilder.Append(':');
        amqpStringBuilder.Append(rabbitMqOpts.Port);
        amqpStringBuilder.Append('/');
        amqpStringBuilder.Append(Uri.EscapeDataString(rabbitMqOpts.VirtualHost.TrimStart('/')));
        
        return amqpStringBuilder.ToString();
    } 
}
