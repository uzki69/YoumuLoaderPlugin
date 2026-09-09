using System.Collections.Generic;

namespace YoumuLoader.Lib;

///<summary>
///Basic Configuration
///</summary>
public class YoumuBaseConfiguration
{
    /// <summary>
    /// Working dir.
    /// </summary>
    public string workingDir = string.Empty;
    /// <summary>
    /// Executable path.
    /// </summary>
    public string executable = string.Empty;

    /// <summary>
    /// yt-dlp used link
    /// </summary>
    public string link = string.Empty;

    /// <summary>
    /// executable args
    /// </summary>
    public Options arguments = new Options();

    /// <summary>
    /// dynamic configuration options
    /// </summary>
    public Dictionary<string, dynamic> dynamic = new Dictionary<string, dynamic>();
}

