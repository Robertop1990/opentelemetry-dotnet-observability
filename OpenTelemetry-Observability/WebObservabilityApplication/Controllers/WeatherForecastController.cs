using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace WebObservabilityApplication.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries =
        [
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        ];

        private readonly ILogger<WeatherForecastController> _logger;
        private readonly ActivitySource _activitySource;


        public WeatherForecastController(
            ILogger<WeatherForecastController> logger)
        {
            _logger = logger;
            _activitySource = new ActivitySource("WeatherForecastController");
        }

        [HttpGet("GetWeatherForecast", Name = "GetWeatherForecast")]
        public IEnumerable<WeatherForecast> Get()
        {
            using var activity = _activitySource.StartActivity("GetWeatherForecast");
            _logger.LogInformation("Request GetWeatherForecast trace_id: {trace_id}", activity?.TraceId);

            var forecasts = Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            }).ToArray();

            _logger.LogInformation("Returning {Count} weather forecasts trace_id: {trace_id}", forecasts.Length, activity?.TraceId);

            return forecasts;
        }

        [HttpGet("GetWeatherForecastV2", Name = "GetWeatherForecastV2")]
        public IEnumerable<WeatherForecast> GetV2()
        {
            using var activity = _activitySource.StartActivity("GetWeatherForecastV2");
            _logger.LogInformation("Request GetWeatherForecastV2 trace_id: {trace_id}", activity?.TraceId);

            var forecasts = Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            }).ToArray();

            _logger.LogInformation("Returning {Count} weather forecasts trace_id: {trace_id}", forecasts.Length, activity?.TraceId);

            return forecasts;
        }
    }
}
