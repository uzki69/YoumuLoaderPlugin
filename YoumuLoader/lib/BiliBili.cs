using Microsoft.Extensions.Logging;

namespace YoumuLoader.Lib;

/// <summary>
/// BiliBili downloading video.
/// </summary>
public class BiliBili : YoumuBase
{
    /// <summary>
    /// Constructs base.
    /// </summary>
    public BiliBili(YoumuBaseConfiguration config,  ILoggerFactory loggerFactory)
        : base(config, loggerFactory.CreateLogger<BiliBili>())
    {}

    /// <summary>
    /// downloadNow implementation.
    /// </summary>
    protected override async Task downloadNow()
    {
        addCookies();
        addAudio();

        if (!addOptions("bb_options"))
        {
            addOptions();
        }

        // TODO: maybe create an function for that if's.
        if (!addOutputName("bb_video_name"))
        {
            if (!addOutputName())
            {
                LogWarning("No video name specified");
            }
        }

        addLink();

        StartProcess(_config);
    }
}
