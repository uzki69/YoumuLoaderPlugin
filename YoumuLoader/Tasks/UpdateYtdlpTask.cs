using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;
using YoumuLoader.Configuration;
using YoumuLoader.Lib;
using YoumuLoader;

/// <summary>
/// Updates yt-dlp.
/// </summary> 
public class UpdateYtdlpTask : IScheduledTask
{
    /// <inheritdoc/>
    public string Name => "Update yt-dlp.";

    /// <inheritdoc/>
    public string Key => "UpdateYtdlp";

    /// <inheritdoc/>
    public string Description => "Updates yt-dlp to the lastest version using yt-dlp -U";

    /// <inheritdoc/>
    public string Category => "Advanced";

    private readonly ILogger<UpdateYtdlpTask> _logger;

    /// <summary>
    /// Constructor.
    /// </summary>
    public UpdateYtdlpTask(ILoggerFactory loggerFactory) {
        _logger = loggerFactory.CreateLogger<UpdateYtdlpTask>();
    }

    /// <inheritdoc/>
    public Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        var _config = Plugin.Instance?.Configuration;

        if (_config == null)
        {
            return new Task(() => _logger.LogError("plugin configuration was null"));
        }

        if (string.IsNullOrWhiteSpace(_config.YtdlpPath))
        {
            return new Task(() => _logger.LogError("Ytdlp not defined"));
        }
        
        return Utils.StartProcess(_config.YtdlpPath, ".", new Options("-U"), logger: _logger) ?? new Task(() => _logger.LogError("Task failed to execute."));
    }

    /// <inheritdoc/>
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers() => [];
}

