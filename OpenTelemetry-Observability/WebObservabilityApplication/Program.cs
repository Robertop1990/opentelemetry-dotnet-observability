using WebObservabilityApplication.Middlewares;
using WebObservabilityApplication.Services;

var builder = WebApplication.CreateBuilder(args);
const string APPNAME = "web-observability-app";

builder.Services.AddSingleton(new MetricsService(APPNAME));
builder.Host.ConfigureOpenTelemetryLogging(builder.Configuration, APPNAME);
builder.Services.ConfigureOpenTelemetryTracing(builder.Configuration, APPNAME);
builder.Services.ConfigureOpenTelemetryMetrics(builder.Configuration, APPNAME);


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseMiddleware<MetricsMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
