namespace SharedKernel.Autofac;

public enum Lifetimes
{
    Singleton,
    PerDependency,
    PerLifetimeScope,
    PerRequest
}
