using System;
using System.Reflection;
using Autofac;
using Castle.DynamicProxy;
using SharedKernel.Autofac;

using TemporaryName.Common.Autofac;
using TemporaryName.Infrastructure.Caching.Memcached;
using TemporaryName.Infrastructure.Caching.Redis;
using TemporaryName.Infrastructure.ChangeDataCapture.Debezium;
using TemporaryName.Infrastructure.HttpClient;
using TemporaryName.Infrastructure.Interceptors;
using TemporaryName.Infrastructure.Messaging.MassTransit;
using TemporaryName.Infrastructure.MultiTenancy;
using TemporaryName.Infrastructure.Observability;
using TemporaryName.Infrastructure.Outbox.EFCore;
using TemporaryName.Infrastructure.Persistence.Hybrid.Graph.Neo4j;
using TemporaryName.Infrastructure.Persistence.Hybrid.NoSql.Cassandra;
using TemporaryName.Infrastructure.Persistence.Hybrid.Olap.ClickHouseDb;
using TemporaryName.Infrastructure.Persistence.Hybrid.Sql.PostgreSQL;
using TemporaryName.Infrastructure.Persistence.Hybrid.Vector.Milvus;
using TemporaryName.Infrastructure.Security.Authorization.Extensions;
using TemporaryName.Infrastructure.Web.ExceptionHandling;

namespace TemporaryName.Infrastructure;

public class InfrastructureModule : Autofac.Module
{
    protected override void Load(ContainerBuilder builder)
    {
        base.Load(builder);

        builder
            .RegisterInterceptor<LoggingInterceptor>(InterceptorKeys.Logging)
            .RegisterInterceptor<DeepLoggingInterceptor>(InterceptorKeys.DeepLogging)
            .RegisterInterceptor<AuditingInterceptor>(InterceptorKeys.Auditing)
            .RegisterInterceptor<CachingInterceptor>(InterceptorKeys.Caching);

        builder
            .RegisterModule<RedisCacheModule>()
            .RegisterModule<DebeziumCdcModule>()
            .RegisterModule<HttpClientModule>()
            .RegisterModule<MassTransitModule>()
            .RegisterModule<ObservabilityModule>()
            .RegisterModule<PostgreSqlApplicationDataModule>()
            .RegisterModule<AuthorizationModule>()
            .RegisterModule<MemcachedCacheModule>()
            .RegisterModule<MultiTenancyModule>()
            .RegisterModule<OutboxEFCoreModule>()
            .RegisterModule<Neo4jModule>()
            .RegisterModule<CassandraModule>()
            .RegisterModule<ClickHouseDbModule>()
            .RegisterModule<MilvusModule>()
            .RegisterModule<ExceptionHandlingModule>();

        IEnumerable<Assembly> assemblies = [
            ThisAssembly,
            typeof().Assembly,
            //Add your assemblies
            ];

        IEnumerable<string> namespaces = [
            "TemporaryName.Infrastructure.ADDYOURNAMESPACES",
        ];

        builder.RegisterServicesByConvention(assemblies, namespaces);
    }
}
