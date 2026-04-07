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
public class YoumuController : ControllerBase // TODO: Task to update ytdlp
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
    public IActionResult YoumuDownload(string video, bool audio, bool playlist) // I know this code looks ugly but IDK
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

        var ytRegex = new Regex("^https://www\\.youtube\\.com/");

        if (string.IsNullOrEmpty(video) || !ytRegex.IsMatch(video) )
        {
            LogError($"Video could not pass: {video}");
            return BadRequest();
        }

        var playlistRegex = new Regex("list=");

        var outFile = string.Empty;
        bool isPlaylist = false;

        if (!string.IsNullOrEmpty(config.Playlist) && playlistRegex.IsMatch(video) && playlist)
        {
            outFile = config.Playlist;
            isPlaylist = true;
        }
        else if (!string.IsNullOrEmpty(config.FileName))
        {
            outFile = config.FileName;
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

        options += $"{config.YtdlpOptions} {video}";
        ParseArgs(startInfo, options);

        LogInfo($"Executing: {startInfo.FileName} {string.Join(" ", startInfo.ArgumentList)}");

        var process = Process.Start(startInfo);

        if (process != null)
        {
            #if DEBUG
            process.OutputDataReceived += (_, args) => LogDebug(args.Data);
            process.BeginOutputReadLine();
            #endif

            process.ErrorDataReceived += (_, args) => LogError(args.Data);
            process.BeginErrorReadLine();

            process.WaitForExit();
            if (process.ExitCode != 0)
            {
                LogError("Yt-dlp exit in failure trying download video");
                return InternalServerError();
            }

            LogInfo(isPlaylist ? "Playlist" : "Video" + $" Downloaded: {video}");

            if (isPlaylist && !string.IsNullOrEmpty(outFile))
            {
                options = $"--cookies {config.CookiesPath} --playlist-items 1 --write-thumbnail --convert-thumbnails jpg --skip-download -o %(playlist)s/cover {video}";

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

                processThumbDownload.ErrorDataReceived += (_, args) => LogError(args.Data);
                processThumbDownload.BeginErrorReadLine();

                processThumbDownload.WaitForExit();
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
}
