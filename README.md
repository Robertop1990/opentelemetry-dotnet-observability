# Observability Implementation with .NET8.0, OpenTelemetry, Loki, Jaeger, Prometheus and Grafana

<details>
<summary>Table of Contents</summary>

- [Observability Implementation with .NET8.0, OpenTelemetry, Loki, Jaeger, Prometheus and Grafana](#observability-implementation-with-net80-opentelemetry-loki-jaeger-prometheus-and-grafana)
  - [Introduction](#introduction)
  - [Components Diagram](#components-diagram)
    - [otel-collector](#otel-collector)
    - [WebObservabilityApplication](#webobservabilityapplication)
    - [loki](#loki)
    - [jaeger](#jaeger)
    - [prometheus](#prometheus)
    - [grafana](#grafana)
  - [Running the application](#running-the-application)
  - [Conclusion](#conclusion)
      - [Contact Addresses](#contact-addresses)
        - [Linkedin: Send a message](#linkedin-send-a-message)

</details>


## Introduction
As sytem arquitectures grow in complexity and scale, we face the challenge of tracking the internal state of applications. As a result, there is a need for increased observability in these increasingly diverse and dynamic computing enviroments.
This is where observability comes in, which is the ability to measure, monitor, and understand the internal state of a system based on the data it generates, such as logs, metrics, and traces.

## Components Diagram

![Components Diagram](images/Observability-Componenst-Diagram.png)

The components used in this implementation are detailed below.

  ### <u>otel-collector</u>
  ![Otel Collector](images/Otel-Collector.png)  
  It's a key component within opentelemetry observation. They are designed to receive, process, and export telemetry data(logs, traces, and metrics).

  **Function**: The otel-collector exposes port 4317 to configure the otlp receiver with the grpc protocol. The otlp receiver will receive logs, traces, and metrics from *WebObservabilityApplication*. The otel-collector exposes port 8889 so that prometheus can perform scraping and obtain metrics.

  ![Otel Collector Receiver](images/Otel-Collector-Receiver-4317.png)
  *Configuring the otlp receiver with the grpc protocol.*

  ### <u>WebObservabilityApplication</u>
  ![WebObservabilityApplication](images/WebObservabilityApplication.png)

  **Function**: The WebObservabilityApplication web application will send its logs, traces, and metrics to otel-collector through port 4317 using the grpc protocol.  
  For sending *logs*, the *LoggerConfigurationExtensions.cs* class is implemented, which takes advantage of the functionalities of the serilog and serilog.sink.opentelemetry packages to capture and send logs through opentelemetry.
  For sending *traces*, the *TracingConfigurationExtensions.cs* class is implemented, which takes advantage of the functionalities of the opentelemetry package to capture and send distributed traces.

  Regarding the sending of metrics, three key components are implemented:  
  ***MetricsService.cs***: Class responsible for capturing, managing, and exposing custom metrics.  
  ***MetricsMiddleware.cs***: Middleware that intercepts HTTP requests and uses the methods of *MetricsService.cs* to record custom metrics.  
  ***MetricsConfigurationExtensions.cs***: Class responsible for capturing and sending metrics through opentelemetry.

  ### <u>loki</u>
  ![Loki](images/Loki.png)  
  It's a log aggregation component designed to store and query logs.

  **Function**: otel-collector will receive the logs from the *WebObservabilityApplication* send them to loki. In the otel-collector configuration file(otel-collector-config.yml) the loki exporter is defined, where the logs will be sent through the endpoint http://loki:3100/loki/api/v1/push.

  ![Loki Exporter](images/Loki-Exporter-Config.png)
  *Configuring the loki exporter in otel-collector-config.yml*

  An attribute is defined in processors, which allows additional tags to be inserted into those that are sent to loki.
  ![Loki Processors](images/Loki-Processors-Config.png)
  ![Loki Processors](images/Loki-Processors-Config-Class.png)
  *Configuring the app_name attribute in otel-collector-config.yml*

  The pipeline for the logs is defined, where the receiver is the otlp(grpc://0.0.0.0:4317/v1/logs), the processors is the resourse and the exporter is loki. With this configuration, the otel-collector will send the logs to loki.

  ![Loki Pipelines](images/Loki-Pipelines-Config.png)
  *Configuring the logs pipelines in otel-collector-config.yml*

  ### <u>jaeger</u>
  ![Loki](images/Jaeger.png)
  This is a component for distributed tracing. It's used to monitor and resolve performance problems in distributed applications.

  **Function**: otel-collector will receive the traces from the *WebObservabilityApplication* send them to jaeger. In the otel-collector configuration file(otel-collector-config.yml) the otlp/jaeger exporter is defined, where traces will be sent through the jaeger:4317 endpoint, which is the through which jaeger receives traces in otlp format in grpc mode.

  ![Jaeger Exporter](images/Jaeger-Exporter.png)
  *Configuring the jaeger exporter in otel-collector-config.yml*

  The pipeline for traces is defined, where the receiver is otlp(grpc://0.0.0.0:4317/v1/traces), the processors is batch and the exporter is otlp/jaeger. With this configuration, otel-collector will send the traces to jaeger.

  ![Jaeger Pipelines](images/Jaeger-Pipelines-Config.png)
  *Configuring the traces pipelines in otel-collector-config.yml*

  > [!NOTE]
  > The batch processor is responsible for batching traces before sending them. This configuration improves performance by reducing the number of requests send by the otel-collector.
  Jaeger also listens on port 4317 and receives these traces, thanks to the otel-collector sending them on its behalf.
  ![Jaeger Processors](images/Jaeger-Processors-Config.png)
  *Configuring the batch processors in otel-collector-config.yml*

  ### <u>prometheus</u>
  ![Prometheus](images/Prometheus.png)
  It's a monitoring and alerting component, designed to collect service metrics in real-time.

  **Function**: otel-collector will receive metrics from the WebObservabilityApplication and expose them in a format prometheus can understand over port 8889.  
  In the otel-collector configuration file(otel-collector-config.yml) the prometheus exporter is defined, where metrics will be exposed through port 8889 so that prometheus can then scrape and collect the metrics from the collector.

  Parameters configured in prometheus exporter:  
  **endpoint (otel-collector:8889)**: Endpoint where the metrics will be exposed.  
  **namespace (custom-metric)**: It's a prefix that is added to the metrics to group them under the same set.  
  **resource_to_telemetry_conversion (enabled: true)**: Enables the conversion of resource attributes to telemetry(They're added as labels to the exported metrics. Important for custom metrics **"MetricsService.cs"**).  
  **send_timestamps (true)**: Allows including timestamps along with the exported metrics.  
  **enable_open_metrics (true)**: Allows metrics to be exposed in an advanced format that includes support for exemplars. **Exemplars** allow linking metrics(prometheus) with traces(jaeger), to delve deeper into an event.

  ![Prometheus Exporter](images/Prometheus-Exporter-Config.png)
  *Configuring the prometheus exporter in otel-collector-config.yml*

  In the prometheus configuration file(prometheus-config.yml), a scrape config is configured that defines how and when to collect metrics.

  Parameters configured in prometheus config:  
  **scrape_interval (5s)**: Prometheus will attempt to collect metrics every 5 seconds.  
  **evaluation_interval (5s)**: Prometheus will evaluate your rules and alerts every 5 seconds.  
  **job_name(otel-collector)**: Defines the otel-collector job to identify metrics coming from this source.  
  **static_configs (otel-collector:8889)**: Prometheus will scrape the otel-collector:8889 endpoint to collect metrics exposed by the otel-collector.  
  ![Prometheus Scrape](images/Prometheus-Scrape-Config.png)
  *Configuring the scrape in prometheus-config.yml*

  > [!IMPORTANT]
  > Support for exemplars is enabled, so that prometheus can capture and store exemplars, connecting metrics and traces to improve the observability and debugging of the system.
  ![Prometheus Docker](images/Prometheus-Docker-Config.png)
  *Configuring enable-feature=exemplar-storage in docker-compose.yml*

  ### <u>grafana</u>
  ![Grafana](images/Grafana.png)  
  It's a monitoring platform, where the logs, traces, and metrics that will leave our web application will be displayed.

  **Function**: In the grafana datasources configuration file(grafana-datasources-config.yml), the datasources for loki, jaeger, and prometheus are defined, which will allow grafana to query these services to obtain logs, traces, and metrics.

  Grafana datasources:  
  **prometheus datasources**: Grafana queries and displays metrics stored in prometheus through the endpoint http://prometheus:9090.  
  **exemplarTraceIdDestinations(exemplars)** is configured, which will allow associating a trace_id with jaeger. This enables a correlation between metrics and traces, providing complete traceability.  
  ![Grafana Prometheus DataSources](images/Grafana-Prometheus-DataSources.png)
  *Configuring prometheus datasoruces in grafana-datasources-config.yml*

  **jaeger datasources**: Grafana queries and displays traces stored in jaeger through the endpoint http://jaeger:16686. **tracesToLogs** is configured to correlate jaeger traces with loki logs. This will allow a click on the trace to view the associated logs. Also configured **nodeGraph**, which allows grafana to visualize a graph of dependencies between microservices or components based on jaeger traces.  
  ![Grafana Jaeger DataSources](images/Grafana-Jaeger-DataSources.png)
  *Configuring jaeger datasoruces in grafana-datasources-config.yml*

  **loki datasources**: Grafana queries and displays the logs stored in loki through the endpoint http://loki:3100. **derivedFields** is configured where the derived field TraceID is defined, which extracts the traceid from the logs recorded in loki and links them to the traces generated by jaeger. This allows navigating from logs to traces for further analysis.  
  ![Grafana Loki DataSources](images/Grafana-Loki-DataSources.png)
  *Configuring loki datasoruces in grafana-datasources-config.yml*

  In the dashboards.yml file, a provider is configured to load dashboards stored in json files, located in the /etc/grafana/provisioning/dashboards path. This allows the example dashboard(dashboard-grafana.json) to be loaded and then displayed in the grafana portal.

  ![Grafana Dashboard Config](images/Grafana-Dashboard-Config.png)
  ![Grafana Dashboard Json](images/Grafana-Dashboard-Json.png)
  *Configuring provider in dashboards.yml*


## Running the application
  Run the docker-compose up -d command. Validate that the components are up.  
  ![Running Aplicaction Docker Compose](images/Running-Aplicaction-Docker-Compose.png)

  Enter the following url in our browser: https://localhost:8081/swagger. Run the endpoints a couple of times to generate logs, traces, and metrics.
  ![Running Aplicaction Swagger](images/Running-Aplicaction-Swagger.png)

  Enter the following url in our browser: http://localhost:16686/search. Validate that the traces are being recorded.
  ![Running Aplicaction Jaeger](images/Running-Aplicaction-Jaeger.png)

  Enter the following url in our browser: http://localhost:9090/graph. Validate that metrics appear and are grouped by **custom_metric.**
  ![Running Aplicaction Prometheus](images/Running-Aplicaction-Prometheus.png)

  Select a metric, run it, and display the exemplars.
  ![Running Aplicaction Prometheus Metrics](images/Running-Aplicaction-Prometheus-Metrics.png)

  ![Running Aplicaction Prometheus Exemplars](images/Running-Aplicaction-Prometheus-Exemplars.png)

  > **Note:** If there is any problem with the display of metrics, validate in targets that prometheus establishes a connection with the metrics endpoint, otherwise, it will not be able to scrape to obtain the metrics generated by the application.
  ![Running Aplicaction Prometheus Targets](images/Running-Aplicaction-Prometheus-Targets.png)

  Enter the following url in our browser: http://localhost:3000 and enter with the user: **admin** and password: **admin_password**
  ![Grafana Explore](images/Grafana-Explore.png)

  Go to the explore option.
  ![Grafana Explore Option](images/Grafana-Explore-Option.png)

  We make a logs query with loki.
  ![Grafana Explore Loki Query](images/Grafana-Explore-Loki-Query.png)

  The logs is linked to the trace. This is a product of the configuration made in the grafana datasources.
  ![Grafana Explore Loki Logs](images/Grafana-Explore-Loki-Logs.png)

  If you click on the jaeger button, we will see the trace associated with the log in jaeger.
  ![Grafana Explore Loki Jaeger](images/Grafana-Explore-Loki-Jaeger.png)

  We make a trace query with jaeger.
  ![Grafana Explore Jaeger](images/Grafana-Explore-Jaeger.png)

  If you select the trace, you can see that it is linked to the logs.
  ![Grafana Explore Jaeger Loki](images/Grafana-Explore-Jaeger-Loki.png)

  We make a metrics query with prometheus and activate the Exempplars.
  ![Grafana Explore Prometheus](images/Grafana-Explore-Prometheus.png)

  If you select an exemplar, you can see that the metric is associated with a trace.
  ![Grafana Explore Prometheus Exemplars](images/Grafana-Explore-Prometheus-Exemplars.png)

  This allows us to have complete traceability.
  ![Grafana Explore Prometheus Exemplars](images/Grafana-Explore-Prometheus-Jaeger.png)

  Now we go to the dashboard option, to see the example dashboard that was loaded into grafana.

  ![Grafana Dashboard Json](images/Grafana-Dashboard-Json.png)

  ![Grafana Dashboard Example](images/Grafana-Dashboard-Example.png)

  Select the dashboard.
  ![Grafana Dashboard Example](images/Grafana-Dashboard-Example-2.png)

  This is a basic dashboard that has five panels referring to the logs, traces, and metrics of the WebObservabilityApplication web application.  
  On the grafana website(https://grafana.com/grafana/dashboards) we can find advanced templates that can be very useful for implement a dashboard from scratch.
  ![Grafana Dashboard Panel](images/Grafana-Dashboard-Panel.png)


## Conclusion
We hace reached the end of this implementation. It has been a bit long, but the results are beneficial, because having otel-collector as our log, traces, and metrics manager, gives us the advantage of incorporating/evaluating other components. In addition to the portability in terms of taking this solution to the cloud of Azure, Amazon, GCP, etc.  

  #### Contact Addresses
  ##### Linkedin: [Send a message](linkedin.com/in/robert-eduardo-arango-ramos-5b5a32101)