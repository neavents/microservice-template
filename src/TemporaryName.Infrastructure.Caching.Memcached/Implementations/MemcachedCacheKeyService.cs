using System;
using System.Globalization;
using System.Text;
using Microsoft.Extensions.Options;
using TemporaryName.Infrastructure.Caching.Abstractions;
using TemporaryName.Infrastructure.Caching.Memcached.Settings;

namespace TemporaryName.Infrastructure.Caching.Memcached.Implementations;

public class MemcachedCacheKeyService : ICacheKeyService
{
    private readonly MemcachedCacheOptions _options;
    private const char Separator = ':';

    public MemcachedCacheKeyService(IOptions<MemcachedCacheOptions> options)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public string GenerateCacheKey(string prefix, params object[] args)
    {
        ArgumentNullException.ThrowIfNull(prefix);

        var stringBuilder = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(_options.InstanceName))
        {
            stringBuilder.Append(_options.InstanceName.TrimEnd(Separator));
            stringBuilder.Append(Separator);
        }

        stringBuilder.Append(prefix.TrimEnd(Separator));

        if (args != null)
        {
            foreach (object? arg in args)
            {
                stringBuilder.Append(Separator);
                stringBuilder.Append(
                    arg is null ? "null" :
                    Convert.ToString(arg, CultureInfo.InvariantCulture)?.Replace(" ", "_", StringComparison.InvariantCulture)
                );
            }
        }
        // Memcached keys have restrictions (no whitespace, control chars, often max 250 bytes)
        // more robust sanitization might be needed.
        return stringBuilder.ToString();
    }
}
