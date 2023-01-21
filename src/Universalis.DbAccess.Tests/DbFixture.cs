using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FluentMigrator.Runner;
using Xunit;

namespace Universalis.DbAccess.Tests;

public class DbFixture : IAsyncLifetime
{
    private readonly TestcontainersContainer _scylla;
    private readonly TestcontainersContainer _cache;
    private readonly TestcontainersContainer _redis;
    private readonly TestcontainersContainer _postgres;

    private readonly Lazy<IServiceProvider> _services;

    public IServiceProvider Services => _services.Value;

    public DbFixture()
    {
        _scylla = new TestcontainersBuilder<TestcontainersContainer>()
            .WithName(Guid.NewGuid().ToString("D"))
            .WithImage("scylladb/scylla:5.1.0")
            .WithExposedPort(9042)
            .WithPortBinding(9042)
            .WithCommand("--smp", "1", "--overprovisioned", "1", "--memory", "512M", "--alternator-port", "8000", "--alternator-write-isolation", "only_rmw_uses_lwt")
            .WithCreateContainerParametersModifier(o =>
            {
                o.HostConfig.CPUCount = 1;
            })
            .Build();
        _cache = new TestcontainersBuilder<TestcontainersContainer>()
            .WithName(Guid.NewGuid().ToString("D"))
            .WithImage("redis:7.0.0")
            .WithPortBinding(6379, true)
            .WithCommand("redis-server", "--save", "", "--loglevel", "warning")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(6379))
            .Build();
        _redis = new TestcontainersBuilder<TestcontainersContainer>()
            .WithName(Guid.NewGuid().ToString("D"))
            .WithImage("redis:7.0.0")
            .WithPortBinding(6379, true)
            .WithCommand("redis-server", "--save", "", "--loglevel", "warning")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(6379))
            .Build();
        _postgres = new TestcontainersBuilder<TestcontainersContainer>()
            .WithName(Guid.NewGuid().ToString("D"))
            .WithImage("postgres:14.3")
            .WithEnvironment("POSTGRES_USER", "universalis")
            .WithEnvironment("POSTGRES_PASSWORD", "universalis")
            .WithPortBinding(5432, true)
            .WithCreateContainerParametersModifier(o =>
            {
                o.HostConfig.ShmSize = 512 * 1024 * 1024;
            })
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5432))
            .Build();

        _services = new Lazy<IServiceProvider>(CreateServiceProvider);
    }

    private IServiceProvider CreateServiceProvider()
    {
        Task.Delay(60000).GetAwaiter().GetResult();
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile(
                 path: "appsettings.Testing.json",
                 optional: false,
                 reloadOnChange: true)
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                { "RedisCacheConnectionString", $"{_cache.Hostname}:{_cache.GetMappedPublicPort(6379)}" },
                { "RedisConnectionString", $"{_redis.Hostname}:{_redis.GetMappedPublicPort(6379)}" },
                { "ScyllaConnectionString", "localhost" },
                { "PostgresConnectionString", $"Host={_postgres.Hostname};Port={_postgres.GetMappedPublicPort(5432)};Username=universalis;Password=universalis;Database=universalis" },
            })
            .Build();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddDbAccessServices(configuration);
        var provider = services.BuildServiceProvider();
        
        // Run database migrations
        var runner = provider.GetRequiredService<IMigrationRunner>();
        runner.MigrateUp();

        return provider;
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync().ConfigureAwait(false);
        await _scylla.StartAsync().ConfigureAwait(false);
        await _cache.StartAsync().ConfigureAwait(false);
        await _redis.StartAsync().ConfigureAwait(false);
    }

    public async Task DisposeAsync()
    {
        await _redis.DisposeAsync().ConfigureAwait(false);
        await _cache.DisposeAsync().ConfigureAwait(false);
        await _scylla.DisposeAsync().ConfigureAwait(false);
        await _postgres.DisposeAsync().ConfigureAwait(false);
    }
}
