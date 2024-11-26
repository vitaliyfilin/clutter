namespace Clutter.Models;

public class MessageModel
{
    public string? Content { get; set; }
    public bool? IsIncoming { get; set; }
    public DateTime? Timestamp { get; set; } = DateTime.Now;
    public bool? IsSystemMessage { get; set; } // New property for system messages
}
