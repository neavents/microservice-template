using System;
using Castle.DynamicProxy;

namespace TemporaryName.Infrastructure.Interceptors;

public class LoggingInterceptor : IInterceptor
{
    public void Intercept(IInvocation invocation)
    {
        throw new NotImplementedException();
    }
}
