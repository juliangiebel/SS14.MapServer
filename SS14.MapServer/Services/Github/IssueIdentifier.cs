namespace SS14.MapServer.Services.Github;

public record IssueIdentifier(string Owner, string Repository, int IssueId);
