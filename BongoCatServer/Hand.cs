// ReSharper disable InconsistentNaming
namespace BongoCatServer;

public enum Hand
{
    right,
    left,
}

public static class HandExtensions
{
    public static Hand Parse(ReadOnlySpan<char> data) => Enum.Parse<Hand>(data);

    public static string Serialize(this Hand hand) => hand switch
    {
        Hand.right => "right",
        Hand.left => "left",
    };
}
