using Octokit;
using SS14.GithubApiHelper.Services;
using SS14.MapServer.Configuration;
using SS14.MapServer.Exceptions;
using ILogger = Serilog.ILogger;

namespace SS14.MapServer.Services.Github;

public sealed class GithubApiService : AbstractGithubApiService
{
    private readonly GithubTemplateService _templateService;

    private readonly ServerConfiguration _serverConfiguration = new();
    private readonly ILogger _log;

    public GithubApiService(IConfiguration configuration, RateLimiterService rateLimiter, GithubTemplateService templateService)
        : base(configuration, rateLimiter)
    {
        _templateService = templateService;
        configuration.Bind(ServerConfiguration.Name, _serverConfiguration);
        _log = Log.ForContext<GithubApiService>();
    }

    public async Task<IEnumerable<string>> GetChangedFilesBetweenCommits(InstallationIdentifier installation, string baseCommit, string headCommit)
    {
        if (!await CheckRateLimit(installation))
            return new List<string>();

        var client = await ClientStore!.GetInstallationClient(installation.InstallationId);
        var compareResult = await client.Repository.Commit.Compare(installation.RepositoryId, baseCommit, headCommit);

        return compareResult.Files.Select(file => file.Filename);
    }

    public async Task<long?> CreateCommentWithTemplate(InstallationIdentifier installation, IssueIdentifier issue, string templateName, object model)
    {
        if (!await CheckRateLimit(installation))
            return null;

        var client = await ClientStore!.GetInstallationClient(installation.InstallationId);
        var body = await _templateService.RenderTemplate(templateName, model, _serverConfiguration.Language);
        var comment = await client.Issue.Comment.Create(issue.Owner, issue.Repository, issue.IssueId, body);

        if (comment != null)
            return comment.Id;

        _log.Error("Failed to create comment on {Repo} {IssueId}",
            issue.Repository,
            $"#{issue.IssueId}");

        return null;
    }

    public async Task UpdateCommentWithTemplate(InstallationIdentifier installation, IssueIdentifier issue, long commentId, string templateName, object model)
    {
        if (!await CheckRateLimit(installation))
            return;

        var client = await ClientStore!.GetInstallationClient(installation.InstallationId);
        var body = await _templateService.RenderTemplate(templateName, model, _serverConfiguration.Language);
        var comment = await client.Issue.Comment.Update(issue.Owner, issue.Repository, commentId, body);

        if (comment != null)
            return;

        _log.Error("Failed to update comment on {Repo} {IssueId}",
            issue.Repository,
            $"#{issue.IssueId}");
    }

    private async Task<bool> CheckRateLimit(InstallationIdentifier installation)
    {
        if (!Configuration.Enabled)
            return false;

        if (!await RateLimiter.Acquire(installation.RepositoryId))
            throw new RateLimitException($"Hit rate limit for repository with id: {installation.RepositoryId}");

        return true;
    }
}
