using System.Diagnostics;
using Serilog;
using SS14.MapServer.Configuration;
using SS14.MapServer.Exceptions;
using ILogger = Serilog.ILogger;

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
        await Run(directory, command, arguments, cancellationToken);
        return Path.Join(directory, _configuration.RelativeMapFilesPath);
    }

    public async Task Build(string directory, CancellationToken cancellationToken = default)
    {
        using var process = new Process();
        SetUpProcess(process);
        process.StartInfo.WorkingDirectory = directory;

        var outputDir = Path.Join(directory, _configuration.RelativeOutputPath);
        process.StartInfo.Arguments = "build -c Release";
        process.OutputDataReceived += LogOutput;

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
            throw new BuildException($"Failed building {_configuration.MapRendererProjectName}");

        _log.Information("Build finished");
    }

    public async Task Run(string directory, string command, List<string> arguments, CancellationToken cancellationToken = default, bool joinExecutablePath = false)
    {
        var executablePath = joinExecutablePath ? Path.Join(directory, command) : command;

        using var process = new Process();
        SetUpProcess(process, executablePath);
        process.StartInfo.WorkingDirectory = directory;
        process.StartInfo.Arguments = string.Join(' ', arguments);
        process.OutputDataReceived += LogOutput;
        process.ErrorDataReceived += LogOutput;

        _log.Information("Running: {Command} {Arguments}", command, string.Join(' ', arguments));

        process.Start();
        _log.Debug("Started process");

        process.BeginErrorReadLine();
        process.BeginOutputReadLine();
        _log.Debug("Waiting for process exit...");
        await process.WaitForExitAsync(cancellationToken).WaitAsync(TimeSpan.FromMinutes(_configuration.ProcessTimeoutMinutes), cancellationToken);
        _log.Debug("Stopped process");
        process.CancelErrorRead();
        process.CancelOutputRead();

        if (!process.HasExited)
        {
            process.Kill();
            throw new BuildException($"Run timed out {_configuration.MapRendererProjectName}");
        }

        _log.Information("Run finished");
    }
    private void SetUpProcess(Process process, string? executable = "dotnet")
    {
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.FileName = executable;
    }

    private void LogOutput(object _, DataReceivedEventArgs args)
    {
        if (args.Data == null)
            return;

        _log.Debug("{Output}", args.Data);
    }
}
