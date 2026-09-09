using YoumuLoader.Lib;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Diagnostics;
/// <summary>
/// Abstract class for each resource option of ytdlp.
/// </summary>
public abstract class YoumuBase {

    /// <summary>
    /// Options of the lib view.
    /// </summary>
    protected YoumuBaseConfiguration _config;

    /// <summary>
    /// default logger.
    /// </summary>
    protected readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="YoumuBase"/> class.
    /// </summary>
    /// <param name="config">Instace of <see cref="YoumuBaseConfiguration"/> interface.</param>
    /// <param name="logger">Instace of <see cref="ILogger"/> interface.</param>
    public YoumuBase(YoumuBaseConfiguration config, ILogger logger)
    {
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Checks and starts the downloading.
    /// </summary>
    public Task? download()
    {
        if (!allChecks()) return null;
        return downloadNow();
    }

    /// <summary>
    /// Downloading function interface.
    /// When implementing this function you probably
    /// should use: addCookies, addAudio, addOptions, addOutputName, addLink.
    /// </summary>
    protected abstract Task downloadNow()
    {
        addCookies();
        addAudio();
        addOptions();
        addOutputName();
        addLink();
        StartProcess(_config);
    }

    /// <summary>
    /// Logs errors
    /// </summary>
    protected void LogError(string ?message)
    {
        if (!string.IsNullOrEmpty(message))
        {
            _logger.LogError("{Message}", message);
        }
    }

    /// <summary>
    /// Logs Info.
    /// </summary>
    protected void LogInfo(string ?message)
    {
       if (!string.IsNullOrEmpty(message))
       {
            _logger.LogInformation(message);
       }
    }

    /// <summary>
    /// Logs Debug info.
    /// </summary>
    [Conditional("DEBUG")]
    protected void LogDebug(string ?message)
    {
        if (!string.IsNullOrEmpty(message))
        {
           _logger.LogDebug(message);
        }
    }

    /// <summary>
    /// Logs Warnings.
    /// </summary>
    protected void LogWarning(string ?message)
    {
        if (!string.IsNullOrEmpty(message))
        {
            _logger.LogWarning(message);
        }
    }

    /// <summary>
    /// executes a process
    /// </summary>
    /// <param name="configuration">Configuration for downloading</param>
    /// <param name="stdout">if needs to read stdout in another manner</param>
    protected async Task StartProcess(YoumuBaseConfiguration configuration,
            DataReceivedEventHandler? stdout = null )
    {


        ProcessStartInfo info = new ProcessStartInfo
        {
            FileName = configuration.executable,
            WorkingDirectory = configuration.workingDir,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        configuration.arguments?.ParseOptionsToProcess(info);

        var process = Process.Start(info);

        if (process == null)
        {
            LogError($"could not start process {info.FileName}");
            return;
        }

        if (stdout == null)
        {
            process.OutputDataReceived += (_, args) => LogDebug(args.Data);
        }

        process.ErrorDataReceived += (_, args) => LogError(args.Data);

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync().ConfigureAwait(false);

        if (process.ExitCode != 0)
        {
            LogError($"process of {info.FileName} failed with exitcode: {process.ExitCode}");
            return;
        }
    }

    private bool allChecks() {
        if (string.IsNullOrEmpty(_config.executable))
        {
            LogError("_config.executable was empty");
            return false;
        }

        if (!_config.dynamic["no_directory"])
        {
            if (string.IsNullOrEmpty(_config.workingDir))
            {
                LogError("_config.workingDir was empty");
                return false;
            }
        }

        if (!_config.dynamic["no_link"])
        {
            if (string.IsNullOrEmpty(_config.link))
            {
                LogError("_config.link was empty");
                return false;
            }
        }
        return true;
    }


    /// <summary>
    /// Add cookies to options
    /// </summary>
    protected void addCookies(string name = "cookies")
    {
        string ?cookies = _config.dynamic[name];
        if (cookies == null)
        {
            LogInfo("No cookies available");
            return;
        }

        if (!string.IsNullOrEmpty(cookies))
        {
          if (System.IO.File.Exists(cookies))
          {
            _config.arguments.Add("--cookies", cookies);
          }
        }
        else
        {
            LogError($"Cookies was not found at: [{cookies}]");
            return;
        }
    }

    /// <summary>
    /// Add audio only to options
    /// </summary>
    /// <returns>true if it added</returns>
    protected bool addAudio() {
        if (_config.dynamic["as_audio"])
        {
            // option to download as audio file
            LogDebug($"as_audio == true, value: ${_config.dynamic["isAudio"]}");
            _config.arguments.Add("--extract-audio");
            _config.arguments.Add("--convert-subs", "lrc");
            _config.arguments.Add("--embed-metadata");
            return true;
        }

        return false;
    }

    /// <summary>
    /// Add options to _config.arguments
    /// </summary>
    /// <param name="name">options name</param>
    /// <returns>true if it added</returns>
    protected bool addOptions(string name = "options") {
        if (_config.dynamic[name] is string options) {
            _config.arguments.AddOptionsString(options);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Add output name to _config.arguments
    /// </summary>
    /// <param name="name">options name</param>
    /// <returns>true if it added</returns>
    protected bool addOutputName(string name = "output")
    {
        if (_config.dynamic[name] is string output)
        {
            _config.arguments.Add("-o", output);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Add link to _config.arguments.
    /// </summary>
    protected void addLink()
    {
        _config.arguments.Add(_config.link);
    }
}
