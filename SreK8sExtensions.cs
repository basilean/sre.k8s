using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Sre.K8s.Probes;
using Sre.K8s.Recovery;
using Sre.K8s.OpenTelemetry;
using Sre.K8s.Swagger;

namespace Sre.K8s;

public static class SreK8sExtensions
{
    public static WebApplicationBuilder AddSreK8s(this WebApplicationBuilder builder, Action<SreK8sOptions>? configure = null)
    {
        var options = new SreK8sOptions();
        configure?.Invoke(options);
        builder.Services.Configure(configure ?? (_ => { }));

        if (options.UseProbes)
            builder.Services.AddSreK8sProbes();

        if (options.UseRecovery)
            builder.Services.AddSreK8sRecovery();

        if (options.UseOpenTelemetry)
        {
            builder.Services.AddSreK8sOpenTelemetry();
            builder.Logging.AddSreK8sOpenTelemetryLog();
        }

        if (options.UseSwagger)
            builder.Services.AddSreK8sSwagger();

        return builder;
    }

    public static WebApplication UseSreK8s(this WebApplication app)
    {
        var options = app.Services.GetRequiredService<IOptions<SreK8sOptions>>().Value;

        if (options.UseProbes)
            app.UseSreK8sProbes();

        if (options.UseRecovery)
            app.UseSreK8sRecovery();

        if (options.UseSwagger)
            app.UseSreK8sSwagger();

        return app;
    }
}
