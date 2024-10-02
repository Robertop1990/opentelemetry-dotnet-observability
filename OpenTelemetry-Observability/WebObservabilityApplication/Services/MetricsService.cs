using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace WebObservabilityApplication.Services
{
    public class MetricsService
    {
        private readonly Meter _meter;
        private readonly string _appName;

        private readonly Counter<long> _requestCounter;
        private readonly Counter<long> _responsesCounter;
        private readonly Histogram<double> _requestProcessingTime;
        private readonly Counter<long> _exceptionsCounter;

        private ObservableGauge<long> _requestsInProgressGauge;
        private ObservableGauge<int> _appInfoGauge;

        private long _requestsInProgress = 0;
        private readonly ConcurrentDictionary<string, (string Method, string Path)> _requestsInProgressMap = new();

        public MetricsService(string appName)
        {
            _meter = new Meter("MetricsService", "1.0");
            _appName = appName;

            _requestCounter = _meter.CreateCounter<long>(
                "microservices_dotnet_request_total",
                description: "Total count of request by method, path, and app name.");

            _responsesCounter = _meter.CreateCounter<long>(
                "microservices_dotnet_responses_total",
                description: "Total count of responses by method, path, and status code.");

            _requestProcessingTime = _meter.CreateHistogram<double>(
                "microservices_dotnet_requests_duration_seconds",
                unit: "seconds",
                description: "Histogram of request processing time (in seconds).");

            _exceptionsCounter = _meter.CreateCounter<long>(
                "microservices_dotnet_exceptions_total",
                description: "Total count of exceptions by method, path, and exception type.");

            _requestsInProgressGauge = _meter.CreateObservableGauge(
                "microservices_dotnet_requests_in_progress",
                () => GetRequestsInProgress(),
                "requests",
                "Gauge of requests currently being processed by method and path.");

            _appInfoGauge = _meter.CreateObservableGauge(
                "microservices_dotnet_app_info",
                () => GetAppInfo(),
                description: "Application information for monitoring purposes."
            );
        }


        public void IncrementRequestCounter(string method, string path)
        {
            _requestCounter.Add(1,
                new KeyValuePair<string, object?>("method", method),
                new KeyValuePair<string, object?>("path", path),
                new KeyValuePair<string, object?>("app_name", _appName),
                GetTraceExemplar());
        }

        // Incrementa el contador de respuestas con exemplars
        public void IncrementResponsesCounter(string method, string path, int statusCode)
        {
            _responsesCounter.Add(1,
                new KeyValuePair<string, object?>("method", method),
                new KeyValuePair<string, object?>("path", path),
                new KeyValuePair<string, object?>("status_code", statusCode),
                new KeyValuePair<string, object?>("app_name", _appName),
                GetTraceExemplar());
        }

        // Registra el tiempo de procesamiento con exemplars
        public void RecordRequestProcessingTime(string method, string path, double duration)
        {
            _requestProcessingTime.Record(duration,
                new KeyValuePair<string, object?>("method", method),
                new KeyValuePair<string, object?>("path", path),
                new KeyValuePair<string, object?>("app_name", _appName),
                GetTraceExemplar());
        }

        // Incrementa el contador de excepciones con exemplars
        public void IncrementExceptionCounter(string method, string path, string exceptionType)
        {
            _exceptionsCounter.Add(1,
                new KeyValuePair<string, object?>("method", method),
                new KeyValuePair<string, object?>("path", path),
                new KeyValuePair<string, object?>("app_name", _appName),
                new KeyValuePair<string, object?>("exception_type", exceptionType),
                GetTraceExemplar());
        }


        // Método para incrementar las solicitudes en progreso
        public void IncrementRequestsInProgress(string requestId, string method, string path)
        {
            _requestsInProgressMap[requestId] = (method, path);
        }

        // Método para decrementar las solicitudes en progreso
        public void DecrementRequestsInProgress(string requestId)
        {
            _requestsInProgressMap.TryRemove(requestId, out _);
        }

        // Método para obtener el número de solicitudes en progreso
        private IEnumerable<Measurement<long>> GetRequestsInProgress()
        {
            foreach (var request in _requestsInProgressMap.Values)
            {
                yield return new Measurement<long>(1,
                    new KeyValuePair<string, object>("method", request.Method),
                    new KeyValuePair<string, object>("path", request.Path),
                    new KeyValuePair<string, object>("app_name", _appName),
                    GetTraceExemplar());
            }
        }

        // Método para obtener la información de la aplicación
        private IEnumerable<Measurement<int>> GetAppInfo()
        {
            return
            [
            new Measurement<int>(1, new KeyValuePair<string, object>("app_name", _appName))
        ];
        }

        private static KeyValuePair<string, object?> GetTraceExemplar()
        {
            var activity = Activity.Current;
            if (activity != null)
            {
                return new KeyValuePair<string, object?>("trace_id", activity.TraceId.ToString());
            }
            return new KeyValuePair<string, object?>("trace_id", string.Empty);
        }
    }
}
