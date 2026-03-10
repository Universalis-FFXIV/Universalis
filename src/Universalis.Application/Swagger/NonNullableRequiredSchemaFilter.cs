using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Universalis.Application.Swagger;

/// <summary>
/// Adds non-nullable properties to a schema's required list.
/// Swashbuckle does not do this automatically for value types or non-nullable reference types,
/// so generated clients would otherwise treat all properties as optional.
/// </summary>
public class NonNullableRequiredSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema.Properties == null)
            return;

        foreach (var (name, property) in schema.Properties)
        {
            if (!property.Nullable && !schema.Required.Contains(name))
            {
                schema.Required.Add(name);
            }
        }
    }
}
