using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Sentry;
using Serilog;
using SS14.MapServer.Configuration;
using SS14.MapServer.Exceptions;
using ILogger = Serilog.ILogger;
// ReSharper disable AccessToDisposedClosure

namespace SS14.MapServer.BuildRunners;

public sealed partial class LocalBuildService
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

        var logBuffer = new StringBuilder();
        process.OutputDataReceived += (_, args) => LogOutput(logBuffer, args);

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
            var exception = new BuildException($"Failed building {_configuration.MapRendererProjectName}");
            ProcessLocalBuildException(logBuffer.ToString(), "build.log", exception);
        }

        _log.Information("Build finished");
    }

    public async Task<string> Run(string directory, string command, List<string> arguments, bool joinExecutablePath = false, CancellationToken cancellationToken = default)
    {
        var executablePath = joinExecutablePath ? Path.Join(directory, command) : command;

        using var process = new Process();
        SetUpProcess(process, executablePath);
        process.StartInfo.WorkingDirectory = directory;
        process.StartInfo.Arguments = string.Join(' ', arguments);

        var logBuffer = new StringBuilder();
        process.OutputDataReceived += (_, args) => LogOutput(logBuffer, args);
        process.ErrorDataReceived += (_, args) => LogOutput(logBuffer, args);

        _log.Information("Running: {Command} {Arguments}", command, string.Join(' ', arguments));

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

        var log = logBuffer.ToString();
        // Bandaid the fact that the map renderer doesn't return an error code when rendering fails

        if (process.ExitCode != 0 || LogErrorRegex().IsMatch(log))
        {
            var exception = new BuildException($"Error while running: {command} {string.Join(' ', arguments)}");
            ProcessLocalBuildException(log, "run.log", exception);
        }

        _log.Information("Run finished");
        return log;
    }

    private void SetUpProcess(Process process, string? executable = "dotnet")
    {
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.FileName = executable;
    }

    private void LogOutput(StringBuilder logBuffer, DataReceivedEventArgs args)
    {
        if (args.Data == null)
            return;

        logBuffer.Append($"{args.Data}\n");
        _log.Debug("{Output}", args.Data);
    }

    private void ProcessLocalBuildException(string log, string fileName, Exception exception)
    {
        if (!SentrySdk.IsEnabled)
            throw exception;

        var data = Encoding.UTF8.GetBytes(log);
        SentrySdk.CaptureException(exception,
            scope =>
            {

                scope.AddAttachment(data, fileName, AttachmentType.Default, "text/plain");
            });
    }

    [GeneratedRegex("error?|exception|fatal")]
    private static partial Regex LogErrorRegex();
}
