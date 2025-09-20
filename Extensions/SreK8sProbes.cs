using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Builder;

namespace Sre.K8s.Probes;

public static class SreK8sProbes
{
    public static IServiceCollection AddSreK8sProbes(this IServiceCollection services)
    {
        services.AddSingleton<ProbeLivez>();
        services.AddSingleton<ProbeReadyz>();
        services.AddHealthChecks()
            .AddCheck<ProbeLivez>("livez")
            .AddCheck<ProbeReadyz>("readyz");
        return services;
    }

    public static WebApplication UseSreK8sProbes(this WebApplication app)
    {
        app.MapHealthChecks("/livez", new HealthCheckOptions { Predicate = c => c.Name == "livez" });
        app.MapHealthChecks("/readyz", new HealthCheckOptions { Predicate = c => c.Name == "readyz" });
        return app;
    }
}

public abstract class SreK8sProbeBase : IHealthCheck
{
    protected bool _ok = true;
    protected string _message = "";

    public void Error(string message) {
      _ok = false;
      _message = message;
    }

    public void Ok(string message = "") {
      _ok = true;
      _message = message;
    }

    public bool Status() => _ok;
    public string Message() => _message;

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (_ok)
            return Task.FromResult(HealthCheckResult.Healthy(_message));
        return Task.FromResult(HealthCheckResult.Unhealthy(_message));
    }
}

public class ProbeLivez : SreK8sProbeBase{}
public class ProbeReadyz : SreK8sProbeBase{}