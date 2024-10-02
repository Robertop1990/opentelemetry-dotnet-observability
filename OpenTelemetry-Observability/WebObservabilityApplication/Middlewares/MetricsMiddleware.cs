using System.Diagnostics;
using WebObservabilityApplication.Services;

namespace WebObservabilityApplication.Middlewares
{
    public class MetricsMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly MetricsService _metricsService;

        public MetricsMiddleware(RequestDelegate next, MetricsService metricsService)
        {
            _next = next;
            _metricsService = metricsService;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var method = context.Request.Method;
            var path = context.Request.Path.ToString();
            var traceId = Activity.Current?.TraceId.ToString() ?? "unknown";
            var requestId = Guid.NewGuid().ToString();

            // Incrementar las solicitudes en progreso
            _metricsService.IncrementRequestsInProgress(requestId, method, path);
            _metricsService.IncrementRequestCounter(method, path);

            // Capturar el tiempo de procesamiento
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Continuar con la siguiente etapa del pipeline
                await _next(context);
            }
            catch (Exception ex)
            {
                // Incrementar el contador de excepciones
                _metricsService.IncrementExceptionCounter(method, path, ex.GetType().Name);
                throw;
            }
            finally
            {
                stopwatch.Stop();

                // Registrar el tiempo de procesamiento de la solicitud
                _metricsService.RecordRequestProcessingTime(method, path, stopwatch.Elapsed.TotalSeconds);

                // Incrementar el contador de respuestas
                _metricsService.IncrementResponsesCounter(method, path, context.Response.StatusCode);

                // Decrementar las solicitudes en progreso
                _metricsService.DecrementRequestsInProgress(requestId);
            }
        }
    }
}
