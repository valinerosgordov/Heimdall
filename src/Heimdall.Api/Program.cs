using System.Text;
using System.Threading.RateLimiting;
using Heimdall.Api.Common;
using Heimdall.Api.Endpoints;
using Heimdall.Api.Security;
using Heimdall.Application.Alerting;
using Heimdall.Application.HealthChecks;
using Heimdall.Application.Hosts;
using Heimdall.Application.Inventory;
using Heimdall.Application.Metrics;
using Heimdall.Application.Overview;
using Heimdall.Contracts;
using Heimdall.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

const string webCorsPolicy = "heimdall-web";

builder.Services.AddInfrastructure(builder.Configuration);

// Application handlers — explicit, scoped, no MediatR.
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IngestMetricsHandler>();
builder.Services.AddScoped<QueryHostMetricsHandler>();
builder.Services.AddScoped<ListHostsHandler>();
builder.Services.AddScoped<EnrollHostHandler>();
builder.Services.AddScoped<CreateHealthCheckTargetHandler>();
builder.Services.AddScoped<ListHealthChecksHandler>();
builder.Services.AddScoped<DeleteHealthCheckTargetHandler>();
builder.Services.AddScoped<GetHealthCheckHistoryHandler>();
builder.Services.AddScoped<GetOverviewHandler>();
builder.Services.AddScoped<CreateAlertRuleHandler>();
builder.Services.AddScoped<ListAlertRulesHandler>();
builder.Services.AddScoped<DeleteAlertRuleHandler>();
builder.Services.AddScoped<ListAlertsHandler>();
builder.Services.AddScoped<ListInventoryHandler>();
builder.Services.AddScoped<CreateServerHandler>();
builder.Services.AddScoped<UpdateServerHandler>();
builder.Services.AddScoped<DeleteServerHandler>();
builder.Services.AddScoped<CreateServerLinkHandler>();
builder.Services.AddScoped<DeleteServerLinkHandler>();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Per-IP rate limiting on the agent-facing write endpoints.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("ingest", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = 120, Window = TimeSpan.FromSeconds(10), QueueLimit = 0 }));
});

// OpenTelemetry traces + metrics (console exporter for dev; swap AddConsoleExporter for AddOtlpExporter in prod).
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("Heimdall.Api"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation(options =>
            // Never trace the Telegram URL — it carries the bot token in the path.
            options.FilterHttpRequestMessage = request => request.RequestUri?.Host != "api.telegram.org")
        .AddConsoleExporter())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddConsoleExporter());

// AOT-friendly JSON: prefer the source-generated contracts, fall back to reflection for framework types.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, HeimdallJsonContext.Default));

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddPolicy(webCorsPolicy, policy =>
    policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

// JWT bearer auth protects the dashboard read/admin endpoints. Agent endpoints use key auth, not JWT.
var jwtSecretProvider = new JwtSecretProvider(builder.Configuration);
builder.Services.AddSingleton(jwtSecretProvider);
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = JwtTokenService.Issuer,
            ValidateAudience = true,
            ValidAudience = JwtTokenService.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretProvider.Secret)),
            ClockSkew = TimeSpan.FromSeconds(30),
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                // EventSource (SSE) cannot set Authorization headers — accept ?access_token= for stream endpoints.
                var token = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(token) && context.HttpContext.Request.Path.StartsWithSegments("/api/stream"))
                    context.Token = token;
                return Task.CompletedTask;
            },
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseExceptionHandler();
app.UseRateLimiter();
app.UseCors(webCorsPolicy);

// Serve the static-exported dashboard (wwwroot) — public assets, before auth.
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => TypedResults.Ok(new { status = "ok" }));
app.MapAuthEndpoints();
app.MapEnrollmentEndpoints();
app.MapIngestEndpoints();
app.MapHostEndpoints();
app.MapHealthCheckEndpoints();
app.MapOverviewEndpoints();
app.MapAlertEndpoints();
app.MapServerEndpoints();

// SPA fallback: any non-API, non-asset route returns index.html so the client router can handle it.
app.MapFallbackToFile("index.html");

app.Run();
