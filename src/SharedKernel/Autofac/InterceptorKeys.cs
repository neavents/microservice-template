using System;

namespace SharedKernel.Autofac;

public static class InterceptorKeys
{
    public const string Logging = "interceptor.logging";
    public const string DeepLogging = "interceptor.deepLogging";
    public const string Auditing = "interceptor.auditing";
    public const string Caching = "interceptor.caching";
}