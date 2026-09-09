using System.Diagnostics;
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
/// <remarks>
/// Initializes a new instance of the <see cref="YoumuController"/> class.
/// </remarks>
/// <param name="loggerFactory">Instace of <see cref="ILogger"/> interface.</param>
[ApiController]
[Authorize(Roles = "Administrator")]
[Route("youmu")]
public partial class YoumuController(
    ILoggerFactory loggerFactory) : ControllerBase // TODO: Task to update ytdlp
{
    private readonly ILogger<YoumuController> _logger = loggerFactory.CreateLogger<YoumuController>();
    private readonly ILoggerFactory _loggerFactory = loggerFactory;

    /// <summary>
    /// Start downloading video.
    /// </summary>
    /// <param name="video">video url.</param>
    /// <param name="audio">is audio.</param>
    /// <param name="playlist">download as playlist.</param>
    /// <returns>status code.</returns>
    [HttpGet("download")]
    public async Task<IActionResult> YoumuDownload(string video, bool audio, bool playlist)
    {
        LogInfo($"Accepted  Video: {video} Audio: {audio} Playlist: {playlist}");

        // check required configuration setup
        var config = Plugin.Instance?.Configuration;
        var yConfig = new YoumuBaseConfiguration();
        YoumuBase yDownloader;

        if (config == null)
        {
            return BadRequest();
        }

        yConfig.WorkingDir = audio ? config.MusicPath : config.VideoPath;

        if (string.IsNullOrEmpty(yConfig.WorkingDir))
        {
            return BadRequest();
        }

        yConfig.Link = video;
        yConfig.Executable = config.YtdlpPath;
       
        {
          AddStringToDynamic("cookies", config.CookiesPath, yConfig);
          AddStringToDynamic("options", config.YtdlpOptions, yConfig);
          yConfig.Dynamic["as_playlist"] = playlist;
          yConfig.Dynamic["as_audio"] = audio;
          AddStringToDynamic("output", config.FileName, yConfig);
        }  

        if (YRegex.Youtube().IsMatch(video))
        {
          AddStringToDynamic("yt_thumbnail_name", config.Thumbnail, yConfig);
          AddStringToDynamic("yt_playlist_name", config.Playlist, yConfig);
          yDownloader = new Youtube(yConfig, _loggerFactory);
        }
        else if (YRegex.BiliBili().IsMatch(video))
        {
            yDownloader = new BiliBili(yConfig, _loggerFactory);
        }
        else
        {
            yDownloader = new YoumuBase(yConfig, _logger);
        }

        if (yDownloader.Download() is Task t)
        {
            await t.ConfigureAwait(false);
            return Ok();
        }

        return BadRequest();
    }

    private void AddStringToDynamic(string key, string? value, YoumuBaseConfiguration config)
    {
      if (!string.IsNullOrEmpty(value))
      {
        config.Dynamic[key] = value;
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
}
