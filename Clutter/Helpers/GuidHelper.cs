namespace Clutter.Helpers;

public static class GuidHelper
{
    public static string GuidToMacAddress(Guid guid)
    {
        var bytes = guid.ToByteArray();
        var macBytes = new byte[6];
        Array.Copy(bytes, 10, macBytes, 0, 6);
        return string.Join(":", macBytes.Select(b => b.ToString("X2")));
    }
}