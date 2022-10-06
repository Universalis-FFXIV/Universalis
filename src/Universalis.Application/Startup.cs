using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Prometheus;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.XPath;
using Universalis.Alerts;
using Universalis.Application.Caching;
using Universalis.Application.ExceptionFilters;
using Universalis.Application.Realtime;
using Universalis.Application.Swagger;
using Universalis.Application.Uploads.Behaviors;
using Universalis.DbAccess;
using Universalis.DbAccess.MarketBoard;
using Universalis.GameData;
using Universalis.Mogboard;

namespace Universalis.Application;

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    private IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddDbAccessServices(Configuration);
        services.AddGameData(Configuration);
        services.AddUserAlerts();

        services.AddAllOfType<IUploadBehavior>(new[] { typeof(Startup).Assembly }, ServiceLifetime.Singleton);

        var cacheSize = int.Parse(Configuration["MarketCurrentDataCacheSize"]);
        services.AddSingleton<ICache<CachedCurrentlyShownQuery, CachedCurrentlyShownData>>(sc =>
        {
            var currentlyShownDb = sc.GetRequiredService<ICurrentlyShownDbAccess>();
            var historyDb = sc.GetRequiredService<IHistoryDbAccess>();
            return new CurrentlyShownCache(cacheSize, currentlyShownDb, historyDb);
        });

        services.AddSingleton<ISocketProcessor, SocketProcessor>();

        services
            .AddAuthentication(NegotiateDefaults.AuthenticationScheme)
            .AddNegotiate();

        services.AddControllers(options =>
        {
            options.Filters.Add<DecoderFallbackExceptionFilter>();
            options.Filters.Add<OperationCancelledExceptionFilter>();
            options.Filters.Add<PostgresExceptionFilter>();
        });

        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
        });

        services.AddSwaggerGen(options =>
        {
            var license = new OpenApiLicense { Name = "MIT", Url = new Uri("https://github.com/Universalis-FFXIV/Universalis/blob/master/LICENSE") };

            options.SwaggerDoc("v1", new UniversalisApiInfo()
                .WithLicense(license)
                .WithVersion(new Version(1, 0)));

            options.SwaggerDoc("v2", new UniversalisApiInfo()
                .WithLicense(license)
                .WithVersion(new Version(2, 0)));
            
            options.SwaggerDoc("v3", new UniversalisApiInfo()
                .WithLicense(license)
                .WithVersion(new Version(3, 0)));

            options.OperationFilter<RemoveVersionParameterFilter>();
            options.DocumentFilter<ReplaceVersionWithExactFilter>();

            options.TagActionsBy(api =>
            {
                if (!api.TryGetMethodInfo(out var mi))
                    return new[] { api.HttpMethod };

                var attr = (ApiTagAttribute)mi.GetCustomAttribute(typeof(ApiTagAttribute));
                return attr == null ? new[] { api.HttpMethod } : new[] { attr.Tag };
            });

            options.DocInclusionPredicate((version, desc) =>
            {
                var versions = desc.CustomAttributes()
                    .OfType<ApiVersionAttribute>()
                    .SelectMany(attr => attr.Versions)
                    .ToList();

                if (version == "v1" && !versions.Any())
                {
                    return true;
                }

                var maps = desc.CustomAttributes()
                    .OfType<MapToApiVersionAttribute>()
                    .SelectMany(attr => attr.Versions)
                    .ToArray();

                return versions.Any(v => $"v{v}" == version)
                       && (!maps.Any() || maps.Any(v => $"v{v}" == version));
            });

            var apiDocs = typeof(Startup).Assembly.GetManifestResourceStream(
                new EmbeddedResourceName("Universalis.Application.xml"));
            if (apiDocs == null)
            {
                throw new FileNotFoundException(nameof(apiDocs));
            }

            options.IncludeXmlComments(() => new XPathDocument(apiDocs));
        });

        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        services.AddMogboard(Configuration);
        services.AddRazorPages();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler("/error");
        }

        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            // Relative paths
            options.SwaggerEndpoint("v1/swagger.json", "Universalis v1");
            options.SwaggerEndpoint("v2/swagger.json", "Universalis v2");
            options.SwaggerEndpoint("v3/swagger.json", "Universalis v3");

            options.DocumentTitle = "Universalis Documentation";
        });

        app.UseWebSockets(new WebSocketOptions
        {
            KeepAliveInterval = TimeSpan.FromMinutes(2),
        });

        app.UseStaticFiles();

        app.UseRouting();
        app.UseHttpMetrics();
        app.UseAuthentication();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapRazorPages();
            endpoints.MapControllers();
            endpoints.MapMetrics();
        });
    }
}