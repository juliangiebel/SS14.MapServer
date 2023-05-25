using System.Diagnostics;
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

    public async Task<string> BuildAndRun(string directory, List<string> command)
    {
        throw new NotImplementedException();
    }

    public async Task Build(string directory)
    {
        using var process = new Process();
        SetUpProcess(process);
        
        var outputDir = Path.Join(directory, _configuration.RelativeOutputPath);
        process.StartInfo.Arguments = $"build -o {outputDir}";
        
        process.ErrorDataReceived += ProcessOnErrorDataReceived;
        process.OutputDataReceived += ProcessOnOutputDataReceived;
        
        _log.Information("Started building {ProjectName}", _configuration.MapRendererProjectName);
        process.Start();
        await process.WaitForExitAsync();

        var output = await process.StandardOutput.ReadToEndAsync();
        _log.Debug("Build output:\n{Output}", output);

        if (process.ExitCode != 0)
            throw new BuildException($"Failed building {_configuration.MapRendererProjectName}");
        
        _log.Information("Build finished");
    }

    private void ProcessOnOutputDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (e.Data == null)
            return;
        
        _log.Debug(e.Data);
    }

    private void ProcessOnErrorDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (e.Data == null)
            return;
        
        _log.Error(e.Data);
    }

    private void SetUpProcess(Process process, string? command = "dotnet.exe")
    {
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.FileName = command;
    }
}