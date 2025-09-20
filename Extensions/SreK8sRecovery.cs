using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Sre.K8s.Probes;

namespace Sre.K8s.Recovery;

public static class SreK8sRecovery
{
    public static IServiceCollection AddSreK8sRecovery(this IServiceCollection services)
    {
        services.AddHostedService<SreK8sRecoveryJob>();
        return services;
    }

    public static WebApplication UseSreK8sRecovery(this WebApplication app)
    {
        app.UseMiddleware<SreK8sRecoveryException>();
        return app;
    }
}

public class SreK8sRecoveryJob : BackgroundService
{
    private readonly ILogger<SreK8sRecoveryJob> _logger;
    private readonly ProbeReadyz _readyz;
    private readonly ProbeLivez _livez;
    private readonly IServiceProvider _services;

    public SreK8sRecoveryJob(
        ILogger<SreK8sRecoveryJob> logger,
        ProbeReadyz readyz,
        ProbeLivez livez,
        IServiceProvider services)
    {
        _logger = logger;
        _readyz = readyz;
        _livez = livez;
        _services = services;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // esta no ready?
            if (!_readyz.Status())
            {
                _logger.LogInformation("Ejecutando RecoveryJob...");

                var allOk = await CheckAll(stoppingToken);

                if (allOk)
                {
                    _logger.LogInformation("Sistema restaurado, marcando como READY");
                    _readyz.Ok(); // todo ok
                }
                else
                {
                    _logger.LogWarning("Verificacion fallida, volveremos a intentar...");
                }
            }

            // recurrente cada 10 segundos
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }

    private async Task<bool> CheckAll(CancellationToken token)
    {
        try
        {
            // Simulamos una verificación externa (ej: conexión a DB)
            await Task.Delay(1000, token);

            // Acá podés usar un service scope para acceder a un DbContext, etc.
            using var scope = _services.CreateScope();
            // var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();
            // await db.PingAsync();

            // Lógica de verificación real:
            var checkOk = DateTime.UtcNow.Second % 2 == 0; // simula falla/success

            return checkOk;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error durante verificacion");
            return false;
        }
    }
}

public class SreK8sRecoveryException
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SreK8sRecoveryException> _logger;
    private readonly ProbeReadyz _readyz;

    public SreK8sRecoveryException(RequestDelegate next, ILogger<SreK8sRecoveryException> logger, ProbeReadyz readyz)
    {
        _next = next;
        _logger = logger;
        _readyz = readyz;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");

            // Si es una excepción crítica, marcamos "no ready"
            if (ex.Message.Contains("DB_FAIL")) // Simula error de base de datos
            {
                _readyz.Error("DB_FAIL");
            }

            context.Response.StatusCode = 500;
            await context.Response.WriteAsync(ex.Message);
        }
    }
}