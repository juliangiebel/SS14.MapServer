using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SS14.MapServer.Models.Entities;

public class ImageFile
{
    [Key]
    public string Path { get; set; } = default!;

    [Required]
    public string InternalPath { get; set; } = default!;

    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public DateTime LastUpdated { get; set; }
}
