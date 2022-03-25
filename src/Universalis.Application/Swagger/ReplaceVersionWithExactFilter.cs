using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Universalis.Application.Swagger;

public class ReplaceVersionWithExactFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        var paths = new OpenApiPaths();
        foreach (var (k, v) in swaggerDoc.Paths)
        {
            paths[k.Replace("v{version}", swaggerDoc.Info.Version)] = v;
        }

        swaggerDoc.Paths = paths;
    }
}