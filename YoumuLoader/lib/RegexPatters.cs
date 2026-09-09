using System.Text.RegularExpressions;

namespace YoumuLoader.Lib;

/// <summary>
/// Generated regex class
/// </summary>
public partial class YRegex
{
    [GeneratedRegex("^(http.://www\\.youtube\\.com/|http.://m\\.youtube\\.com/|http.://music\\.youtube\\.com/)")]
    public static partial Regex Youtube();

    [GeneratedRegex("list=")]
    public static partial Regex YoutubePlaylist();

    [GeneratedRegex("list=OLAK5uy")]
    public static partial Regex YoutubeGeneratedPlaylist();

    [GeneratedRegex("^(http.://www\\.bilibili\\.com/)")]
    public static partial Regex BiliBili();
}
