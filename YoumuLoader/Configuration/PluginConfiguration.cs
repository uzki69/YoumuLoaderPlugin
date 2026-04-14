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
        VideoPath = string.Empty;
        MusicPath = string.Empty;
        CookiesPath = string.Empty;
        YtdlpPath = "yt-dlp";
        YtdlpOptions = "--write-sub; --all-subs";
        FileName = "%(uploader)s/%(title)s.%(ext)s";
        Playlist = "%(playlist_uploader|.)s/%(album,playlist)s/%(track_number,playlist_index)s. %(track,title)s.%(ext)s";
        Thumbnail = "%(playlist_uploader|.)s/%(album,playlist)s/cover";
    }

    /// <summary>
    /// Gets or sets a value for the download location.
    /// </summary>
    public string VideoPath { get; set; }

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
    /// Gets or sets a value for downloaded musics path.
    /// </summary>
    public string MusicPath { get; set; }

    /// <summary>
    /// Gets or sets a value for Thumbnail path.
    /// </summary>
    public string Thumbnail { get; set; }
}
