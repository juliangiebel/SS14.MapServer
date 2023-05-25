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
        
        var outputDir = Path.Join(directory, _configuration.RelativeOutputPath);
        process.StartInfo.Arguments = $"build -o {outputDir}";

        _log.Information("Started building {ProjectName}", _configuration.MapRendererProjectName);
        process.Start();
        await process.WaitForExitAsync();

        var output = await process.StandardOutput.ReadToEndAsync();
        _log.Debug("Build output:\n{Output}", output);

        if (process.ExitCode != 0)
            throw new BuildException($"Failed building {_configuration.MapRendererProjectName}");
        
        _log.Information("Build finished");
    }

    public async Task Run(string directory, string command, List<string> arguments)
    { 
        var executablePath = Path.Join(directory, command);

        using var process = new Process();
        SetUpProcess(process, executablePath);
        process.StartInfo.ArgumentList.AddRange(arguments);
       
        _log.Information("Running: {Command} {Arguments}", command, string.Join(' ', arguments));
        process.Start();
        await process.WaitForExitAsync();
        
        var output = await process.StandardOutput.ReadToEndAsync();
        _log.Debug("Output:\n{Output}", output);

        _log.Information("Run finished");

    }
    private void SetUpProcess(Process process, string? executable = "dotnet.exe")
    {
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.FileName = executable;
    }
}