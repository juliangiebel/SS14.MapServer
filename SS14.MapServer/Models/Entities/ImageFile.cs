using System.ComponentModel.DataAnnotations;

namespace SS14.MapServer.Models.Entities;
#pragma warning disable CS8618
public class ImageFile
{
    [Key]
    public string Path { get; set; }
    
    public string InternalPath { get; set; }
}