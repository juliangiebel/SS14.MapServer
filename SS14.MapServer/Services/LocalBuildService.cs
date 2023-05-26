using System.Diagnostics;
using NuGet.Packaging;
using Serilog;
using SS14.MapServer.Configuration;
using ILogger = Serilog.ILogger;

namespace SS14.MapServer.Services;

public sealed class LocalBuildService
{
    private readonly BuildConfiguration _configuration = new();
    private readonly ILogger _log;
    
    public LocalBuildService(IConfiguration configuration)
    {
        configuration.Bind(BuildConfiguration.Name, _configuration);
        _log = Log.ForContext(typeof(LocalBuildService));
    }

    public async Task<string> GetDotNetVersion()
    {
        using var process = new Process();
        SetUpProcess(process);
        process.StartInfo.Arguments = "--version";
        process.Start();
        await process.WaitForExitAsync();
        return await process.StandardOutput.ReadToEndAsync();
    }

    public async Task<string> BuildAndRun(string directory, string command, List<string> arguments)
    {
        await Build(directory);
        await Run(directory, command, arguments);
        return "";
    }

    public async Task Build(string directory)
    {
        using var process = new Process();
        SetUpProcess(process);
        process.StartInfo.WorkingDirectory = directory;

        var outputDir = Path.Join(directory, _configuration.RelativeOutputPath);
        process.StartInfo.Arguments = "build";
        process.OutputDataReceived += LogOutput;
        
        _log.Information("Started building {ProjectName}", _configuration.MapRendererProjectName);
        
        process.Start();
        process.BeginOutputReadLine();
        await process.WaitForExitAsync().WaitAsync(TimeSpan.FromMinutes(_configuration.ProcessTimeoutMinutes));
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

    public async Task Run(string directory, string command, List<string> arguments)
    {
        var executablePath = Path.Join(directory, command);
        
        using var process = new Process();
        SetUpProcess(process, executablePath);
        process.StartInfo.WorkingDirectory = directory;
        process.StartInfo.Arguments = string.Join(' ', arguments);
        process.OutputDataReceived += LogOutput;
       
        _log.Information("Running: {Command} {Arguments}", command, string.Join(' ', arguments));
        
        process.Start();
        process.BeginOutputReadLine();
        await process.WaitForExitAsync().WaitAsync(TimeSpan.FromMinutes(_configuration.ProcessTimeoutMinutes));
        process.CancelOutputRead();
        
        if (!process.HasExited)
        {
            process.Kill();
            throw new BuildException($"Run timed out {_configuration.MapRendererProjectName}");
        }
        
        _log.Information("Run finished");
    }
    private void SetUpProcess(Process process, string? executable = "dotnet.exe")
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