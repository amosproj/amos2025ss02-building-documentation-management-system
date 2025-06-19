using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace BUILD.ING.Swagger
{
    /// <summary>
    /// Adds an example for the DocumentMetadataPatchRequest schema so that Swagger UI shows
    /// a request payload where <c>categoryId</c> is null. This visually demonstrates that
    /// the API accepts a nullable category when the user manually enters the category name
    /// in the front-end.
    /// </summary>
    public class DocumentMetadataPatchRequestExampleSchemaFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            if (context.Type == typeof(BUILD.ING.Controllers.DocumentsController.DocumentMetadataPatchRequest))
            {
                schema.Example = new OpenApiObject
                {
                    ["categoryId"] = new OpenApiNull(),
                    ["buildingId"] = new OpenApiInteger(42)
                };
            }
        }
    }
}
