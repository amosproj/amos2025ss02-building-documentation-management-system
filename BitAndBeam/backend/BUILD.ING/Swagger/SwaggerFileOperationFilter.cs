using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace BUILD.ING.Swagger
{
    /// <summary>
    /// Configures Swagger/OpenAPI operation filter to properly handle file upload operations
    /// </summary>
    public class SwaggerFileOperationFilter : IOperationFilter
    {
        /// <summary>
        /// Apply this filter to operations with IFormFile parameters
        /// </summary>
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var fileParameters = context.MethodInfo.GetParameters()
                .Where(p => p.ParameterType == typeof(IFormFile) || p.ParameterType == typeof(IFormFileCollection));

            if (fileParameters.Any())
            {
                // Remove any existing parameters from the Swagger document for file parameters
                foreach (var fileParameter in fileParameters)
                {
                    var parameterToRemove = operation.Parameters.FirstOrDefault(p => p.Name == fileParameter.Name);
                    if (parameterToRemove != null)
                    {
                        operation.Parameters.Remove(parameterToRemove);
                    }
                }

                // Set the correct content type for file uploads
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
                                    p => new OpenApiSchema
                                    {
                                        Type = "string",
                                        Format = "binary",
                                        Description = GetParameterDescription(context.ApiDescription, p.Name)
                                    }
                                ),
                                Required = new HashSet<string>(fileParameters.Select(p => p.Name))
                            }
                        }
                    },
                    Required = true,
                    Description = "File upload"
                };
            }
        }

        private string GetParameterDescription(ApiDescription apiDescription, string parameterName)
        {
            var parameter = apiDescription.ParameterDescriptions.FirstOrDefault(p => p.Name == parameterName);
            return parameter?.ModelMetadata?.Description ?? $"Upload {parameterName}";
        }
    }
}

