using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Universalis.Application;

public static class ServiceCollectionExtensions
{
    public static void AddAllOfType<T>(
        this IServiceCollection services,
        IEnumerable<Assembly> assemblies,
        ServiceLifetime lifetime = ServiceLifetime.Transient)
    {
        // https://medium.com/agilix/asp-net-core-inject-all-instances-of-a-service-interface-64b37b43fdc8
        var typesFromAssemblies = assemblies
            .SelectMany(a => a.DefinedTypes.Where(x => x.GetInterfaces().Contains(typeof(T))));

        foreach (var type in typesFromAssemblies)
        {
            services.Add(new ServiceDescriptor(typeof(T), type, lifetime));
        }
    }
}