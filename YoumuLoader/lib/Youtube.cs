using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Diagnostics;

namespace YoumuLoader.Lib;

/// <summary>
/// Youtube class
/// </summary>
public class Youtube : YoumuBase {

    /// <summary>
    /// constructor
    /// </summary>
    public Youtube(YoumuBaseConfiguration configuration, ILoggerFactory loggerFactory )
        : base(configuration, loggerFactory.CreateLogger<Youtube>())
    {}

    private bool _isPlaylist = false;

    /// <summary>
    /// downloads the youtube video
    /// </summary>
    protected override async Task downloadNow()
    {
        constructArgs();

        // download video/playlist
        await StartProcess(_config).ConfigureAwait(false);

        // download thumbnail if playlist
        if (_isPlaylist && _config.dynamic["yt_thumbnail_name"] is string thumbnailOutName)
        {
            if (YRegex.YoutubeGeneratedPlaylist().IsMatch(_config.link))
            {
                _config.arguments.Flush();
                addCookies();
                _config.arguments.Add("--print", "pre_process:%(album)s", "--skip-download", "--playlist-items", "1", _config.link);
                string name = string.Empty;
                await StartProcess(_config, (_, args) => {name += args.Data;}).ConfigureAwait(false);

                LogDebug($"album name: {name}");
                thumbnailOutName = thumbnailOutName.Replace("%(playlist)s", name, System.StringComparison.CurrentCulture);
            }
            LogDebug($"thumbnailOutName: |{thumbnailOutName}|");
            _config.arguments.Flush();

            addCookies();

            _config.arguments.Add("--no-overwrites", "--playlist-items", "0", "--write-thumbnail", "--convert-thumbnails", "jpg", "-o", "thumbnail:", "-o", thumbnailOutName, _config.link);

            LogInfo("Downloading thumbnail");
            await StartProcess(_config).ConfigureAwait(false);
            LogInfo("Thumbnail downloaded");
        }
    }

    private void constructArgs()
    {
        addCookies();

        addAudio();

        if (!addOptions("yt_options"))
        {
            addOptions();
        }

        if (YRegex.YoutubePlaylist().IsMatch(_config.link))
        {
            if (_config.dynamic["as_playlist"])
            {
                if (YRegex.YoutubeGeneratedPlaylist().IsMatch(_config.link))
                {
                    _config.arguments.Add("--no-embed-thumbnail");
                }

                if (_config.dynamic["as_audio"])
                {
                    // adding track numbers for the files
                    _config.arguments.Add("--parse-metadata", "%(track_number,playlist_index)s/%(playlist_count)s:%(meta_track)s");
                }


                if (!addOutputName("yt_playlist_name"))
                {
                    LogDebug("using default output name");
                    if (!addOutputName())
                    {
                        LogWarning("No playlist name specified");
                    }
                }

                _isPlaylist = true;
            }
            else
            {
                _config.link = _config.link.Split("&list=")[0];
                if (!addOutputName("yt_video_name"))
                {
                    if (!addOutputName())
                    {
                        LogWarning("No video name specified");
                    }
                }
            }
        }
        else
        {
            if (!addOutputName("yt_video_name"))
            {
                if (!addOutputName())
                {
                    LogWarning("no video name specified");
                }
            }
        }

        addLink();
    }
}
