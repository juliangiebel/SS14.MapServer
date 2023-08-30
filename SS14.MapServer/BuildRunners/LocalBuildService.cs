using System.Diagnostics;
using System.Text;
using Sentry;
using Serilog;
using SS14.MapServer.Configuration;
using SS14.MapServer.Exceptions;
using ILogger = Serilog.ILogger;
// ReSharper disable AccessToDisposedClosure

namespace SS14.MapServer.BuildRunners;

public sealed class LocalBuildService
{
    private readonly BuildConfiguration _configuration = new();
    private readonly ILogger _log;

    public LocalBuildService(IConfiguration configuration)
    {
        configuration.Bind(BuildConfiguration.Name, _configuration);
        _log = Log.ForContext(typeof(LocalBuildService));
    }

    public async Task<string> GetDotNetVersion(CancellationToken cancellationToken = default)
    {
        using var process = new Process();
        SetUpProcess(process);
        process.StartInfo.Arguments = "--version";
        process.Start();
        await process.WaitForExitAsync(cancellationToken);
        return await process.StandardOutput.ReadToEndAsync(cancellationToken);
    }

    public async Task<string> BuildAndRun(string directory, string command, List<string> arguments, CancellationToken cancellationToken = default)
    {
        await Build(directory, cancellationToken);
        await Run(directory, command, arguments, true, cancellationToken);
        return Path.Join(directory, _configuration.RelativeMapFilesPath);
    }

    public async Task Build(string directory, CancellationToken cancellationToken = default)
    {
        using var process = new Process();
        SetUpProcess(process);
        process.StartInfo.WorkingDirectory = directory;

        var outputDir = Path.Join(directory, _configuration.RelativeOutputPath);
        process.StartInfo.Arguments = "build -c Release";

        using var logStream = new MemoryStream();
        await using var logWriter = new StreamWriter(logStream);
        process.OutputDataReceived += (_, args) => LogOutput(args, logWriter);

        _log.Information("Started building {ProjectName}", _configuration.MapRendererProjectName);

        process.Start();
        process.BeginOutputReadLine();
        await process.WaitForExitAsync(cancellationToken).WaitAsync(TimeSpan.FromMinutes(_configuration.ProcessTimeoutMinutes), cancellationToken);
        process.CancelOutputRead();

        if (!process.HasExited)
        {
            process.Kill();
            throw new BuildException($"Building timed out {_configuration.MapRendererProjectName}");
        }

        if (process.ExitCode != 0)
        {
            process.Close();
            logWriter.Close();
            var exception = new BuildException($"Failed building {_configuration.MapRendererProjectName}");
            CaptureBuildRunnerException(exception, logStream);
        }

        process.Close();
        logWriter.Close();
        _log.Information("Build finished");
    }

    public async Task Run(string directory, string command, List<string> arguments, bool joinExecutablePath = false, CancellationToken cancellationToken = default)
    {
        var executablePath = joinExecutablePath ? Path.Join(directory, command) : command;

        using var process = new Process();
        SetUpProcess(process, executablePath);
        process.StartInfo.WorkingDirectory = directory;
        process.StartInfo.Arguments = string.Join(' ', arguments);

        using var logStream = new MemoryStream();
        await using var logWriter = new StreamWriter(logStream);
        process.OutputDataReceived += (_, args) => LogOutput(args, logWriter);
        process.ErrorDataReceived += (_, args) => LogOutput(args, logWriter);

        _log.Information("Running: {Command} {Arguments}", command, string.Join(' ', arguments));

        // ReSharper disable once AccessToDisposedClosure
        await Task.Run(() => process.Start(), cancellationToken).WaitAsync(TimeSpan.FromMinutes(1), cancellationToken);

        if (process.HasExited)
            throw new BuildException($"Run timed out {_configuration.MapRendererProjectName}");

        process.BeginErrorReadLine();
        process.BeginOutputReadLine();
        await process.WaitForExitAsync(cancellationToken).WaitAsync(TimeSpan.FromMinutes(_configuration.ProcessTimeoutMinutes), cancellationToken);
        process.CancelErrorRead();
        process.CancelOutputRead();

        if (!process.HasExited)
        {
            process.Kill();
            throw new BuildException($"Run timed out {_configuration.MapRendererProjectName}");
        }

        if (process.ExitCode == 0)
        {
            var exception = new BuildException($"Running {command} {string.Join(' ', arguments)} failed");
            CaptureBuildRunnerException(exception, logStream);
            process.Close();
            logWriter.Close();
        }

        process.Close();
        logWriter.Close();
        _log.Information("Run finished");
    }

    private void SetUpProcess(Process process, string? executable = "dotnet")
    {
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.FileName = executable;
    }

    private void LogOutput(DataReceivedEventArgs args, TextWriter logWriter)
    {
        if (args.Data == null)
            return;

        logWriter.Write(args.Data);
        _log.Debug("{Output}", args.Data);
    }

    private void CaptureBuildRunnerException(Exception exception, Stream stream)
    {
        if (!SentrySdk.IsEnabled)
            throw exception;

        var content = new StreamAttachmentContent(stream);
        var attachment = new Attachment(AttachmentType.Default, content, "run.log", null);
        var breadcrumb = new Breadcrumb("Captured run log", "log");

        SentrySdk.CaptureException(exception, scope =>
            {
                scope.AddBreadcrumb(breadcrumb, Hint.WithAttachments(attachment));
            });

    }
}
