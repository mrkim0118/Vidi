namespace Vidi;

internal static class VideoFileTypes
{
    public static readonly string[] Extensions =
    [
        ".mp4",
        ".mkv",
        ".avi",
        ".mov",
        ".wmv",
        ".flv",
        ".webm",
        ".mpeg",
        ".mpg",
        ".m4v",
        ".3gp",
        ".ts",
        ".mts",
        ".m2ts",
        ".ogv",
        ".vob",
        ".asf",
        ".rm",
        ".rmvb",
        ".divx",
        ".xvid",
        ".h264",
        ".h265",
        ".hevc"
    ];

    public static string CreateOpenDialogFilter(string videoFilesLabel, string allFilesLabel)
    {
        var patterns = string.Join(';', Extensions.Select(extension => $"*{extension}"));
        return $"{videoFilesLabel}|{patterns}|{allFilesLabel}|*.*";
    }
}
