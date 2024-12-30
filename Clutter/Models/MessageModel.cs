namespace Clutter.Models;

public sealed record MessageModel
{
    public string? Name { get; set; }
    public string? Content { get; set; }
    public bool? IsIncoming { get; set; }
    public DateTime? Timestamp { get; set; } = DateTime.Now;
    public bool? IsSystemMessage { get; set; } 
    public bool? IsAvatarVisible { get; set; } 
}
