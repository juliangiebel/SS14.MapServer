using Octokit;
using SS14.GithubApiHelper.Services;
using SS14.MapServer.Exceptions;

namespace SS14.MapServer.Services;

public sealed class GithubApiService : AbstractGithubApiService
{
    public GithubApiService(IConfiguration configuration, IssueRateLimiterService rateLimiter) 
        : base(configuration, rateLimiter)
    {
    }

    public async Task<IEnumerable<string>> GetChangedFilesBetweenCommits(long installationId, long repositoryId, string baseCommit, string headCommit)
    {
        if (!Configuration.Enabled)
            return new List<string>();

        if (!await RateLimiter.Acquire(repositoryId))
            throw new RateLimitException($"Hit rate limit for repository with id: {repositoryId}");
        
        var client = await ClientStore!.GetInstallationClient(installationId);
        var compareResult = await client.Repository.Commit.Compare(repositoryId, baseCommit, headCommit);
        
        return compareResult.Files.Select(file => file.Filename);
    }
}