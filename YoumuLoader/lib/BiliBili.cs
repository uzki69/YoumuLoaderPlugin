using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace YoumuLoader.Lib;

/// <summary>
/// BiliBili downloading video.
/// </summary>
public class BiliBili : YoumuBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="YoumuBase"/> class.
    /// </summary>
    public BiliBili(YoumuBaseConfiguration config,  ILoggerFactory loggerFactory)
        : base(config, loggerFactory.CreateLogger<BiliBili>())
    {}

    /// <summary>
    /// downloadNow implementation.
    /// </summary>
    /// <returns></returns>
    protected override async Task DownloadNow()
    {
        AddCookies();
        AddAudio();

        if (!AddOptions("bb_options"))
        {
            AddOptions();
        }

        // TODO: maybe create an function for that if's.
        if (!AddOutputName("bb_video_name"))
        {
            if (!AddOutputName())
            {
                LogWarning("No video name specified");
            }
        }

        AddLink();

        await StartProcess(_config).ConfigureAwait(false);
    }
}
