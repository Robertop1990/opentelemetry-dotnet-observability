using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class MetricsConfigurationExtensions
    {
        public static IServiceCollection ConfigureOpenTelemetryMetrics(
            this IServiceCollection services,
            IConfiguration configuration,
            string appName)
        {
            services.AddOpenTelemetry()
                .WithMetrics(builder =>
                {
                    builder
                        .AddMeter("MetricsService")
                        .SetExemplarFilter(ExemplarFilterType.TraceBased)
                        .SetResourceBuilder(ResourceBuilder.CreateDefault()
                            .AddService(appName)
                            .AddAttributes(new Dictionary<string, object>
                            {
                              { "app_name", appName }
                            }))
                        .AddProcessInstrumentation()
                        .AddRuntimeInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddAspNetCoreInstrumentation()
                        .AddOtlpExporter(options =>
                        {
                            options.Endpoint = new Uri(configuration["OpenTelemetry:CollectorUri"] + "/v1/metrics");
                            options.Protocol = OtlpExportProtocol.Grpc;
                        });
                });

            return services;
        }
    }
}
