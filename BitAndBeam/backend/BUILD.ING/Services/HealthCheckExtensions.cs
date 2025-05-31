using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BUILD.ING.Services
{
    /// <summary>
    /// Extension methods for health checks
    /// </summary>
    public static class HealthCheckExtensions
    {
        /// <summary>
        /// Writes a detailed JSON response for health checks
        /// </summary>
        public static Task WriteDetailedJsonResponse(HttpContext context, HealthReport report)
        {
            context.Response.ContentType = "application/json";

            var response = new
            {
                status = report.Status.ToString(),
                duration = report.TotalDuration.TotalMilliseconds,
                timestamp = DateTimeOffset.UtcNow,
                checks = report.Entries.Select(e => new
                {
                    name = e.Key,
                    status = e.Value.Status.ToString(),
                    duration = e.Value.Duration.TotalMilliseconds,
                    description = e.Value.Description,
                    exception = e.Value.Exception?.Message,
                    data = e.Value.Data.Any() ? e.Value.Data : null
                })
            };

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            return context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
        }

        /// <summary>
        /// Initiates health checks and waits for them to complete, useful during startup
        /// </summary>
        public static async Task CheckHealthOnStartupAsync(this HealthCheckService healthCheckService, IServiceProvider serviceProvider)
        {
            // Create a logger to record startup health check results
            var loggerFactory = serviceProvider.GetService(typeof(Microsoft.Extensions.Logging.ILoggerFactory))                
                as Microsoft.Extensions.Logging.ILoggerFactory;
            var logger = loggerFactory?.CreateLogger("StartupHealthCheck") ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;

            try
            {
                logger.LogInformation("Running startup health checks...");

                // Only check ready tagged services
                var report = await healthCheckService.CheckHealthAsync(check => check.Tags.Contains("ready"));

                if (report.Status == HealthStatus.Healthy)
                {
                    logger.LogInformation("All startup health checks passed");
                }
                else
                {
                    logger.LogWarning("Some startup health checks failed or are degraded");

                    // Log details for each failed check
                    foreach (var entry in report.Entries.Where(e => e.Value.Status != HealthStatus.Healthy))
                    {
                        logger.LogWarning($"Health check '{entry.Key}' status: {entry.Value.Status}, Description: {entry.Value.Description}");
                        if (entry.Value.Exception != null)
                        {
                            logger.LogWarning($"Exception: {entry.Value.Exception.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Exception during startup health checks");
            }
        }
    }
}
