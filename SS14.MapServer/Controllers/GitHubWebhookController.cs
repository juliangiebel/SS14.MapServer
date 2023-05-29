using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    private readonly GitConfiguration _gitConfiguration;

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

        if (!await CheckFiles(payload))
            return;
        //TODO: Check if branch matches configured target branch and schedule an update maps job
    }

    private async Task<bool> CheckFiles(PatchedPushEventPayload payload)
    {
        if (!_gitConfiguration.RetrieveMapFilesFromDiff)
            return true;


        var files = await _githubApiService.GetChangedFilesBetweenCommits(
            payload.Installation.Id, 
            payload.Repository.Id, 
            payload.Before, 
            payload.Head);

        return false;
    }
}

public sealed class PatchedPushEventPayload : PushEventPayload
{
    public string Before { get; private set; }
}