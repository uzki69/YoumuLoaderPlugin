using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
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
public class YoumuController : ControllerBase
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
    /// <returns>status code.</returns>
    [HttpGet("download")]
    public IActionResult YoumuDownload(string video, bool audio)
    {
        LogInfo($"Accepted  Video: {video} Audio: {audio}");

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

        if (string.IsNullOrEmpty(config.DownloadPath))
        {
            LogError("Download path was empty");
            return InternalServerError();
        }

        if (!System.IO.Directory.CreateDirectory(config.DownloadPath).Exists)
        {
            LogError("Download directory could not be created");
        }

        var options = string.Empty;

        if (!string.IsNullOrEmpty(config.CookiesPath))
        {
            if (System.IO.File.Exists(config.CookiesPath))
            {
                options += $"--cookies \"{config.CookiesPath}\" ";
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

        if (!string.IsNullOrEmpty(config.Playlist) && playlistRegex.IsMatch(video))
        {
            outFile = config.Playlist;
            isPlaylist = true;
        }
        else if (!string.IsNullOrEmpty(config.FileName))
        {
            outFile = config.FileName;
        }

        if (!string.IsNullOrEmpty(outFile))
        {
            options += $"-o \"{outFile}\" ";
        }

        options += $"{config.YtdlpOptions} --js-runtimes deno:\"{config.DenoPath}\" \"{video}\"";

        var startInfo = new ProcessStartInfo();

        startInfo.Arguments = options;
        startInfo.FileName = config.YtdlpPath;
        startInfo.WorkingDirectory = config.DownloadPath;
        startInfo.CreateNoWindow = true;
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        startInfo.UseShellExecute = false;

        LogInfo($"Executing: {startInfo.FileName} {startInfo.Arguments}");

        var process = Process.Start(startInfo);

        if (process != null)
        {
            #if DEBUG
            process.OutputDataReceived += (_, args) => LogInfo(args.Data);
            process.ErrorDataReceived += (_, args) => LogDebug(args.Data);
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            #endif
            process.WaitForExit();
            if (process.ExitCode != 0)
            {
                LogError("Yt-dlp exit in failure");
                return InternalServerError();
            }

            LogInfo(isPlaylist ? "Playlist" : "Video" + $" Downloaded: {video}");
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
}
