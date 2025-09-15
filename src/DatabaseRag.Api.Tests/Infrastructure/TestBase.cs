using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DatabaseRag.Api.Tests.Infrastructure
{
    /// <summary>
    /// Base class for all test classes that provides common configuration and services
    /// </summary>
    public abstract class TestBase : IDisposable
    {
        protected IConfiguration Configuration { get; private set; }
        protected IServiceProvider ServiceProvider { get; private set; }
        protected ILogger Logger { get; private set; }

        protected TestBase()
        {
            // Build configuration from test settings
            var configBuilder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.Test.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables();

            Configuration = configBuilder.Build();

            // Setup dependency injection
            var services = new ServiceCollection();
            ConfigureServices(services);
            ServiceProvider = services.BuildServiceProvider();

            // Setup logging
            var loggerFactory = ServiceProvider.GetRequiredService<ILoggerFactory>();
            Logger = loggerFactory.CreateLogger(GetType());
        }

        /// <summary>
        /// Configure additional services for testing
        /// </summary>
        /// <param name="services">Service collection to configure</param>
        protected virtual void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.AddDebug();
                builder.SetMinimumLevel(LogLevel.Debug);
            });

            services.AddSingleton(Configuration);
        }

        /// <summary>
        /// Get SQL Server connection string for tests
        /// </summary>
        protected string GetSqlConnectionString()
        {
            return Configuration["TestSettings:SqlServer:ConnectionString"]
                ?? throw new InvalidOperationException("SQL Server connection string not configured for tests");
        }

        /// <summary>
        /// Get Cosmos DB connection string for tests
        /// </summary>
        protected string GetCosmosConnectionString()
        {
            return Configuration["TestSettings:CosmosDb:ConnectionString"]
                ?? throw new InvalidOperationException("Cosmos DB connection string not configured for tests");
        }

        /// <summary>
        /// Get Cosmos DB database name for tests
        /// </summary>
        protected string GetCosmosDatabaseName()
        {
            return Configuration["TestSettings:CosmosDb:DatabaseName"]
                ?? throw new InvalidOperationException("Cosmos DB database name not configured for tests");
        }

        /// <summary>
        /// Get API base URL for integration tests
        /// </summary>
        protected string GetApiBaseUrl()
        {
            return Configuration["TestSettings:ApiSettings:BaseUrl"]
                ?? throw new InvalidOperationException("API base URL not configured for tests");
        }

        public virtual void Dispose()
        {
            if (ServiceProvider is IDisposable disposableServiceProvider)
            {
                disposableServiceProvider.Dispose();
            }
            GC.SuppressFinalize(this);
        }
    }
}