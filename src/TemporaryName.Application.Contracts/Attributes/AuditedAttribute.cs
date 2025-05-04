using System;
using Autofac.Extras.DynamicProxy;
using SharedKernel.Autofac;

namespace TemporaryName.Application.Contracts.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true)]
public sealed class AuditedAttribute : InterceptAttribute
{
    public AuditedAttribute() : base(InterceptorKeys.Auditing) { }
}
