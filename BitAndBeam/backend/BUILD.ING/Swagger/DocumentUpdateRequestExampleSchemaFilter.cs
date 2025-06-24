using BUILD.ING.Controllers; // For DocumentUpdateRequest
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace BUILD.ING.Swagger
{
    /// <summary>
    /// Adds an example for the DocumentUpdateRequest schema so that Swagger UI shows
    /// a request payload with typical fields. This helps demonstrate the expected structure.
    /// </summary>
    public class DocumentUpdateRequestExampleSchemaFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            ArgumentNullException.ThrowIfNull(schema);
            ArgumentNullException.ThrowIfNull(context);
            if (context.Type == typeof(DocumentUpdateRequest))
            {
                schema.Example = new OpenApiObject
                {
                    ["title"] = new OpenApiNull(),
                    ["description"] = new OpenApiNull()
                };
            }
        }
    }
}
