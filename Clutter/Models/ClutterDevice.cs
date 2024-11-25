using Plugin.BLE.Abstractions.Contracts;

namespace Clutter.Models;

public sealed record ClutterDevice(IDevice Device)
{
    public Guid Id => Device.Id;
    public string Name => Device.Name;

    public override string ToString() => $"Device: {Name} ({Id})";
}

