using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Windows;

namespace Heimdall.Desktop;

/// <summary>
/// The Heimdall desktop shell: on launch it brings up TimescaleDB (Docker), the published API and agent,
/// then shows the dashboard in an embedded WebView2. Closing the window stops everything.
/// </summary>
public partial class MainWindow : Window
{
    private const string ApiBaseUrl = "http://localhost:5087";

    private readonly string _distDir;       // ...\dist
    private readonly string _repoRoot;      // repo root (has docker-compose.yml)
    private readonly List<Process> _children = [];

    public MainWindow()
    {
        InitializeComponent();
        var exeDir = AppContext.BaseDirectory;                              // ...\dist\desktop
        _distDir = Path.GetFullPath(Path.Combine(exeDir, ".."));           // ...\dist
        _repoRoot = Path.GetFullPath(Path.Combine(exeDir, "..", ".."));    // repo root
        Loaded += OnLoaded;
        Closing += OnClosing;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            SetStatus("Starting database…");
            await Task.Run(EnsureDatabase);

            SetStatus("Starting services…");
            StartBackend();

            SetStatus("Waiting for the dashboard…");
            await WaitForApiAsync();

            SetStatus("Loading…");
            await Web.EnsureCoreWebView2Async();
            Web.NavigationCompleted += (_, args) =>
            {
                if (args.IsSuccess)
                {
                    Loading.Visibility = Visibility.Collapsed;
                    Web.Visibility = Visibility.Visible;
                }
            };
            Web.CoreWebView2.Navigate(ApiBaseUrl);
        }
        catch (Exception ex)
        {
            SetStatus("Could not start Heimdall: " + ex.Message);
        }
    }

    private void EnsureDatabase()
    {
        if (!IsDockerRunning())
        {
            var dockerDesktop = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "Docker", "Docker", "Docker Desktop.exe");
            if (File.Exists(dockerDesktop))
                Process.Start(new ProcessStartInfo(dockerDesktop) { UseShellExecute = true });

            for (var i = 0; i < 60 && !IsDockerRunning(); i++)
                Thread.Sleep(3000);
        }

        RunToExit("docker", $"compose -f \"{ComposeFile}\" up -d");
    }

    private static bool IsDockerRunning()
    {
        try { return RunToExit("docker", "info") == 0; }
        catch { return false; }
    }

    private void StartBackend()
    {
        _children.Add(StartChild(Path.Combine(_distDir, "api", "Heimdall.Api.exe"), Path.Combine(_distDir, "api")));
        _children.Add(StartChild(Path.Combine(_distDir, "agent", "Heimdall.Agent.exe"), Path.Combine(_distDir, "agent")));
    }

    private static Process StartChild(string exePath, string workingDirectory)
    {
        var psi = new ProcessStartInfo(exePath)
        {
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        psi.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        return Process.Start(psi) ?? throw new InvalidOperationException($"Failed to start {exePath}.");
    }

    private static async Task WaitForApiAsync()
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        for (var i = 0; i < 60; i++)
        {
            try
            {
                if ((await http.GetAsync($"{ApiBaseUrl}/health")).IsSuccessStatusCode)
                    return;
            }
            catch { /* not up yet */ }
            await Task.Delay(1000);
        }
        throw new TimeoutException("The API did not become ready in time.");
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        foreach (var child in _children)
        {
            try { if (!child.HasExited) child.Kill(entireProcessTree: true); }
            catch { /* best effort */ }
        }
        try { RunToExit("docker", $"compose -f \"{ComposeFile}\" stop"); }
        catch { /* best effort */ }
    }

    private string ComposeFile => Path.Combine(_repoRoot, "docker-compose.yml");

    private static int RunToExit(string fileName, string arguments)
    {
        using var process = Process.Start(new ProcessStartInfo(fileName, arguments)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        }) ?? throw new InvalidOperationException($"Failed to start {fileName}.");
        process.WaitForExit();
        return process.ExitCode;
    }

    private void SetStatus(string text) => Dispatcher.Invoke(() => Status.Text = text);
}
