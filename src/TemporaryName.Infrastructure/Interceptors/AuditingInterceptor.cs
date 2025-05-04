using System;
using Castle.DynamicProxy;

namespace TemporaryName.Infrastructure.Interceptors;

public class AuditingInterceptor : IInterceptor
{
    public void Intercept(IInvocation invocation)
    {
        throw new NotImplementedException();
    }
}
