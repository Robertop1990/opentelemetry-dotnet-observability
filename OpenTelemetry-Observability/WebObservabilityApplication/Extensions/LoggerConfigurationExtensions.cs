using Serilog;
using Serilog.Sinks.OpenTelemetry;
using Serilog.Sinks.SystemConsole.Themes;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class LoggerConfigurationExtensions
    {
        public static IHostBuilder ConfigureOpenTelemetryLogging(
            this IHostBuilder hostBuilder,
            IConfiguration configuration,
            string appName)
        {
            return hostBuilder.UseSerilog((context, services, loggerConfiguration) =>
            {
                loggerConfiguration
                  .ReadFrom.Configuration(context.Configuration)
                  .ReadFrom.Services(services)
                  .WriteTo.Async(x => x.Console(theme: AnsiConsoleTheme.Code))
                  .ConfigureEnrichment(appName)
                  .ConfigureOpenTelemetry(configuration, appName);
            });
        }

        public static LoggerConfiguration ConfigureEnrichment(
            this LoggerConfiguration loggerConfiguration,
            string appName)
        {
            return loggerConfiguration
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithThreadId()
                .Enrich.WithProperty("ApplicationName", appName);
        }

        public static LoggerConfiguration ConfigureOpenTelemetry(
            this LoggerConfiguration loggerConfiguration,
            IConfiguration configuration,
            string appName)
        {
            try
            {
                loggerConfiguration.WriteTo.OpenTelemetry(options =>
                {
                    options.Endpoint = configuration.GetValue<string>("OpenTelemetry:CollectorUri");
                    options.Protocol = OtlpProtocol.Grpc;
                    options.IncludedData = IncludedData.TraceIdField | IncludedData.SpanIdField;
                    options.ResourceAttributes = new Dictionary<string, object>
                    {
                        { "app_name", appName }
                    };
                });
            }
            catch (Exception ex) 
            {
                Log.Error(ex, "ConfigureOpenTelemetryLogging Error");
            }

            return loggerConfiguration;
        }
    }
}
