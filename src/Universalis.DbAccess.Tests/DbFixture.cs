using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Universalis.DbAccess.Tests;

public class DbFixture : IAsyncLifetime
{
    private readonly TestcontainersContainer _scylla;
    private readonly TestcontainersContainer _cache;
    private readonly TestcontainersContainer _redis;

    private readonly Lazy<IServiceProvider> _services;

    public IServiceProvider Services => _services.Value;

    public DbFixture()
    {
        _scylla = new TestcontainersBuilder<TestcontainersContainer>()
            .WithName(Guid.NewGuid().ToString("D"))
            .WithImage("scylladb/scylla:5.0.5")
            .WithExposedPort(8000)
            .WithPortBinding(8000, true)
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

        _services = new Lazy<IServiceProvider>(() => CreateServiceProvider());
    }

    private IServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile(
                 path: "appsettings.json",
                 optional: false,
                 reloadOnChange: true)
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                { "AWS:ServiceURL", $"http://{_scylla.Hostname}:{_scylla.GetMappedPublicPort(8000)}" },
                { "RedisCacheConnectionString", $"{_cache.Hostname}:{_cache.GetMappedPublicPort(6379)}" },
                { "RedisConnectionString", $"{_redis.Hostname}:{_redis.GetMappedPublicPort(6379)}" },
            })
            .Build();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddDbAccessServices(configuration);
        Task.Delay(5000).GetAwaiter().GetResult();
        return services.BuildServiceProvider();
    }

    public async Task InitializeAsync()
    {
        await _scylla.StartAsync().ConfigureAwait(false);
        await _cache.StartAsync().ConfigureAwait(false);
        await _redis.StartAsync().ConfigureAwait(false);
    }

    public async Task DisposeAsync()
    {
        await _redis.DisposeAsync().ConfigureAwait(false);
        await _cache.DisposeAsync().ConfigureAwait(false);
        await _scylla.DisposeAsync().ConfigureAwait(false);
    }
}
