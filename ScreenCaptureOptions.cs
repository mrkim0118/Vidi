namespace Vidi;

public sealed record ScreenCaptureOptions(string Format, string SaveFolder)
{
    private static readonly string[] SupportedFormats = ["png", "jpg", "tiff"];

    public static ScreenCaptureOptions CreateDefault()
    {
        var pictures = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        var folder = Path.Combine(pictures, "Vidi Captures");
        return new ScreenCaptureOptions("png", folder);
    }

    public string Extension
    {
        get
        {
            var format = Format.Trim().TrimStart('.').ToLowerInvariant();
            return SupportedFormats.Contains(format) ? format : "png";
        }
    }

    public static IReadOnlyList<string> Formats => SupportedFormats;
}
