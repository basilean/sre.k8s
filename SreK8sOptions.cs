namespace Sre.K8s;

public class SreK8sOptions
{
    public bool UseProbes { get; set; } = true;
    public bool UseRecovery { get; set; } = true;
    public bool UseOpenTelemetry { get; set; } = true;
    public bool UseSwagger { get; set; } = true;
}