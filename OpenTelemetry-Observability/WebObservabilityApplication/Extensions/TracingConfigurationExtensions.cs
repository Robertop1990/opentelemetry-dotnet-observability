using OpenTelemetry.Trace;
using OpenTelemetry.Resources;
using OpenTelemetry.Exporter;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class TracingConfigurationExtensions
    {
        public static IServiceCollection ConfigureOpenTelemetryTracing(
            this IServiceCollection services,
            IConfiguration configuration,
            string appName)
        {
            services.AddOpenTelemetry()
                .WithTracing(builder =>
                {
                    builder
                        .SetResourceBuilder(ResourceBuilder.CreateDefault()
                            .AddService(appName)
                            .AddAttributes(new Dictionary<string, object>
                            {
                              { "app_name", appName }
                            }))
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddSource(appName)
                        .AddHttpClientInstrumentation()
                        .AddOtlpExporter(options =>
                        {
                            options.Endpoint = new Uri(configuration["OpenTelemetry:CollectorUri"] + "/v1/traces");
                            options.Protocol = OtlpExportProtocol.Grpc;
                        });
                });

            return services;
        }
    }
}
