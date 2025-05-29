# .NET Microservice Base Template (Neavents)

[![.NET Version](https://img.shields.io/badge/.NET-9.0-blueviolet)](https://dotnet.microsoft.com/download/dotnet/9.0)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE.md) [![PRs Welcome](https://img.shields.io/badge/PRs-welcome-brightgreen.svg)](#contributing)

**Current Version:** 1.0-earlyalpha

A foundational .NET microservice template following clean architecture practices, designed to accelerate development for projects like Neavents. This template is packed with enterprise-grade features, promoting robustness, scalability, and maintainability. (NOT FOR PRODUCTION RIGHT NOW! USE WITH CAUTION)


---


## Overview

This template, provides a well-structured starting point for building .NET 9 microservices. It integrates a wide array of modern technologies and patterns, aiming to reduce boilerplate and enforce best practices from the outset. The `sourceName` for generated projects is `TemporaryName`, which you will replace with your actual service name when instantiating the template. This project has some very useful scripts you will love to use at `/scripts` directory.

## Key Features

* **Clean Architecture:** Promotes separation of concerns with distinct layers for Domain, Application, Infrastructure, and Presentation (API/Workers).
* **.NET 9:** Leverages the latest features and performance improvements from the .NET platform.
* **Autofac for Dependency Injection:** Utilizes Autofac for powerful DI capabilities, including convention-based registration and interceptors.
* **Domain-Driven Design Primitives:** Includes base classes and interfaces for Entities, Aggregate Roots, Value Objects, Domain Events, and Specifications.
* **Result Pattern:** Uses a `Result` pattern for clear and robust error handling in service layers.
* **AOP with Interceptors:**
    * **Logging:** Automatically log method entry, exit, duration, and exceptions.
    * **Auditing:** Capture audit trails for method invocations.
    * **Caching:** Placeholder for caching interceptor.
    * **Deep Logging:** Placeholder for more detailed logging.
* **API Layer (ASP.NET Core):**
    * Standard Web API setup.
    * Global Exception Handling Middleware mapping exceptions to `ProblemDetails`.
* **Background Workers:**
    * **Hangfire:** Support for Hangfire background job processing.
    * **Quartz.NET:** Support for Quartz.NET scheduled jobs.
* **Caching:**
    * **Redis:** Integration with Redis for distributed caching.
    * **Memcached:** Integration with Memcached.
    * Abstracted `ICacheService` and `ICacheKeyService`.
* **Change Data Capture (CDC) with Debezium & Kafka:**
    * Framework for consuming Debezium events from Kafka.
    * Outbox message processor for relaying events captured from an outbox table.
* **Configuration Management:**
    * Standardized loading of `appsettings.json`, environment-specific files, assembly-specific files, environment variables, and command-line arguments.
    * Dedicated settings files for caching, MassTransit, observability, persistence, and security.
* **Database & Persistence:**
    * **EF Core with PostgreSQL:** Default relational database setup.
    * **Transactional Outbox Pattern:** Integrated with EF Core to ensure atomic persistence of business data and outgoing events.
    * **Database Migrations Tool:** CLI tool for managing EF Core migrations (`add`, `remove`, `apply`).
    * **Database Seeding Tool:** Placeholder for a data seeding utility.
    * **Multiple Database Support (Hybrid Persistence):**
        * **Neo4j:** Graph database integration.
        * **Cassandra:** NoSQL wide-column store integration.
        * **ClickHouse:** OLAP column-oriented database integration.
        * **Milvus:** Vector database integration (placeholder).
* **Deployment & Orchestration:**
    * **Docker:** Dockerfile provided for containerizing the Web API. (Assumes Dockerfile exists in `src/TemporaryName.WebApi/`)
    * **Docker Compose:** Scripts for managing local development environments.
    * **Kubernetes:**
        * Deployment, Service, HorizontalPodAutoscaler (HPA), PodDisruptionBudget (PDB), NetworkPolicy manifests.
        * **Linkerd Integration:** Manifests for ServiceAccount, Server, ServerAuthorization, and HTTPRoute for Linkerd service mesh.
* **Messaging with MassTransit & RabbitMQ:**
    * Robust configuration for RabbitMQ, including connection, error handling, retries, dead-lettering, and consumer features (concurrency, circuit breaker, rate limiter, outbox).
    * Protobuf serialization support.
    * Message scheduling with RabbitMQ's delayed exchange plugin.
    * `IIntegrationEventPublisher` for publishing events via MassTransit, with dynamic mapping from POCO domain events to Protobuf messages.
* **Multi-Tenancy:**
    * Comprehensive multi-tenancy support including tenant resolution strategies (Host, Header, QueryString, Route, Claim), tenant stores (Configuration, Database, Remote HTTP, InMemory), and tenant context.
    * Tenant-scoped DbContext and data isolation primitives.
    * Caching for tenant information.
* **Observability:**
    * **Logging:** Structured logging with Serilog, configured for console and Elasticsearch.
    * **Tracing & Metrics with OpenTelemetry:** Configured for OTLP export, with various instrumentations (ASP.NET Core, HttpClient, EF Core, MassTransit, Runtime).
    * **Elastic APM:** Integration with Elastic APM for performance monitoring and distributed tracing. (This seems to be the primary APM/Observability focus as per `observabilitysettings.json` contents and DI logic.)
* **Security:**
    * **Authorization:** Core framework for permission-based and policy-based authorization. Includes requirements and handlers for "HasPermission", "HasAllPermissions", "HasAnyPermission".
    * **Secret Management (HashiCorp Vault):** Client provider and secret manager for HashiCorp Vault.
    * Keycloak integration placeholder.
* **Developer Experience:**
    * Extensive shell scripts for common development tasks: build, format (csharpier), lint, test, publish projects, run API/workers, manage Docker Compose.
    * `.editorconfig` and solution-wide `global.json` (implied for SDK version management).
    * `Directory.Build.props` and `Directory.Build.targets` (implied for centralized project settings).
* **Testing:** Standard xUnit project structure for different layers.
* **Versioning:** Configured for Nerdbank.GitVersioning.


---


## Getting Started

### Prerequisites

* .NET SDK 9.0 or later.
* Docker Desktop (for local containerization and Docker Compose).
* (Optional) Kubernetes cluster (e.g., minikube, kind, Docker Desktop Kubernetes) for deployment.
* (Optional) Linkerd CLI and control plane installed on your Kubernetes cluster.
* (Optional) RabbitMQ instance.
* (Optional) PostgreSQL instance.
* (Optional) Redis instance.
* (Optional) Memcached instance.
* (Optional) HashiCorp Vault instance.
* (Optional) Debezium, Kafka, Schema Registry setup for CDC features.
* (Optional) Elasticsearch, Kibana, APM Server for observability features.

### Installation

1.  **Install the template:**
    (NuGet is not supported for now)
    For local development (after cloning this repository):
    ```bash
    dotnet new install path/to/neavents-microservice-template
    ```

2.  **Create a new microservice from the template:**
    ```bash
    dotnet new neavents-microservice-template -n MyAwesomeService -o MyAwesomeServiceOutput
    # Replace MyAwesomeService with your desired service name.
    # This will replace "TemporaryName" throughout the template.
    ```
    This command creates a new solution in the `MyAwesomeServiceOutput` directory. The `sourceName` "TemporaryName" in the template files will be replaced by "MyAwesomeService".

### Running the Service

#### Using .NET Run Scripts

Several scripts are provided in the `/scripts` directory:

* **Build:** `./scripts/build.sh` - Builds the entire solution in Release configuration.
* **Run API:** `./scripts/run-api.sh` - Runs the Web API project using the `https` launch profile.
* **Run Workers:**
    * `./scripts/run-worker-hangfire.sh`
    * `./scripts/run-worker-quartz.sh`
* **Publish Projects:**
    * `./scripts/publish-all.sh` - Publishes WebApi, Hangfire worker, and Quartz worker.
    * `./scripts/publish-api.sh`
    * `./scripts/publish-worker-hangfire.sh`
    * `./scripts/publish-worker-quartz.sh`

Make sure scripts are executable (`chmod +x ./scripts/*.sh`).

#### Using Docker Compose

* **Start services:** `./scripts/docker-compose-up.sh` - Builds images if necessary and starts services in detached mode.
* **Stop services:** `./scripts/docker-compose-down.sh` - Stops and removes containers.
* **View logs:** `./scripts/docker-compose-logs.sh [service_name]` - Tails logs for all services or a specific one.
* **Build API Docker Image:** `./scripts/docker-build-api.sh [image_name] [tag]` - Builds the API Docker image.


---


## Project Structure (Simplified)

After generating a project named `YourServiceName` from the `TemporaryName` source:

- YourServiceName/
  - .template.config/         # Template configuration (excluded from generated project)
  - kubernetes/                 # Kubernetes manifests
    - linkerd/              # Linkerd specific manifests
    - deployment-template.yaml
    - ... (other k8s YAMLs)
  - scripts/                    # Utility shell scripts
  - src/
    - YourServiceName.Application/
    - YourServiceName.Application.Contracts/
    - YourServiceName.Domain/
    - YourServiceName.Infrastructure/
      - Caching.Memcached/
      - Caching.Redis/
      - ChangeDataCapture.Debezium/
      - Configuration/
      - DataAccess/
      - Hosting.Extensions/
      - HttpClient/
      - Messaging.MassTransit/
      - MultiTenancy/
      - Observability/
      - Outbox.EFCore/
      - Persistence.Common.EFCore/
      - Persistence.Hybrid.Graph.Neo4j/
      - Persistence.Hybrid.NoSql.Cassandra/
      - Persistence.Hybrid.Olap.ClickHouseDb/
      - Persistence.Hybrid.Sql.PostgreSQL/
      - Persistence.Hybrid.Vector.Milvus/ (Placeholder)
      - Persistence.Seeding/ (Placeholder)
      - Security.Auth.Keycloak/ (Placeholder)
      - Security.Authorization/
      - Security.Secrets.HashiCorpVault/
      - Web.ExceptionHandling/
    - YourServiceName.WebApi/
    - YourServiceName.Worker.Hangfire/
    - YourServiceName.Worker.Quartz/
    - SharedKernel/             # Core primitives, shared across layers
  - tests/                      # Unit and integration tests for each layer
    - YourServiceName.Application.Tests/
    - ...
  - tools/
    - YourServiceName.Tools.Persistence.Migrations/
    - YourServiceName.Tools.Persistence.Seeding/ (Placeholder)
  - YourServiceName.sln
  - README.md                   # This file!
  - .editorconfig
  - .gitignore
  - version.json                # Nerdbank.GitVersioning configuration


---


## Core Concepts & Design Decisions

### Clean Architecture

The template adheres to Clean Architecture principles, ensuring:

* **Independence of Frameworks:** The core business logic (Domain, Application) is independent of frameworks like ASP.NET Core, EF Core, etc.
* **Testability:** Layers are designed to be testable in isolation.
* **Dependency Rule:** Dependencies flow inwards. UI/Web/Infrastructure depend on Application/Domain, but not vice-versa.

### Domain-Driven Design (DDD) Primitives

The `YourServiceName.Domain` project includes base classes and interfaces to support DDD concepts:

* **Entities & Aggregate Roots:** `Entity<TId>` and `AggregateRoot<TId>` provide foundational elements for domain modeling, including domain event management.
* **Value Objects:** A base `ValueObject` class for implementing immutable value types.
* **Domain Events:** `IDomainEvent` and `DomainEvent` base record for capturing significant occurrences within the domain.
* **Specifications:** `ISpecification<T>` and `Specification<T>` base class for defining reusable query and command criteria.
* **Auditing Interfaces:** `IAuditable`, `ICreatedAuditable`, `IModifiedAuditable` for entities that require audit trails.
* **Soft Deletion:** `ISoftDeletable` interface and `SoftDeletableEntity<TId>` base class.

### Result Pattern

The `SharedKernel.Primitives.Result` and `Result<TValue>` classes are used to handle operations that can succeed or fail, providing a clear and explicit way to return outcomes and errors without relying on exceptions for control flow. This pattern improves error handling and makes method signatures more expressive.

### Error Primitives

`SharedKernel.Primitives.Error` and `ErrorType` provide a structured way to define and categorize errors throughout the application. Custom domain and infrastructure exceptions often wrap these `Error` objects.

### Interceptors (Aspect-Oriented Programming)

Autofac interceptors are used for cross-cutting concerns:

* **`LoggingInterceptor`:** Logs method entry, arguments, exit, return values, and duration.
* **`AuditingInterceptor`:** Creates audit logs for intercepted method calls, capturing user, action, and outcome.
* **`CachingInterceptor`:** (Placeholder) Intended for method-level caching.
* **`DeepLoggingInterceptor`:** (Placeholder) Intended for more verbose logging if needed.

These are registered in `TemporaryName.Infrastructure/InfrastructureModule.cs` and can be applied to services using attributes from `TemporaryName.Application.Contracts/Attributes/`.


---


## Feature Deep Dive

### API (ASP.NET Core)

* **Project:** `src/YourServiceName.WebApi`
* Standard ASP.NET Core Web API setup using controllers or minimal APIs.
* **Global Exception Handling:** The `GlobalExceptionHandlingMiddleware` catches unhandled exceptions and transforms them into standardized `ProblemDetails` (RFC 7807) responses. It uses a factory pattern with specific mappers for different exception types (e.g., `ValidationDomainExceptionMapper`, `NotFoundDomainExceptionMapper`).
* **Configuration:** API settings are managed via `appsettings.json` and environment-specific overrides. Additional settings for caching, MassTransit, observability etc., are loaded from dedicated files (`cachingsettings.json`, `masstransitsettings.json`, etc.) by the `SettingsConfigurator`.

### Background Workers

* **Hangfire (`src/YourServiceName.Worker.Hangfire`):**
    * Provides a project structure for hosting Hangfire background jobs.
    * Configuration for connecting to Hangfire's storage (e.g., SQL Server, Redis) would be done in its `Program.cs` and `appsettings.json`.
* **Quartz.NET (`src/YourServiceName.Worker.Quartz`):**
    * Provides a project structure for hosting Quartz.NET scheduled jobs.
    * Jobs and triggers would be defined and scheduled within this project.

### Caching

* **Abstraction:** `ICacheService` and `ICacheKeyService` in `TemporaryName.Infrastructure.Caching.Abstractions` provide a common interface for caching operations.
* **Redis (`src/TemporaryName.Infrastructure.Caching.Redis`):**
    * Implementation of `ICacheService` using StackExchange.Redis.
    * Configuration via `RedisCacheOptions` (connection string, instance name, SSL).
* **Memcached (`src/TemporaryName.Infrastructure.Caching.Memcached`):**
    * Implementation of `ICacheService` using EnyimMemcachedCore.
    * Configuration via `MemcachedCacheOptions` (servers, instance name, auth).
* **Provider Selection:** `cachingsettings.json` defines active providers and a default provider. The `CachingLayerInjection` in `TemporaryName.WebApi` handles conditional registration.

### Change Data Capture (Debezium + Kafka)

* **Project:** `src/YourServiceName.Infrastructure.ChangeDataCapture.Debezium`
* **Purpose:** Enables capturing database changes (e.g., from an outbox table) using Debezium and processing them via Kafka.
* **`GenericDebeziumConsumer`:** A hosted service for consuming messages from Kafka topics populated by Debezium. It handles Protobuf deserialization (with Schema Registry) and delegates message processing to an `IDebeziumEventHandler`.
* **`KafkaOutboxMessageProcessor` & `KafkaOutboxMessageSource`:** These classes are specifically designed to work with the transactional outbox pattern.
    * `KafkaOutboxMessageSource` consumes Protobuf-encoded outbox messages (as defined in `TemporaryName.Contracts.Proto.Outbox.V1.OutboxMessageProto`) from a Kafka topic that Debezium populates.
    * It transforms these into `ConsumedOutboxMessage` instances for the `OutboxEventRelayService`.
* **Configuration:** `KafkaConsumerSettings` and `KafkaOutboxConsumerSettings` for Kafka connection, topics, group ID, Schema Registry, and retry policies.

### Configuration

* **Standardized Loading:** `HostBuilderExtensions.ConfigureStandardAppConfiguration` ensures a consistent configuration loading order:
    1.  Common appsettings (`appsettings.common.json`, `appsettings.common.{Environment}.json`) - relative path configurable.
    2.  Application-specific appsettings (`appsettings.json`, `appsettings.{Environment}.json`).
    3.  Assembly-specific appsettings (`appsettings.{AssemblyName}.json`, `appsettings.{AssemblyName}.{Environment}.json`).
    4.  Environment Variables.
    5.  Command-line arguments.
   
* **Dedicated Settings Files:** For complex modules like MassTransit, Caching, Observability, Persistence, and Security, settings are typically organized into separate JSON files (e.g., `masstransitsettings.json`) and loaded by the `SettingsConfigurator` in the WebApi project.

### Database & Migrations (EF Core + PostgreSQL)

* **Default ORM:** Entity Framework Core.
* **Default Database:** PostgreSQL.
* **DbContext:**
    * `ApplicationDbContextBase`: Common configurations, DbSet for `OutboxMessage`.
    * `OutboxEnabledDbContextBase`: Inherits from `DbContext` and includes `OutboxMessages` DbSet and base configuration for it.
    * `PostgreSqlApplicationDbContext`: PostgreSQL-specific implementation, inherits from `OutboxEnabledDbContextBase` and applies provider-specific configurations (e.g., `jsonb` column types).
* **Migrations Tool (`tools/YourServiceName.Tools.Persistence.Migrations`):**
    * A Spectre.Console CLI application for managing database migrations.
    * Commands: `add <NAME>`, `remove`, `apply`.
    * Supports EF Core migrations for PostgreSQL by default.
    * Uses `ConnectionStringResolver` to pick up connection strings from various sources.
    * The `EfCoreMigrationRunner` executes `dotnet ef` commands.
    * Uses a `DesignTimePlaceholderDbContextFactory` for EF Core tools, which should be adapted to your actual DbContext.
* **Data Seeding Tool (`tools/YourServiceName.Tools.Persistence.Seeding`):**
    * A placeholder project for implementing database seeding logic.

### Deployment (Docker, Kubernetes, Linkerd)

* **Docker:**
    * `docker-build-api.sh`: Script to build the Web API Docker image. (Assumes a `Dockerfile` in `src/YourServiceName.WebApi/`).
    * Docker Compose scripts (`docker-compose-up.sh`, `docker-compose-down.sh`, `docker-compose-logs.sh`) for local multi-container orchestration.
* **Kubernetes (`kubernetes/`):**
    * `deployment-template.yaml`: Defines a Deployment with replicas, selector, service account, resource requests/limits, and readiness/liveness probes configured for Linkerd.
    * `service-svc-template.yaml`: Defines a ClusterIP Service to expose the deployment internally.
    * `scaler-hpa-template.yaml`: HorizontalPodAutoscaler configured to scale based on CPU utilization, with examples for memory or custom metrics.
    * `disruption-pdb-template.yaml`: PodDisruptionBudget to ensure a minimum number of pods are available during voluntary disruptions.
    * `netpol-template.yaml`: NetworkPolicy for restricting ingress and egress traffic, with examples for allowing traffic from specific namespaces/pods (e.g., API gateway, other services, DNS).
* **Linkerd (`kubernetes/linkerd/`):**
    * `service-account-sa-template.yaml`: Defines a ServiceAccount for the service.
    * `server-template.yaml`: Linkerd `Server` CRD to define a logical server for policy enforcement, selecting pods and a port.
    * `serverauthorization-template.yaml`: Linkerd `ServerAuthorization` CRD to define which clients are allowed to call this server (mTLS identities).
    * `httproute-template.yaml`: Kubernetes Gateway API `HTTPRoute` (can be used by Linkerd) for defining L7 routing rules, timeouts, etc.
    * The `deployment-template.yaml` includes `linkerd.io/inject: enabled` annotation.

### Exception Handling

* **Project:** `src/YourServiceName.Infrastructure.Web.ExceptionHandling`
* **`GlobalExceptionHandlingMiddleware`:** Catches unhandled exceptions.
* **`ProblemDetailsFactory`:** Uses registered `IExceptionProblemDetailsMapper` instances to convert exceptions into RFC 7807 `ProblemDetails` objects. Mappers are ordered and selected based on exception type specificity.
* **Mappers:** Specific mappers are provided for common exception types (e.g., `ArgumentException`, `NotFoundDomainException`, `ValidationDomainException`, `HttpRequestException`) and a `DefaultExceptionMapper` for general exceptions.
* **Configuration:** `GlobalExceptionHandlingOptions` allows customization like `IncludeStackTrace` and `ProblemTypeUriBase`.

### Messaging (MassTransit + RabbitMQ)

* **Project:** `src/YourServiceName.Infrastructure.Messaging.MassTransit`
* **Core:** Uses MassTransit for message bus abstraction with RabbitMQ as the default transport.
* **Configuration (`masstransitsettings.json` and `RabbitMq*.cs` options classes):**
    * Connection details for RabbitMQ (`RabbitMqConnectionOptions`).
    * Global error handling (retries, redelivery) (`GlobalErrorHandlingOptions`).
    * Endpoint-specific features:
        * Concurrency limits (`EndpointConcurrencyOptions`).
        * Circuit breaker (`EndpointCircuitBreakerOptions`).
        * Rate limiter (`EndpointRateLimiterOptions`).
        * Consumer Outbox (for idempotent consumers) (`EndpointConsumerOutboxOptions`).
        * Consumer Timeout (`EndpointConsumerTimeoutOptions`).
        * Quorum Queues (`EndpointQuorumQueueOptions`).
    * Health checks (`RabbitMqHealthCheckOptions`).
* **Serialization:** Configured for Protobuf.
* **Scheduling:** Uses RabbitMQ's delayed message exchange for scheduled messages.
* **Transactional Outbox (Producer side):** Integrated with EF Core. The `OutboxEventRelayService` (see CDC section) consumes from Kafka (populated by Debezium from the outbox table) and uses `MassTransitIntegrationEventPublisher` to publish to RabbitMQ.
* **Setup:** `DependencyInjection.AddMassTransitLayer` and `RabbitMqBusFactoryExtensions.AddConfiguredMassTransitWithAssemblyScanning` handle the main setup.

### Multi-Tenancy

* **Project:** `src/YourServiceName.Infrastructure.MultiTenancy`
* **Core:** Provides a flexible multi-tenancy framework.
* **Tenant Resolution:**
    * `TenantResolutionMiddleware` orchestrates tenant identification.
    * Strategies: Host Header, HTTP Header, Query String, Route Value, Claim. Each configurable via `TenantResolutionStrategyOptions`.
    * `ITenantStrategyProvider` selects the appropriate strategy.
* **Tenant Stores:**
    * `ITenantStore` defines the contract for fetching tenant information.
    * Implementations:
        * `ConfigurationTenantStore`: Reads from `MultiTenancyOptions.Tenants` in `appsettings.json`.
        * `DatabaseTenantStore`: Fetches from a database (connection configured via `TenantStoreOptions.ConnectionStringName`).
        * `RemoteHttpTenantStore`: Fetches from a remote HTTP endpoint.
        * `InMemoryTenantStore`: For testing or dynamic scenarios.
    * `ITenantStoreProvider` selects the store based on `TenantStoreOptions.Type`.
* **Tenant Context:** `ITenantContext` holds the resolved `ITenantInfo` for the current request (scoped).
* **Caching:** `CachingTenantStoreInterceptor` (via Autofac decorator) caches `ITenantStore` results.
* **Data Isolation:**
    * `ITenantScopedEntity` interface for entities that belong to a tenant.
    * `TenantScopedDbContextBase` automatically applies query filters based on the resolved `CurrentDbContextTenantId` and handles TenantId assignment on save.
* **Configuration:** `MultiTenancyOptions` is the main configuration class.

### Observability

* **Project:** `src/YourServiceName.Infrastructure.Observability`
* **Logging (Serilog):**
    * Configured via `HostBuilderExtensions.ConfigureStandardAppConfiguration` and `DependencyInjection.ConfigureSerilogForElk`.
    * Includes console sink and an Elasticsearch sink.
    * Enriches logs with ApplicationName, Version, Environment, MachineName, ProcessID, ThreadID, and exception details.
    * Configuration in `ObservabilityOptions` (`SerilogSettings`, `ElasticsearchSinkSettings`).
* **Tracing & Metrics (OpenTelemetry):**
    * Configured in `TemporaryName.WebApi/Configurators/OpenTelemetryConfigurator.cs` if `MassTransitOptions.EnableOpenTelemetry` is true.
    * Sets up tracing with sources for MassTransit.
    * The `ObservabilityOptions` in `observabilitysettings.json` provides detailed OTLP exporter settings for traces, metrics, and logs, and fine-grained control over instrumentations.
* **APM (Elastic APM):**
    * `services.AddConfiguredAppElasticApm()` in `TemporaryName.Infrastructure/DependencyInjection.cs` sets up the Elastic APM agent based on `ObservabilityOptions.ElasticApm` settings.
    * Collects performance metrics and distributed traces.

### Security

* **Authorization (`src/YourServiceName.Infrastructure.Security.Authorization`):**
    * Provides a robust, claims-based authorization framework.
    * **Permissions:** Defined as constants (e.g., `ProductsPermissions.View`). A `PermissionAggregator` can list all defined permissions.
    * **Requirements:** `HasPermissionRequirement`, `HasAllPermissionsRequirement`, `HasAnyPermissionRequirement`.
    * **Handlers:** Corresponding authorization handlers that check for the `permission` claim (constant defined in `AuthorizationClaimTypes`).
    * **Policies:** Predefined policies (e.g., `ProductsPolicyNames.ViewProducts`) that map to these requirements.
    * Registered via `AddAppAuthorizationCore` and Autofac module.
* **Secret Management (HashiCorp Vault - `src/YourServiceName.Infrastructure.Security.Secrets.HashiCorpVault`):**
    * `ISecretManager` abstraction for retrieving secrets.
    * `HashiCorpVaultSecretManager` implementation using `VaultSharp`.
    * `IVaultClientProvider` and `VaultClientProvider` to manage the Vault client lifecycle.
    * Configuration via `HashiCorpVaultOptions` (address, auth method, token, AppRole, Kubernetes). Settings are in `securitysettings.json`.
* **Authentication (Placeholder):**
    * `TemporaryName.Infrastructure.Security.Auth.Keycloak` project exists but is empty, suggesting a placeholder for Keycloak integration. Authentication would typically be handled by middleware like `Microsoft.AspNetCore.Authentication.JwtBearer`.

### Other Persistence Options

The template includes project structures and basic DI setup for several other database technologies, indicating a flexible approach to data storage:

* **Neo4j (`src/YourServiceName.Infrastructure.Persistence.Hybrid.Graph.Neo4j`):** For graph data. Includes `INeo4jClientProvider` and `Neo4jOptions`.
* **Cassandra (`src/YourServiceName.Infrastructure.Persistence.Hybrid.NoSql.Cassandra`):** For wide-column NoSQL data. Includes `ICassandraSessionProvider` and `CassandraOptions`.
* **ClickHouse (`src/YourServiceName.Infrastructure.Persistence.Hybrid.Olap.ClickHouseDb`):** For OLAP and analytical workloads. Includes `IClickHouseConnectionProvider` and `ClickHouseOptions`.
* **Milvus (`src/YourServiceName.Infrastructure.Persistence.Hybrid.Vector.Milvus`):** Placeholder for vector database integration, likely for AI/ML embedding similarity searches.


---


## Scripts and Tooling

The `/scripts` directory contains several utility scripts to streamline common development tasks:

* **`build.sh`**: Builds the solution.
* **`db-add-migration.sh <MigrationName>`**: Adds a new EF Core migration using the custom migration tool.
* **`db-migrate.sh`**: Applies pending EF Core migrations using the custom migration tool.
* **`db-remove-migration.sh`**: Removes the last EF Core migration using the custom migration tool.
* **`db-seed.sh`**: (Placeholder) Runs the database seeding tool.
* **`docker-build-api.sh`**: Builds the API's Docker image.
* **`docker-compose-*.sh`**: Scripts for Docker Compose (`up`, `down`, `logs`).
* **`format.sh`**: Formats code using CSharpier.
* **`lint.sh`**: Checks code formatting and runs analyzers.
* **`publish-*.sh`**: Scripts for publishing the API and worker projects.
* **`run-*.sh`**: Scripts for running the API and worker projects locally.
* **`test.sh`**: Runs tests for the solution.
* **`commit-and-push.sh "Your commit message"`**:
    * **Purpose:** Automates the process of staging all current changes, committing them with a user-provided message, and pushing the current branch to its remote counterpart.
    * **Arguments:**
        * `"Your commit message"` (Required): The commit message, enclosed in quotes.
    * **Behavior:**
        * Ensures it runs from the project's absolute root directory (navigates if necessary).
        * Executes `git add .` to stage all changes (new, modified, deleted).
        * Executes `git commit -m "MESSAGE"` using the provided message.
        * Executes `git push`.
        * Returns the user to the directory they were in when the script was invoked.
        * Includes basic error handling and will exit if Git commands fail.
    * **Usage Example:**
        ```bash
        ./scripts/commit-and-push.sh "feat: Add initial user authentication endpoints"
        ```
* **`merge-and-push.sh <feature-branch> <target-branch>`**:
    * **Purpose:** Automates merging a feature branch into a target branch (e.g., `main` or `develop`), pushing the updated target branch, and then switching back to the original branch.
    * **Arguments:**
        * `<feature-branch>` (Required): The name of the branch you want to merge (e.g., `my-feature-branch`).
        * `<target-branch>` (Required): The name of the branch you want to merge into (e.g., `main`, `develop`).
    * **Behavior:**
        * Records the current branch the user is on.
        * Ensures it runs from the project's absolute root directory.
        * Executes `git fetch --all --prune` to get the latest remote state.
        * Switches to the `<target-branch>`.
        * Executes `git pull origin <target-branch>` to ensure the target branch is up-to-date.
        * Executes `git merge --no-ff <feature-branch>` to merge the feature branch, always creating a merge commit.
        * Executes `git push origin <target-branch>`.
        * Switches back to the original branch the user was on before running the script.
        * Includes error handling. If a merge conflict occurs, the script will exit, and manual resolution is required.
    * **Usage Example:**
        ```bash
        # Assuming you want to merge 'feature/user-profile' into 'develop'
        ./scripts/merge-and-push.sh feature/user-profile develop
        ```


---


## Configuration Files Overview

Key configuration files and their purpose:

* **`appsettings.json` (in WebApi, Workers):** Base application settings, logging levels.
* **`appsettings.{Environment}.json`:** Environment-specific overrides.
* **`AppSettings/cachingsettings.json`:** Configuration for Redis, Memcached, etc.
* **`AppSettings/masstransitsettings.json`:** Detailed configuration for MassTransit, RabbitMQ connections, endpoints, retries, etc.
* **`AppSettings/observabilitysettings.json`:** Settings for OpenTelemetry, Serilog sinks (like Elasticsearch), and Elastic APM.
* **`AppSettings/securitysettings.json`:** Configuration for security features like HashiCorp Vault.
* **`AppSettings/persistencesettings.json` (Implied by SettingsConfigurator):** Would contain connection strings and settings for various persistence options.
* **`kubernetes/` files:** Contain placeholders like `your-registry/neavents-venue-service:latest` which need to be replaced with actual values. Many `template-*` names also need customization.
* **`version.json`:** Nerdbank.GitVersioning configuration.


---


## Testing

The template includes a standard `/tests` directory with placeholder unit test projects for each layer (e.g., `TemporaryName.Application.Tests`, `TemporaryName.Domain.Tests`, `TemporaryName.Infrastructure.Tests`). These projects are set up with xUnit.

Use the `./scripts/test.sh` script to run all tests.


---


## Contributing

Contributions are welcome! Please follow standard GitHub flow: Fork, Branch, Commit, Pull Request.


---


## License

This project is licensed under the MIT License. See the LICENSE file for details.