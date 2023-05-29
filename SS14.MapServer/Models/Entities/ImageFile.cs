using System.ComponentModel.DataAnnotations;

namespace SS14.MapServer.Models.Entities;

public class ImageFile
{
    [Key]
    public string Path { get; set; } = default!;

    public string InternalPath { get; set; } = default!;
}