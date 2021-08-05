using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using System;
using System.IO;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Universalis.Alerts;
using Universalis.Application.Swagger;
using Universalis.Application.Uploads.Behaviors;
using Universalis.DbAccess;
using Universalis.GameData;

namespace Universalis.Application
{
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
            services.AddDbAccessServices();
            services.AddGameData(Configuration);
            services.AddUserAlerts();

            services.AddAllOfType<IUploadBehavior>(new[] { typeof(Startup).Assembly }, ServiceLifetime.Scoped);

            services
                .AddAuthentication(NegotiateDefaults.AuthenticationScheme)
                .AddNegotiate();

            services.AddControllers(options =>
            {
                options.Conventions.Add(new GroupByNamespaceConvention());
            }).AddNewtonsoftJson();

            services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
            });

            services.AddSwaggerGen(options =>
            {
                var license = new OpenApiLicense { Name = "MIT", Url = new Uri("https://github.com/Universalis-FFXIV/Universalis/blob/master/LICENSE") };

                var apiDescription = typeof(Startup).Assembly.GetManifestResourceStream("Universalis.Application.doc_description.html");
                if (apiDescription == null)
                {
                    throw new FileNotFoundException(nameof(apiDescription));
                }

                using var sr = new StreamReader(apiDescription);
                var description = sr.ReadToEnd();

                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Universalis",
                    Version = "v1",
                    License = license,
                    Description = description,
                });

                options.SwaggerDoc("v2", new OpenApiInfo
                {
                    Title = "Universalis",
                    Version = "v2",
                    License = license,
                    Description = description,
                });

                var docsFilePath = Path.Combine(AppContext.BaseDirectory, "Universalis.Application.xml");
                options.IncludeXmlComments(docsFilePath);
            });

            services.AddSwaggerGenNewtonsoftSupport();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("./v1/swagger.json", "Universalis v1");
                options.SwaggerEndpoint("./v2/swagger.json", "Universalis v2");
            });

            app.UseRouting();

            app.UseAuthentication();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
