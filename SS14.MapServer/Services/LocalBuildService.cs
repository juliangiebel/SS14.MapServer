using System.Diagnostics;
using SS14.MapServer.Configuration;

namespace SS14.MapServer.Services;

public sealed class LocalBuildService
{
    private readonly BuildConfiguration _configuration = new();

    public LocalBuildService(IConfiguration configuration)
    {
        configuration.Bind(BuildConfiguration.Name, _configuration);
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

    public async Task<string> BuildAndRun(string directory, string[] command)
    {
        throw new NotImplementedException();
    }
    
    private void SetUpProcess(Process process)
    {
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.FileName = "dotnet.exe";
    }
}