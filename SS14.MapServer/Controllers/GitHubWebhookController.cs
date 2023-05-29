using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Octokit;
using SS14.GithubApiHelper.Helpers;
using SS14.MapServer.Configuration;
using SS14.MapServer.Helpers;

namespace SS14.MapServer.Controllers;

[Route("api/[controller]")]
[ApiController]
public class GitHubWebhookController : ControllerBase
{
    private const string GithubEventHeader = "x-github-event";
    private const string GitBranchRefPrefix = "refs/heads/";

    private readonly IConfiguration _configuration;
    private readonly GitConfiguration _gitConfiguration;

    public GitHubWebhookController(IConfiguration configuration)
    {
        _configuration = configuration;
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
                HandlePushEvent(serializer.Deserialize<PushEventPayload>(json));
                break;
        }
    }

    private void HandlePushEvent(PushEventPayload payload)
    {
        if (!payload.Ref.Equals(GitBranchRefPrefix + _gitConfiguration.Branch))
            return;

        foreach (var commit in payload.Commits)
        {
            
        }
        //TODO: Check if branch matches configured target branch and schedule an update maps job
    }
}