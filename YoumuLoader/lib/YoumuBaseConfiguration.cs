using System.Collections.Generic;

namespace YoumuLoader.Lib;

/// <summary>
/// Basic Configuration.
/// </summary>
public class YoumuBaseConfiguration
{
    /// <summary>
    /// Gets or sets working dir, where files are downloaded.
    /// </summary>
    public string WorkingDir { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets executable path.
    /// </summary>
    public string Executable { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets yt-dlp used link.
    /// </summary>
    public string Link { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets executable args.
    /// </summary>
    public Options Arguments { get; set; } = new Options();

    /// <summary>
    /// Gets dynamic configuration options.
    /// </summary>
    public Dictionary<string, dynamic> Dynamic { get; } = [];
}
