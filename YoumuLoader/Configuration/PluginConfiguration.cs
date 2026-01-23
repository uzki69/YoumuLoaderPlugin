using MediaBrowser.Model.Plugins;

namespace YoumuLoader.Configuration;

/// <summary>
/// Plugin configuration.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginConfiguration"/> class.
    /// </summary>
    public PluginConfiguration()
    {
        DownloadPath = string.Empty;
        CookiesPath = string.Empty;
        DenoPath = string.Empty;
        YtdlpPath = "yt-dlp";
        YtdlpOptions = "--ignore-errors --no-warnings --write-sub --all-subs";
        FileName = "%(uploader)s/%(title)s/%(title)s.%(ext)s";
        Playlist = "%(playlist)s/%(title)s S01E%(playlist_index)s.%(ext)s";
    }

    /// <summary>
    /// Gets or sets a value for the download location.
    /// </summary>
    public string DownloadPath { get; set; }

    /// <summary>
    /// Gets or sets a value for the cookies path.
    /// </summary>
    public string CookiesPath { get; set; }

    /// <summary>
    /// Gets or sets a value for yt-dlp binary.
    /// </summary>
    public string YtdlpPath { get; set; }

    /// <summary>
    /// Gets or sets a value for yt-dlp args.
    /// </summary>
    public string YtdlpOptions { get; set; }

    /// <summary>
    /// Gets or sets a value for the output filename.
    /// </summary>
    public string FileName { get; set; }

    /// <summary>
    /// Gets or sets a value for the output path and file name for playlists.
    /// </summary>
    public string Playlist { get; set; }

    /// <summary>
    /// Gets or sets a value for Deno binary path.
    /// </summary>
    public string DenoPath { get; set; }
}
