using System;
using System.Globalization;
using System.Text;
using Microsoft.Extensions.Options;
using TemporaryName.Infrastructure.Caching.Abstractions;
using TemporaryName.Infrastructure.Caching.Redis.Settings;

namespace TemporaryName.Infrastructure.Caching.Redis.Implementations;

public class RedisCacheKeyService : ICacheKeyService
{
    private readonly RedisCacheOptions _options;
    private const char Separator = ':';

    public RedisCacheKeyService(IOptionsMonitor<RedisCacheOptions> options)
    {
        _options = options?.CurrentValue ?? throw new ArgumentNullException(nameof(options));
    }

    public string GenerateCacheKey(string prefix, params object[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNullOrWhiteSpace(prefix);
        
        var stringBuilder = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(_options.InstanceName))
        {
            stringBuilder.Append(_options.InstanceName.TrimEnd(Separator));
            stringBuilder.Append(Separator);
        }

        stringBuilder.Append(prefix?.TrimEnd(Separator) ?? throw new ArgumentNullException(nameof(prefix)));

        if (args.Length > 0)
        {
            foreach (object? arg in args)
            {
                stringBuilder.Append(Separator);
                stringBuilder.Append(
                    arg is null ? "null" :
                    Convert.ToString(arg, CultureInfo.InvariantCulture)
                );
            }
        }
        return stringBuilder.ToString();
    }
}
