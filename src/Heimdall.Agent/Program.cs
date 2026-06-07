using Heimdall.Agent;
using Heimdall.Agent.Collectors;
using Heimdall.Agent.Enrollment;
using Heimdall.Agent.Push;
using Heimdall.Agent.Workers;
using Microsoft.Extensions.Options;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<HeimdallAgentOptions>(
    builder.Configuration.GetSection(HeimdallAgentOptions.SectionName));

builder.Services.AddSingleton(TimeProvider.System);

if (OperatingSystem.IsWindows())
    builder.Services.AddSingleton<ICpuMemorySource, WindowsCpuMemorySource>();
else if (OperatingSystem.IsLinux())
    builder.Services.AddSingleton<ICpuMemorySource, LinuxCpuMemorySource>();
else
    throw new PlatformNotSupportedException("The Heimdall agent supports Windows and Linux.");

builder.Services.AddSingleton<ISystemMetricsCollector, SystemMetricsCollector>();
builder.Services.AddSingleton<AgentKeyStore>();

builder.Services.AddHttpClient<HeimdallApiClient>((serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<IOptions<HeimdallAgentOptions>>().Value;
    client.BaseAddress = new Uri(options.ServerUrl.TrimEnd('/') + "/");
    // A hung server must not stall the collection loop for the default 100s.
    client.Timeout = TimeSpan.FromSeconds(Math.Clamp(options.IntervalSeconds * 2, 5, 30));
});

builder.Services.AddHostedService<MetricCollectionWorker>();

var host = builder.Build();
host.Run();
