using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Linq;

namespace BUILD.ING.Services
{
    /// <summary>
    /// Operation filter to properly handle file uploads in Swagger UI
    /// </summary>
    public class FileUploadOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var fileParameters = context.MethodInfo.GetParameters()
                .Where(p => p.ParameterType == typeof(IFormFile) || p.ParameterType == typeof(List<IFormFile>))
                .ToList();

            if (fileParameters.Count > 0)
            {
                // If there are file parameters, we need to make the operation use multipart/form-data
                operation.RequestBody = new OpenApiRequestBody
                {
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["multipart/form-data"] = new OpenApiMediaType
                        {
                            Schema = new OpenApiSchema
                            {
                                Type = "object",
                                Properties = fileParameters.ToDictionary(
                                    p => p.Name,
                                    _ => new OpenApiSchema
                                    {
                                        Type = "string",
                                        Format = "binary"
                                    }
                                ),
                                Required = new HashSet<string>(fileParameters.Select(p => p.Name))
                            }
                        }
                    }
                };
            }
        }
    }
}
