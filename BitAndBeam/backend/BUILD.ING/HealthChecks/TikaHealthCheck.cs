using System;
using System.Threading;
using System.Threading.Tasks;
using BUILD.ING.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace BUILD.ING.HealthChecks
{
    public class TikaHealthCheck : IHealthCheck
    {
        private readonly TikaService _tikaService;
        private readonly ILogger<TikaHealthCheck> _logger;

        public TikaHealthCheck(TikaService tikaService, ILogger<TikaHealthCheck> logger)
        {
            _tikaService = tikaService;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Running Tika health check");
                return await _tikaService.CheckHealthAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Tika health check");
                return HealthCheckResult.Unhealthy("Tika health check failed", ex);
            }
        }
    }
}
