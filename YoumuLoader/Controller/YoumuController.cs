using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ICU4N.Text;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.IO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Logging;

namespace YoumuLoader.Controller;

/// <summary>
/// Api Controller.
/// </summary>
[ApiController]
[Authorize(Roles = "Administrator")]
[Route("youmu")]
public partial class YoumuController : ControllerBase // TODO: Task to update ytdlp
{
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
        LogInfo($"Accepted  Video: {video} Audio: {audio} Playlist: {playlist}");

        var config = Plugin.Instance?.Configuration;

        if (config == null)
        {
            LogError("Configuration was null");
            return InternalServerError();
        }

        if (string.IsNullOrEmpty(config.YtdlpPath))
        {
            LogError("Yt-dlp path was null");
            return InternalServerError();
        }

        if (!System.IO.File.Exists(config.YtdlpPath))
        {
            LogError("Yt-dlp file not found");
            return InternalServerError();
        }

        if (string.IsNullOrEmpty(config.VideoPath) && !audio)
        {
            LogError("Video path was empty");
            return InternalServerError();
        }

        if (string.IsNullOrEmpty(config.MusicPath) && audio)
        {
            LogError("Music path was empty");
            return InternalServerError();
        }

        if (!System.IO.Directory.CreateDirectory(config.VideoPath).Exists)
        {
            LogError("Video directory could not be created");
            return InternalServerError();
        }

        if (!System.IO.Directory.CreateDirectory(config.MusicPath).Exists)
        {
            LogError("Music directory could not be created");
            return InternalServerError();
        }

        var options = string.Empty;

        if (!string.IsNullOrEmpty(config.CookiesPath))
        {
            if (System.IO.File.Exists(config.CookiesPath))
            {
                options += $"--cookies {config.CookiesPath} ";
            }
            else
            {
                LogError("Cookie file does not exist");
            }
        }

        if (audio == true)
        {
            options += "--extract-audio ";
        }

        var ytRegex = YtRegex();

        if (string.IsNullOrEmpty(video) || !ytRegex.IsMatch(video))
        {
            LogError($"Video could not pass: {video}");
            return BadRequest();
        }

        var playlistRegex = PlaylistRegex();

        var outFile = config.FileName;
        bool isPlaylist = false;
        var youtube_url = video;

        if (!string.IsNullOrEmpty(config.Playlist) && playlistRegex.IsMatch(video))
        {
            if (playlist)
            {
                if (YoutubeAlbumRegex().IsMatch(video) && !string.IsNullOrEmpty(config.Album))
                {
                    outFile = config.Album;
                }
                else
                {
                outFile = config.Playlist;
                }

                isPlaylist = true;
            }
            else
            {
                youtube_url = video.Split("&list=")[0];
            }
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = config.YtdlpPath,
            WorkingDirectory = audio ? config.MusicPath : config.VideoPath,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        if (!string.IsNullOrEmpty(outFile))
        {
            startInfo.ArgumentList.Add("-o");
            startInfo.ArgumentList.Add(outFile);
        }

        options += $"{config.YtdlpOptions} {youtube_url}";
        ParseArgs(startInfo, options);

        LogInfo($"Executing: {startInfo.FileName} {string.Join(" ", startInfo.ArgumentList)}");

        var process = Process.Start(startInfo);

        if (process != null)
        {
            process.OutputDataReceived += (_, args) => LogDebug(args.Data);
            process.ErrorDataReceived += (_, args) => LogError(args.Data);

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync().ConfigureAwait(false);

            if (process.ExitCode != 0)
            {
                LogError("Yt-dlp exit in failure trying download video");
                return InternalServerError();
            }

            LogInfo(isPlaylist ? "Playlist" : "Video" + $" Downloaded: {youtube_url}");

            if (isPlaylist && !string.IsNullOrEmpty(outFile) && !string.IsNullOrEmpty(config.CookiesPath) && !string.IsNullOrEmpty(config.Thumbnail))
            {
                options = $"--cookies {config.CookiesPath} --playlist-items 1 --write-thumbnail --convert-thumbnails jpg --skip-download -o {config.Thumbnail} {youtube_url}";

                var thumb_startinfo = new ProcessStartInfo
                {
                    FileName = config.YtdlpPath,
                    WorkingDirectory = audio ? config.MusicPath : config.VideoPath,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                };

                ParseArgs(thumb_startinfo, options);

                LogInfo($"Executing thumb download: {thumb_startinfo.FileName} {string.Join(" ", thumb_startinfo.ArgumentList)}");

                var processThumbDownload = Process.Start(thumb_startinfo);

                if (processThumbDownload == null)
                {
                    return InternalServerError();
                }

                processThumbDownload.OutputDataReceived += (_, args) => LogDebug(args.Data);
                processThumbDownload.ErrorDataReceived += (_, args) => LogError(args.Data);

                process.BeginOutputReadLine();
                processThumbDownload.BeginErrorReadLine();

                await processThumbDownload.WaitForExitAsync().ConfigureAwait(false);

                if (processThumbDownload.ExitCode != 0)
                {
                    LogError("yt-dlp exit in failure trying download the thumbnail");
                }
            }

            return Ok();
        }

        return InternalServerError();
    }

    private StatusCodeResult InternalServerError()
    {
        return StatusCode(StatusCodes.Status500InternalServerError);
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

    private static void ParseArgs(ProcessStartInfo startInfo, string args)
    {
        foreach (var arg in args.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            startInfo.ArgumentList.Add(arg);
        }
    }

    [GeneratedRegex("^(http.://www\\.youtube\\.com/|http.://m\\.youtube\\.com/|http.://music\\.youtube\\.com/)")]
    private static partial Regex YtRegex();

    [GeneratedRegex("list=")]
    private static partial Regex PlaylistRegex();

    [GeneratedRegex("list=OLAK5uy")]
    private static partial Regex YoutubeAlbumRegex();
}
