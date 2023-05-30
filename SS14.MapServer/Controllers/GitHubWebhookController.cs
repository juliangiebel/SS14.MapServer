using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using Microsoft.IdentityModel.Tokens;
using Octokit;
using SS14.GithubApiHelper.Helpers;
using SS14.MapServer.Configuration;
using SS14.MapServer.Helpers;
using SS14.MapServer.Services;

namespace SS14.MapServer.Controllers;

[Route("api/[controller]")]
[ApiController]
public class GitHubWebhookController : ControllerBase
{
    private const string GithubEventHeader = "x-github-event";
    private const string GitBranchRefPrefix = "refs/heads/";

    private readonly GithubApiService _githubApiService;
    
    private readonly IConfiguration _configuration;
    private readonly GitConfiguration _gitConfiguration = new();

    public GitHubWebhookController(IConfiguration configuration, GithubApiService githubApiService)
    {
        _configuration = configuration;
        _githubApiService = githubApiService;
        configuration.Bind(GitConfiguration.Name, _gitConfiguration);
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task Post()
    {
        Request.EnableBuffering();
        if (!Request.Headers.TryGetValue(GithubEventHeader, out var eventName) || !await GithubWebhookHelper.VerifyWebhook(Request, _configuration))
            return;

        var json = await GithubWebhookHelper.RetrievePayload(Request);
        var serializer = new Octokit.Internal.SimpleJsonSerializer();

        switch (eventName)
        {
            case "push":
                await HandlePushEvent(serializer.Deserialize<PatchedPushEventPayload>(json));
                break;
        }
    }

    private async Task HandlePushEvent(PatchedPushEventPayload payload)
    {
        if (!payload.Ref.Equals(GitBranchRefPrefix + _gitConfiguration.Branch))
            return;

        var files = await CheckFiles(payload);
        if (files.IsNullOrEmpty())
            return;
        //TODO: Check if branch matches configured target branch and schedule an update maps job
    }

    private async Task<IEnumerable<string>> CheckFiles(PatchedPushEventPayload payload)
    {
        var enumerable = await _githubApiService.GetChangedFilesBetweenCommits(
            payload.Installation.Id, 
            payload.Repository.Id, 
            payload.Before, 
            payload.After);

        var files = enumerable.ToList();
        
        //Check for map files
        var mapFileMatcher = new Matcher();
        mapFileMatcher.AddIncludePatterns(_gitConfiguration.MapFilePatterns);
        mapFileMatcher.AddExcludePatterns(_gitConfiguration.MapFileExcludePatterns);
        var mapFileMatchResult = mapFileMatcher.Match(files);

        if (!mapFileMatchResult.HasMatches)
            return Enumerable.Empty<string>();

        if (!_gitConfiguration.DontRunWithCodeChanges)
            return mapFileMatchResult.Files.Select(match => match.Path);
        
        //Check for code files
        var codeFileMatcher = new Matcher();
        codeFileMatcher.AddIncludePatterns(_gitConfiguration.CodeChangePatterns);
        var codeFileMatchResult = codeFileMatcher.Match(files);

        if (codeFileMatchResult.HasMatches)
            return Enumerable.Empty<string>();

        return mapFileMatchResult.Files.Select(match => match.Path);
    }
}

/// <summary>
/// Imagine having a stable API lol. Contains the correct properties for the commit before and after the push event.
/// </summary>
public sealed class PatchedPushEventPayload : PushEventPayload
{
    public string Before { get; private set; }
    public string After { get; private set; }
    public new string Ref { get; private set; }
}