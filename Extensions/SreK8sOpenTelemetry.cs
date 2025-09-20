using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Builder;
using OpenTelemetry.Trace;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;

namespace Sre.K8s.OpenTelemetry;

public static class SreK8sOpenTelemetry
{
    public static IServiceCollection AddSreK8sOpenTelemetry(this IServiceCollection services)
    {
        services.AddOpenTelemetry()
          .WithTracing(tracing =>
          {
              tracing
                  .AddAspNetCoreInstrumentation()
                  .AddHttpClientInstrumentation()
                  .AddOtlpExporter();
          })
          .WithMetrics(metrics =>
          {
              metrics
                  .AddAspNetCoreInstrumentation()
                  .AddHttpClientInstrumentation()
                  .AddRuntimeInstrumentation()
                  .AddOtlpExporter();
          });
        return services;
    }

    public static ILoggingBuilder AddSreK8sOpenTelemetryLog(this ILoggingBuilder builder)
    {
        builder.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
            logging.ParseStateValues = true;
            logging.AddOtlpExporter();
        });
        return builder;
    }
}