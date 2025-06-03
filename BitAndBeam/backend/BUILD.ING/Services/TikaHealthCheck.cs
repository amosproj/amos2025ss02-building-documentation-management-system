using System;
using System.Threading;
using System.Threading.Tasks;
using BitAndBeam.Tika;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace BUILD.ING.Services
{
    /// <summary>
    /// Health check implementation for the Apache Tika service
    /// </summary>
    public class TikaHealthCheck : IHealthCheck
    {
        private readonly TikaService _tikaService;
        private readonly ILogger<TikaHealthCheck> _logger;

        public TikaHealthCheck(TikaService tikaService, ILogger<TikaHealthCheck> logger)
        {
            _tikaService = tikaService;
            _logger = logger;
        }

        /// <summary>
        /// Performs a health check by verifying connectivity to the Tika service
        /// </summary>
        /// <param name="context">Health check context</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Health check result</returns>
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Performing health check for Tika service");

            try
            {
                // Delegate to the TikaService's health check method
                var result = await _tikaService.CheckHealthAsync(cancellationToken).ConfigureAwait(false);

                // Add additional data to the health check result
                var data = new System.Collections.Generic.Dictionary<string, object>
                {
                    { "LastChecked", DateTimeOffset.UtcNow },
                    { "Service", "Apache Tika" }
                };

                if (result.Status == HealthStatus.Healthy)
                {
                    return HealthCheckResult.Healthy(result.Description, data);
                }
                else if (result.Status == HealthStatus.Degraded)
                {
                    return HealthCheckResult.Degraded(result.Description, null, data);
                }
                else
                {
                    return HealthCheckResult.Unhealthy(result.Description, null, data);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception during Tika health check");

                return HealthCheckResult.Unhealthy(
                    "Unhandled exception during health check",
                    ex,
                    new System.Collections.Generic.Dictionary<string, object>
                    {
                        { "LastChecked", DateTimeOffset.UtcNow },
                        { "Service", "Apache Tika" },
                        { "ExceptionType", ex.GetType().Name }
                    });
            }
        }
    }
}
