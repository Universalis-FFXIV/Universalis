using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FluentMigrator.Runner;
using Universalis.Common.GameData;
using Xunit;

namespace Universalis.DbAccess.Tests;

public class DbFixture : IAsyncLifetime
{
    private readonly IContainer _scylla;
    private readonly IContainer _cache1;
    private readonly IContainer _cache2;
    private readonly IContainer _redis;
    private readonly IContainer _postgres;

    private readonly Lazy<IServiceProvider> _services;

    public IServiceProvider Services => _services.Value;

    public DbFixture()
    {
        _scylla = new ContainerBuilder()
            .WithName(Guid.NewGuid().ToString("D"))
            .WithImage("scylladb/scylla:5.1.3")
            .WithExposedPort(9042)
            .WithPortBinding(9042)
            .WithCommand("--smp", "1", "--overprovisioned", "1", "--memory", "512M")
            .WithCreateParameterModifier(o =>
            {
                o.HostConfig.CPUCount = 1;
            })
            .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("Scylla .* initialization completed."))
            .Build();
        _cache1 = new ContainerBuilder()
            .WithName(Guid.NewGuid().ToString("D"))
            .WithImage("redis:7.0.8")
            .WithPortBinding(6379, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(6379))
            .Build();
        _cache2 = new ContainerBuilder()
            .WithName(Guid.NewGuid().ToString("D"))
            .WithImage("redis:7.0.8")
            .WithPortBinding(6379, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(6379))
            .Build();
        _redis = new ContainerBuilder()
            .WithName(Guid.NewGuid().ToString("D"))
            .WithImage("redis:7.0.8")
            .WithPortBinding(6379, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(6379))
            .Build();
        _postgres = new ContainerBuilder()
            .WithName(Guid.NewGuid().ToString("D"))
            .WithImage("postgres:14.6")
            .WithEnvironment("POSTGRES_USER", "universalis")
            .WithEnvironment("POSTGRES_PASSWORD", "universalis")
            .WithPortBinding(5432, true)
            .WithCreateParameterModifier(o =>
            {
                o.HostConfig.ShmSize = 512 * 1024 * 1024;
            })
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5432))
            .Build();

        _services = new Lazy<IServiceProvider>(CreateServiceProvider);
    }

    private IServiceProvider CreateServiceProvider()
    {
        Task.WhenAll(_scylla.StartAsync(), _cache1.StartAsync(), _cache2.StartAsync(), _redis.StartAsync(), _postgres.StartAsync()).GetAwaiter().GetResult();
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile(
                 path: "appsettings.Testing.json",
                 optional: false,
                 reloadOnChange: true)
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                { "RedisCacheConnectionString", $"{_cache1.Hostname}:{_cache1.GetMappedPublicPort(6379)},{_cache2.Hostname}:{_cache2.GetMappedPublicPort(6379)}" },
                { "RedisConnectionString", $"{_redis.Hostname}:{_redis.GetMappedPublicPort(6379)},allowAdmin=true" },
                { "ScyllaConnectionString", "localhost" },
                { "PostgresConnectionString", $"Host={_postgres.Hostname};Port={_postgres.GetMappedPublicPort(5432)};Username=universalis;Password=universalis;Database=universalis" },
            })
            .Build();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddDbAccessServices(configuration);
        services.AddSingleton<IWorldToDcRegion>(new WorldToDcRegionMock());
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
        await _cache1.StartAsync().ConfigureAwait(false);
        await _cache2.StartAsync().ConfigureAwait(false);
        await _redis.StartAsync().ConfigureAwait(false);
    }

    public async Task DisposeAsync()
    {
        await _redis.DisposeAsync().ConfigureAwait(false);
        await _cache1.DisposeAsync().ConfigureAwait(false);
        await _cache2.DisposeAsync().ConfigureAwait(false);
        await _scylla.DisposeAsync().ConfigureAwait(false);
        await _postgres.DisposeAsync().ConfigureAwait(false);
    }

    public async Task ClearCache()
    {
        foreach (var server in _services.Value.GetRequiredService<IPersistentRedisMultiplexer>().GetDatabase().Multiplexer.GetServers())
        {
            await server.FlushAllDatabasesAsync();
        }
    }

    private class WorldToDcRegionMock : IWorldToDcRegion
    {
        public (string Dc, string Region) Get(int worldId)
        {
            return worldId switch
            {
                92 or 93 => ("Gaia", "Japan"),
                39 => ("Chaos", "Europe"),
                40 => ("Chaos", "Europe"),
                36 => ("Light", "Europe"),
                _ => ("Unknown", "Unknown")
            };
        }
    }
}
