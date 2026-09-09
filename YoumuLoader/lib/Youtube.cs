using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace YoumuLoader.Lib;

/// <summary>
/// Youtube class.
/// </summary>
/// <remarks>
/// constructor.
/// </remarks>
public class Youtube(YoumuBaseConfiguration configuration, ILoggerFactory loggerFactory) : YoumuBase(configuration, loggerFactory.CreateLogger<Youtube>())
{
    private bool _isPlaylist = false;

    /// <summary>
    /// downloads the youtube video.
    /// </summary>
    /// <returns>Task.</returns>
    protected override async Task DownloadNow()
    {
        ConstructArgs();

        // download video/playlist
        await StartProcess(_config).ConfigureAwait(false);

        // download thumbnail if playlist
        if (_isPlaylist && AttemptDict("yt_thumbnail_name") is string thumbnailOutName)
        {
            if (YRegex.YoutubeGeneratedPlaylist().IsMatch(_config.Link))
            {
                _config.Arguments.Flush();
                AddCookies();
                _config.Arguments.Add("--print", "pre_process:%(album)s", "--skip-download", "--playlist-items", "1", _config.Link);
                string name = string.Empty;
                await StartProcess(_config, (_, args) => name += args.Data).ConfigureAwait(false);

                LogDebug($"album name: {name}");
                thumbnailOutName = thumbnailOutName.Replace("%(playlist)s", name, System.StringComparison.CurrentCulture);
            }

            LogDebug($"thumbnailOutName: |{thumbnailOutName}|");
            _config.Arguments.Flush();

            AddCookies();

            _config.Arguments.Add("--no-overwrites", "--playlist-items", "0", "--write-thumbnail", "--convert-thumbnails", "jpg", "-o", "thumbnail:", "-o", thumbnailOutName, _config.Link);

            LogInfo("Downloading thumbnail");
            await StartProcess(_config).ConfigureAwait(false);
            LogInfo("Thumbnail downloaded");
        }
    }

    private void ConstructArgs()
    {
        AddCookies();

        AddAudio();

        if (!AddOptions("yt_options"))
        {
            AddOptions();
        }

        if (YRegex.YoutubePlaylist().IsMatch(_config.Link))
        {
            if (AttemptDict("as_playlist"))
            {
                if (YRegex.YoutubeGeneratedPlaylist().IsMatch(_config.Link))
                {
                    _config.Arguments.Add("--no-embed-thumbnail");
                }

                if (AttemptDict("as_audio"))
                {
                    // adding track numbers for the files
                    _config.Arguments.Add("--parse-metadata", "%(track_number,playlist_index)s/%(playlist_count)s:%(meta_track)s");
                }

                if (!AddOutputName("yt_playlist_name"))
                {
                    LogDebug("using default output name");
                    if (!AddOutputName())
                    {
                        LogWarning("No playlist name specified");
                    }
                }

                _isPlaylist = true;
            }
            else
            {
                _config.Link = _config.Link.Split("&list=")[0];
                if (!AddOutputName("yt_video_name"))
                {
                    if (!AddOutputName())
                    {
                        LogWarning("No video name specified");
                    }
                }
            }
        }
        else
        {
            if (!AddOutputName("yt_video_name"))
            {
                if (!AddOutputName())
                {
                    LogWarning("no video name specified");
                }
            }
        }

        AddLink();
    }
}
