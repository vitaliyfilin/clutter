namespace Clutter.Models;

public sealed class MessageModel
{
    public string? Content { get; set; }
    public bool IsIncoming { get; set; }
    public string CurrentTime { get; set; } = DateTime.Now.ToString("HH:mm");
}