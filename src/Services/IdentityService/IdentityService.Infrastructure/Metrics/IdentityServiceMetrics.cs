using System.Diagnostics.Metrics;

namespace IdentityService.Infrastructure.Metrics;

public static class IdentityServiceMetrics
{
    public const string MeterName = "IdentityService";
    private static readonly Meter Meter = new(MeterName, "1.0.0");

    public static readonly Counter<long> AuthStartCounter = Meter.CreateCounter<long>(
        "identity_auth_start_total",
        description: "Number of authentication flows started");

    public static readonly Counter<long> AuthCallbackSuccessCounter = Meter.CreateCounter<long>(
        "identity_auth_callback_success_total",
        description: "Number of successful authentication callbacks");

    public static readonly Counter<long> AuthCallbackFailureCounter = Meter.CreateCounter<long>(
        "identity_auth_callback_failure_total",
        description: "Number of failed authentication callbacks");

    public static readonly Histogram<double> AuthSessionLatency = Meter.CreateHistogram<double>(
        "identity_auth_session_duration_seconds",
        unit: "s",
        description: "Duration from session creation to successful verification");
}
