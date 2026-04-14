using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using YoumuLoader.Lib;

namespace YoumuLoader.Controller;

/// <summary>
/// Api Controller.
/// </summary>
[ApiController]
[Authorize(Roles = "Administrator")]
[Route("youmu")]
public partial class YoumuController : ControllerBase // TODO: Task to update ytdlp
{
    private const int IsPlaylistFlag = 1;
    private readonly ILogger<YoumuController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="YoumuController"/> class.
    /// </summary>
    /// <param name="loggerFactory">Instace of <see cref="ILogger"/> interface.</param>
    public YoumuController(
        ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<YoumuController>();
    }

    /// <summary>
    /// Start downloading video.
    /// </summary>
    /// <param name="video">video url.</param>
    /// <param name="audio">is audio.</param>
    /// <param name="playlist">download as playlist.</param>
    /// <returns>status code.</returns>
    [HttpGet("download")]
    public async Task<IActionResult> YoumuDownload(string video, bool audio, bool playlist) // I know this code looks ugly but I'm lazy
    {
        Options options = new Options();

        LogInfo($"Accepted  Video: {video} Audio: {audio} Playlist: {playlist}");

        // check required configuration setup
        var config = Plugin.Instance?.Configuration;
        {
            var (err, status) = CheckNotNullAndRequiredConfiguration(config, video, audio, playlist);
            if (err != null)
            {
                LogError(err);
                return HttpStatus(status);
            }
        }

        // add options for downloading video/playlist/music
        {
            var err = InitContentDownloadOptions(options, config!, video, audio, playlist);
            if (err != null)
            {
                LogError(err);
                return InternalServerError();
            }
        }

        // Download video
        {
            var startInfo = CreateProcessInfo(config!.YtdlpPath, audio ? config.MusicPath : config.VideoPath);

            options.ParseOptionsToProcess(startInfo);

            LogInfo($"Executing: {startInfo.FileName} {string.Join(" ", startInfo.ArgumentList)}");
            await StartProcess(startInfo).ConfigureAwait(false);
            // Assuming yt url is last in options
            LogInfo((options.Flag & IsPlaylistFlag) == IsPlaylistFlag ? "Playlist" : "Video" + $" Downloaded: {options.Peek()}");
        }

        // Download thumbnail
        if ((options.Flag & IsPlaylistFlag) == IsPlaylistFlag && !string.IsNullOrEmpty(config.Thumbnail))
        {
            options.Flush();
            {
                var err = InitThumbnailDownloadOptions(options, config.CookiesPath, config.Thumbnail, video);
                if (err != null)
                {
                    LogError(err);
                    return InternalServerError();
                }
            }

            var startInfo = CreateProcessInfo(config.YtdlpPath, audio ? config.MusicPath : config.VideoPath);
            options.ParseOptionsToProcess(startInfo);
            LogInfo($"Executing Downloading thumbnail process: {startInfo.FileName} {string.Join(" ", startInfo.ArgumentList)}");
            await StartProcess(startInfo).ConfigureAwait(false);
            LogInfo("Thumbnail Downloaded");
        }

        return Ok();
    }

    private ProcessStartInfo CreateProcessInfo(string exePath, string pwd)
    {
        return new ProcessStartInfo
        {
            FileName = exePath,
            WorkingDirectory = pwd,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
    }

    private async Task StartProcess(ProcessStartInfo startInfo)
    {
        var process = Process.Start(startInfo);

        if (process == null)
        {
            LogError("could not start process");
            return;
        }

        process.OutputDataReceived += (_, args) => LogDebug(args.Data);
        process.ErrorDataReceived += (_, args) => LogError(args.Data);

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync().ConfigureAwait(false);

        if (process.ExitCode != 0)
        {
            LogError($"process of {startInfo.FileName} failed with exitcode: {process.ExitCode}");
            return;
        }
    }

    private StatusCodeResult InternalServerError()
    {
        return StatusCode(StatusCodes.Status500InternalServerError);
    }

    private StatusCodeResult HttpStatus(int code)
    {
        return StatusCode(code);
    }

    private void LogError(string? message)
    {
        if (!string.IsNullOrEmpty(message))
        {
            _logger.LogError("{Message}", message);
        }
    }

    private void LogInfo(string? message)
    {
        if (!string.IsNullOrEmpty(message))
        {
            _logger.LogInformation("{Message}", message);
        }
    }

    [Conditional("DEBUG")]
    private void LogDebug(string? message)
    {
        if (!string.IsNullOrEmpty(message))
        {
            _logger.LogDebug("{Message}", message);
        }
    }

    private static (string? ErrMessage, int Status) CheckNotNullAndRequiredConfiguration(Configuration.PluginConfiguration? config, string video, bool audio, bool playlist)
    {
        static (string ErrMessage, int Code) ISError(string msg) => (msg, StatusCodes.Status500InternalServerError);
        static (string ErrMessage, int Code) BRError(string msg) => (msg, StatusCodes.Status400BadRequest);

        if (config == null)
        {
            return ISError("Configuration was null");
        }

        if (string.IsNullOrEmpty(config.YtdlpPath))
        {
            return ISError("Yt-dlp path was null");
        }

        if (!System.IO.File.Exists(config.YtdlpPath))
        {
            return ISError("Yt-dlp file not found");
        }

        if (audio)
        {
            if (string.IsNullOrEmpty(config.MusicPath))
            {
                return ISError("Music path was empty");
            }
            else if (!System.IO.Directory.CreateDirectory(config.MusicPath).Exists)
            {
                return ISError("Music directory could not be created");
            }
        }
        else if (string.IsNullOrEmpty(config.VideoPath))
        {
            return ISError("Video path was empty");
        }
        else if (!System.IO.Directory.CreateDirectory(config.VideoPath).Exists)
        {
            return ISError("Video directory could not be created");
        }

        if (playlist)
        {
            if (string.IsNullOrEmpty(config.Playlist))
            {
                ISError("playlist ytdlp outname not defined");
            }
        }
        else
        {
            if (string.IsNullOrEmpty(config.FileName))
            {
                ISError("filename ytdlp outname not defined");
            }
        }

        {
            var response = VerifyIsValidLink(video);
            if (response != null)
            {
                return BRError(response);
            }
        }

        return (null, 0);
    }

    /// <summary>
    /// Fill options.
    /// </summary>
    /// <param name="options">options.</param>
    /// <param name="config">config.</param>
    /// <param name="link">link.</param>
    /// <param name="audio">is audio.</param>
    /// <param name="playlist">playlist.</param>
    /// <returns>error message.</returns>
    private static string? InitContentDownloadOptions(Options options, Configuration.PluginConfiguration config, string link, bool audio, bool playlist)
    {
        string yt_url = link;

        // cookies
        {
            var res = OptionsFillCookies(config.CookiesPath, options);
            if (res != null)
            {
                return res;
            }
        }

        // audio only
        if (audio)
        {
            options.Add("--extract-audio");
        }

        // appending custom options
        if (!string.IsNullOrEmpty(config.YtdlpOptions))
        {
            options.Add(config.YtdlpOptions);
        }

        // appending ytdlp outname
        options.Add("-o");

        if (PlaylistRegex().IsMatch(link))
        {
            if (playlist)
            {
                options.Add(config.Playlist);
                options.Flag = IsPlaylistFlag;
            }
            else
            {
                yt_url = link.Split("&list=")[0];
                options.Add(config.FileName);
            }
        }
        else
        {
            options.Add(config.FileName);
        }

        // yt url
        options.Add(yt_url);

        return null;
    }

    private static string? InitThumbnailDownloadOptions(Options options, string? cookiesPath, string thumbnailOut, string link)
    {
        {
            var res = OptionsFillCookies(cookiesPath, options);
            if (res != null)
            {
                return res;
            }
        }

        options.Add("--playlist-items 0", "--write-thumbnail", "--convert-thumbnails jpg", "-o", "thumbnail:", "-o", thumbnailOut, link);
        return null;
    }

    private static string? VerifyIsValidLink(string? link)
    {
        if (string.IsNullOrEmpty(link) || !YtRegex().IsMatch(link))
        {
            return $"Video does not match: {link}";
        }

        return null;
    }

    [GeneratedRegex("^(http.://www\\.youtube\\.com/|http.://m\\.youtube\\.com/|http.://music\\.youtube\\.com/)")]
    private static partial Regex YtRegex();

    [GeneratedRegex("list=")]
    private static partial Regex PlaylistRegex();

    private static string? OptionsFillCookies(string? path, Options options)
    {
        if (!string.IsNullOrEmpty(path))
        {
            if (System.IO.File.Exists(path))
            {
                options.Add("--cookies");
                options.Add(path);
            }
            else
            {
                return "Cookie file does not exist";
            }
        }

        return null;
    }
}
