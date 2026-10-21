namespace CursorHub.Core;

// One uniform transform shared by rendering, hit testing and the native alpha mask.
internal readonly record struct InterfaceViewport(float Scale, float Left, float Top)
{
    public const float Width = 1100, Height = 500;
    public static InterfaceViewport Fit(float width, float height)
    {
        float scale = Math.Max(.001f, Math.Min(width / Width, height / Height));
        return new(scale, (width - Width * scale) / 2, (height - Height * scale) / 2);
    }
    public float X(float pixel) => (pixel - Left) / Scale;
    public float Y(float pixel) => (pixel - Top) / Scale;
}
