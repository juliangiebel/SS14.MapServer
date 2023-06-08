using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using Microsoft.IdentityModel.Tokens;
using Octokit;
using SS14.GithubApiHelper.Helpers;
using SS14.MapServer.Configuration;
using SS14.MapServer.Helpers;
using SS14.MapServer.MapProcessing;
using SS14.MapServer.MapProcessing.Services;
using SS14.MapServer.Models;
using SS14.MapServer.Models.Entities;
using SS14.MapServer.Services;
using SS14.MapServer.Services.Github;

namespace SS14.MapServer.Controllers;

[Route("api/[controller]")]
[ApiController]
public class GitHubWebhookController : ControllerBase
{
    private const string GithubEventHeader = "x-github-event";
    private const string GitBranchRefPrefix = "refs/heads/";

    private readonly GithubApiService _githubApiService;
    private readonly ProcessQueue _processQueue;

    private readonly IConfiguration _configuration;
    private readonly GitConfiguration _gitConfiguration = new();
    private readonly ServerConfiguration _serverConfiguration = new();
    private readonly BuildConfiguration _buildConfiguration = new();

    private Context _context;

    public GitHubWebhookController(
        IConfiguration configuration,
        GithubApiService githubApiService,
        ProcessQueue processQueue,
        Context context)
    {
        _configuration = configuration;
        _githubApiService = githubApiService;
        _processQueue = processQueue;
        _context = context;
        configuration.Bind(GitConfiguration.Name, _gitConfiguration);
        configuration.Bind(ServerConfiguration.Name, _serverConfiguration);
        configuration.Bind(BuildConfiguration.Name, _buildConfiguration);
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Post()
    {
        if (!_buildConfiguration.Enabled)
            return NotFound("Automated building features are disabled");

        Request.EnableBuffering();
        if (!Request.Headers.TryGetValue(GithubEventHeader, out var eventName) || !await GithubWebhookHelper.VerifyWebhook(Request, _configuration))
            return Unauthorized();

        var json = await GithubWebhookHelper.RetrievePayload(Request);
        var serializer = new Octokit.Internal.SimpleJsonSerializer();

        switch (eventName)
        {
            case "push":
                await HandlePushEvent(serializer.Deserialize<PatchedPushEventPayload>(json));
                break;
            case "pull_request":
                await HandlePullRequestEvent(serializer.Deserialize<PullRequestEventPayload>(json));
                break;
        }

        return Ok();
    }

    private async Task HandlePullRequestEvent(PullRequestEventPayload payload)
    {
        if (!_gitConfiguration.RunOnPullRequests || payload.Action != "synchronize" && payload.Action != "opened" )
            return;

        var headCommit = payload.PullRequest.Head;
        var repository = payload.PullRequest.Head.Repository.CloneUrl;

        var enumerable = await CheckFiles(
            payload.Installation.Id,
            payload.Repository.Id,
            payload.PullRequest.Base.Ref,
            $"{headCommit.User.Login}:{headCommit.Ref}"
            );

        var files = enumerable.Select(Path.GetFileName).ToList();

        if (files.Count == 0)
            return;

        await CreateInitialPrComment(payload, payload.PullRequest.Base, files);

        var installation = new InstallationIdentifier(payload.Installation.Id, payload.Repository.Id);

        //Ensure the the ref will always just be the branch name
        var bareRef = Path.GetFileName(headCommit.Ref);
        var processItem = new ProcessItem(
            $"pull/{payload.PullRequest.Number}/head:{bareRef}",
            files!,
            // ReSharper disable once AsyncVoidLambda
            async (provider, result) => await OnPrProcessingResult(provider, result, installation, payload.PullRequest),
            repository);

        await _processQueue.TryQueueProcessItem(processItem);
    }

    private async Task HandlePushEvent(PatchedPushEventPayload payload)
    {
        if (!payload.Ref.Equals(GitBranchRefPrefix + _gitConfiguration.Branch))
            return;

        //if (!_gitConfiguration.RetrieveMapFilesFromDiff)
        //{
            //TODO: Add a way to just render all maps so the instance doesn'T have to be registered as a github app.
        //}

        var enumerable = await CheckFiles(
            payload.Installation.Id,
            payload.Repository.Id,
            payload.Before,
            payload.After);

        var files = enumerable.Select(Path.GetFileName).ToList();

        if (files.Count == 0)
            return;

        var processItem = new ProcessItem(Path.GetFileName(payload.Ref), files!, (_, _) => { });
        await _processQueue.TryQueueProcessItem(processItem);
    }

    private async Task<IEnumerable<string>> CheckFiles(long installationId, long repositoryId, string baseCommit, string headCommit)
    {
        var enumerable = await _githubApiService.GetChangedFilesBetweenCommits(
            new InstallationIdentifier(installationId, repositoryId),
            baseCommit,
            headCommit);

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

    private async Task CreateInitialPrComment(PullRequestEventPayload payload, GitReference baseCommit, List<string> files)
    {
        // ReSharper disable once MethodHasAsyncOverload
        var prComment = _context.PullRequestComment?.Find(
            baseCommit.User.Login,
            baseCommit.Repository.Name,
            payload.PullRequest.Number);

        if (prComment != null)
            return;

        var issue = new IssueIdentifier(
            baseCommit.User.Login,
            baseCommit.Repository.Name,
            payload.PullRequest.Number);

        var commentId = await _githubApiService.CreateCommentWithTemplate(
            new InstallationIdentifier(payload.Installation.Id, payload.Repository.Id),
            issue,
            "generating_map",
            new { files = files.ToArray() });

        SavePrComment(commentId, issue);
    }

    /// <summary>
    /// Retrieves the maps that where rendered and creates a PR comment containing the map images
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <param name="result"></param>
    /// <param name="installation"></param>
    /// <param name="pullRequest"></param>
    /// <remarks>This method gets called out of scope. Which is why _context needs to be set again here</remarks>
    private async Task OnPrProcessingResult(IServiceProvider serviceProvider, MapProcessResult result,
        InstallationIdentifier installation, PullRequest pullRequest)
    {
        var images = new List<(string Name, string Url)>();

        using var scope = serviceProvider.CreateScope();
        _context = scope.ServiceProvider.GetService<Context>()!;
        foreach (var mapId in result.MapIds)
        {
            var map = await FindMapWithGrids(mapId);
            //Grab the largest grid
            map!.Grids.Sort((grid, grid1) => grid1.Extent.CompareTo(grid.Extent));
            var grid = map.Grids[0];

            var url = GetGridImageUrl(mapId, grid.GridId);
            images.Add((map.DisplayName, url));
        }

        // ReSharper disable once MethodHasAsyncOverload
        var prComment = _context.PullRequestComment?.Find(
            pullRequest.Base.User.Login,
            pullRequest.Base.Repository.Name,
            pullRequest.Number);

        var issue = new IssueIdentifier(
            pullRequest.Base.User.Login,
            pullRequest.Base.Repository.Name,
            pullRequest.Number);

        if (prComment == null)
        {
            var commentId = await _githubApiService.CreateCommentWithTemplate(
                installation,
                issue,
                "map_comment",
                new { images = images.ToArray() });

            SavePrComment(commentId, issue);
        }
        else
        {
            await _githubApiService.UpdateCommentWithTemplate(
                installation,
                issue,
                prComment.CommentId,
                "map_comment",
                new { images = images.ToArray() });
        }
    }

    private void SavePrComment(int? commentId, IssueIdentifier issue)
    {
        if (!commentId.HasValue)
            return;

        var prComment = new PullRequestComment
        {
            Owner = issue.Owner,
            Repository = issue.Repository,
            IssueNumber = issue.IssueId,
            CommentId = commentId.Value
        };
        _context.PullRequestComment?.Add(prComment);
        _context.SaveChanges();
    }

    private async Task<Map?> FindMapWithGrids(Guid id)
    {
        return await _context.Map!
            .Include(map => map.Grids)
            .Where(map => map.MapGuid.Equals(id))
            .SingleOrDefaultAsync();
    }

    private string GetGridImageUrl(Guid mapGuid, int gridId)
    {
        //Can't access Url.Action here
        return
            $"{_serverConfiguration.Host.Scheme}://{_serverConfiguration.Host.Host}:{_serverConfiguration.Host.Port}/api/Image/grid/{mapGuid}/{gridId}";
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
