using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace BUILD.ING.Swagger
{
    public class BuildingRequestExampleSchemaFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            if (context.Type == typeof(BUILD.ING.Models.Building))
            {
                schema.Example = new OpenApiObject
                {
                    ["buildingId"] = new OpenApiInteger(1),
                    ["name"] = new OpenApiString("Neuer Gebäudename"),
                    ["address"] = new OpenApiString("Beispielstraße 99"),
                    ["constructionYear"] = new OpenApiInteger(2022),
                    ["totalArea"] = new OpenApiDouble(200.5),
                    ["floors"] = new OpenApiInteger(4),
                    ["description"] = new OpenApiString("Modernisiertes Gebäude"),
                    ["coordinates"] = new OpenApiObject
                    {
                        ["x"] = new OpenApiDouble(11.5761),
                        ["y"] = new OpenApiDouble(48.1374)
                    },
                    ["createdAt"] = new OpenApiString("2024-05-01T00:00:00Z"),
                    ["updatedAt"] = new OpenApiString("2025-05-13T00:00:00Z"),
                    ["documents"] = new OpenApiArray(),
                    ["buildingDocumentRelations"] = new OpenApiArray()
                };
            }
        }
    }
}
