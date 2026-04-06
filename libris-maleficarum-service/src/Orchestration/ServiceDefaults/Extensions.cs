using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ServiceDiscovery;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Microsoft.Extensions.Hosting;

// Adds common Aspire services: service discovery, resilience, health checks, and OpenTelemetry.
// This project should be referenced by each service project in your solution.
// To learn more about using this project, see https://aka.ms/dotnet/aspire/service-defaults
public static class Extensions
{
    private const string HealthEndpointPath = "/health";
    private const string AlivenessEndpointPath = "/alive";

    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.ConfigureOpenTelemetry();

        builder.AddDefaultHealthChecks();

        builder.Services.AddServiceDiscovery();

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            // Turn on resilience by default
            http.AddStandardResilienceHandler();

            // Turn on service discovery by default
            http.AddServiceDiscovery();
        });

        // Uncomment the following to restrict the allowed schemes for service discovery.
        // builder.Services.Configure<ServiceDiscoveryOptions>(options =>
        // {
        //     options.AllowedSchemes = ["https"];
        // });

        return builder;
    }

    public static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    // Add custom application meters
                    .AddMeter("LibrisMaleficarum.Api");
            })
            .WithTracing(tracing =>
            {
                tracing.AddSource(builder.Environment.ApplicationName)
                    // Add custom application activity source
                    .AddSource("LibrisMaleficarum.Api")
                    .AddAspNetCoreInstrumentation(tracing =>
                        // Exclude health check requests from tracing
                        tracing.Filter = context =>
                            !context.Request.Path.StartsWithSegments(HealthEndpointPath)
                            && !context.Request.Path.StartsWithSegments(AlivenessEndpointPath)
                    )
                    // Uncomment the following line to enable gRPC instrumentation (requires the OpenTelemetry.Instrumentation.GrpcNetClient package)
                    //.AddGrpcClientInstrumentation()
                    .AddHttpClientInstrumentation();

                // Use AlwaysOnSampler in development to view all traces
                if (builder.Environment.IsDevelopment())
                {
                    tracing.SetSampler(new AlwaysOnSampler());
                }
            })
            .ConfigureResource(resource => resource
                .AddService(serviceName: builder.Environment.ApplicationName));

        builder.AddOpenTelemetryExporters();

        return builder;
    }

    private static TBuilder AddOpenTelemetryExporters<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        if (useOtlpExporter)
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }

        // Uncomment the following lines to enable the Azure Monitor exporter (requires the Azure.Monitor.OpenTelemetry.AspNetCore package)
        //if (!string.IsNullOrEmpty(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
        //{
        //    builder.Services.AddOpenTelemetry()
        //       .UseAzureMonitor();
        //}

        return builder;
    }

    /// <summary>
    /// Creates and registers the custom application Meter and ActivitySource for instrumentation.
    /// These are registered as singletons so they can be injected into services.
    /// The Meter creates its own counters on demand.
    /// </summary>
    public static TBuilder AddApplicationTelemetry<TBuilder>(this TBuilder builder, string serviceName = "LibrisMaleficarum.Api") where TBuilder : IHostApplicationBuilder
    {
        // Create custom application meter - counters will be created by TelemetryService as needed
        var applicationMeter = new Meter(serviceName, "1.0.0");

        // Create custom activity source for distributed tracing
        var applicationActivitySource = new ActivitySource(serviceName);

        // Register as singletons for dependency injection
        builder.Services.AddSingleton(applicationMeter);
        builder.Services.AddSingleton(applicationActivitySource);

        return builder;
    }

    public static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddHealthChecks()
            // Add a default liveness check to ensure app is responsive
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return builder;
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        // All health checks must pass for app to be considered ready to accept traffic after starting
        app.MapHealthChecks(HealthEndpointPath, new HealthCheckOptions
        {
            ResponseWriter = WriteDetailedHealthResponse,
        })
            .AllowAnonymous();

        // Only health checks tagged with the "live" tag must pass for app to be considered alive
        app.MapHealthChecks(AlivenessEndpointPath, new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("live"),
        })
            .AllowAnonymous();

        return app;
    }

    private static async Task WriteDetailedHealthResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        var entries = report.Entries.ToDictionary(
            e => e.Key,
            e => (object)new
            {
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration.ToString(),
                exception = e.Value.Exception?.GetType().Name,
                tags = e.Value.Tags,
            });

        var response = new
        {
            status = report.Status.ToString(),
            totalDuration = report.TotalDuration.ToString(),
            entries,
        };

        await context.Response.WriteAsJsonAsync(response, options);
    }
}
