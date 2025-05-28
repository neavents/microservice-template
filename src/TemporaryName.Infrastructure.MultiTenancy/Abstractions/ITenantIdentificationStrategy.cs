using System;
using Microsoft.AspNetCore.Http;

namespace TemporaryName.Infrastructure.MultiTenancy.Abstractions;

public interface ITenantIdentificationStrategy
{
    public int Priority { get; }
    public Task<string?> IdentifyTenantAsync(HttpContext context);
}
