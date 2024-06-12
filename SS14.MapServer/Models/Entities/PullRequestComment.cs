using Microsoft.Build.Framework;
using Microsoft.EntityFrameworkCore;

namespace SS14.MapServer.Models.Entities;

[PrimaryKey(nameof(Owner), nameof(Repository), nameof(IssueNumber))]
public class PullRequestComment
{
    [Required]
    public string Owner { get; set; } = default!;
    [Required]
    public string Repository { get; set; } = default!;
    [Required]
    public long IssueNumber { get; set; }
    [Required]
    public long CommentId { get; set; }
}
