using Judhur.Infrastructure.Data;

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Judhur.Api.HealthChecks;

public sealed class DatabaseHealthCheck(AppDbContext context) : IHealthCheck
{
    private readonly AppDbContext _context = context;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        => await _context.Database.CanConnectAsync(cancellationToken)
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("The database is unreachable.");
}
