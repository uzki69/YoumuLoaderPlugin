using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace YoumuLoader.Lib;

/// <summary>
/// Base class for each resource option of ytdlp.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="YoumuBase"/> class.
/// </remarks>
/// <param name="config">Instace of <see cref="YoumuBaseConfiguration"/> interface.</param>
/// <param name="logger">Instace of <see cref="ILogger"/> interface.</param>
public class YoumuBase(YoumuBaseConfiguration config, ILogger logger)
{
    /// <summary>
    /// default logger.
    /// </summary>
    protected readonly ILogger _logger = logger;

    /// <summary>
    /// Options of the lib view.
    /// </summary>
    protected readonly YoumuBaseConfiguration _config = config;

    /// <summary>
    /// Checks and starts the downloading.
    /// </summary>
    /// <returns>Task for downloading.</returns>
    public Task? Download()
    {
        if (!AllChecks())
        {
            return null;
        }

        return DownloadNow();
    }

    /// <summary>
    /// Downloading function interface.
    /// When implementing this function you probably
    /// should use: addCookies, addAudio, addOptions, addOutputName, addLink.
    /// </summary>
    /// <returns>Task for downloading.</returns>
    protected virtual async Task DownloadNow()
    {
        AddCookies();
        AddAudio();
        AddOptions();
        AddOutputName();
        AddLink();
        await StartProcess(_config).ConfigureAwait(false);
    }

    /// <summary>
    /// Logs errors.
    /// </summary>
    /// <param name="message">message.</param>
    protected void LogError(string? message)
    {
        if (!string.IsNullOrEmpty(message))
        {
            _logger.LogError("{Message}", message);
        }
    }

    /// <summary>
    /// Logs Info.
    /// </summary>
    /// <param name="message">message.</param>
    protected void LogInfo(string? message)
    {
       if (!string.IsNullOrEmpty(message))
       {
            _logger.LogInformation(message);
       }
    }

    /// <summary>
    /// Logs Debug info.
    /// </summary>
    /// <param name="message">message.</param>
    [Conditional("DEBUG")]
    protected void LogDebug(string? message)
    {
        if (!string.IsNullOrEmpty(message))
        {
           _logger.LogDebug(message);
        }
    }

    /// <summary>
    /// Logs Warnings.
    /// </summary>
    /// <param name="message">message.</param>
    protected void LogWarning(string? message)
    {
        if (!string.IsNullOrEmpty(message))
        {
            _logger.LogWarning(message);
        }
    }

    /// <summary>
    /// executes a process.
    /// </summary>
    /// <param name="configuration">Configuration for downloading.</param>
    /// <param name="stdout">if needs to read stdout in another manner.</param>
    /// <returns>Task.</returns>
    protected async Task StartProcess(
        YoumuBaseConfiguration configuration,
        DataReceivedEventHandler? stdout = null )
    {
        ProcessStartInfo info = new ProcessStartInfo
        {
            FileName = configuration.Executable,
            WorkingDirectory = configuration.WorkingDir,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        configuration.Arguments?.ParseOptionsToProcess(info);

        LogInfo($"Starting process: {info.FileName} {string.Join(" ", info.ArgumentList)} on directory: {info.WorkingDirectory}");

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

        LogInfo($"Video Downloaded: {_config.Link}");
    }

    /// <summary>
    /// Checks if it has the basics for downloading.
    /// </summary>
    private bool AllChecks()
    {
        if (string.IsNullOrEmpty(_config.Executable))
        {
            LogError("_config.executable was empty");
            return false;
        }

        // case command does not need a directory
        if (AttemptDict("no_directory") == null)
        {
            if (string.IsNullOrEmpty(_config.WorkingDir))
            {
                LogError("_config.workingDir was empty");
                return false;
            }
        }

        // case command does not need a link.
        if (AttemptDict("no_link") == null)
        { 
          if (string.IsNullOrEmpty(_config.Link))
          {
              LogError("_config.link was empty");
              return false;
          }
        }

        return true;
    }

    /// <summary>
    /// Add cookies to options.
    /// </summary>
    /// <param name="name">name.</param>
    protected void AddCookies(string name = "cookies")
    {
        string? cookies = AttemptDict(name);
        if (cookies == null)
        {
            LogInfo("No cookies available");
            return;
        }

        if (!string.IsNullOrEmpty(cookies))
        {
          if (System.IO.File.Exists(cookies))
          {
            _config.Arguments.Add("--cookies", cookies);
          }
        }
        else
        {
            LogError($"Cookies was not found at: [{cookies}]");
            return;
        }
    }

    /// <summary>
    /// Add audio only to options.
    /// </summary>
    /// <returns>true if it added.</returns>
    protected bool AddAudio()
    {
        if (AttemptDict("as_audio"))
        {
            // option to download as audio file
            LogDebug($"as_audio == true, value: ${AttemptDict("as_audio")}");
            _config.Arguments.Add("--extract-audio");
            _config.Arguments.Add("--convert-subs", "lrc");
            _config.Arguments.Add("--embed-metadata");
            return true;
        }

        return false;
    }

    /// <summary>
    /// Add options to _config.arguments.
    /// </summary>
    /// <param name="name">options name.</param>
    /// <returns>true if it added.</returns>
    protected bool AddOptions(string name = "options")
    {
        if (AttemptDict(name) is string options)
        {
            _config.Arguments.AddOptionsString(options);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Add output name to _config.arguments.
    /// </summary>
    /// <param name="name">options name.</param>
    /// <returns>true if it added.</returns>
    protected bool AddOutputName(string name = "output")
    {
        if (AttemptDict(name) is string output)
        {
            _config.Arguments.Add("-o", output);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Add link to _config.arguments.
    /// </summary>
    protected void AddLink()
    {
        _config.Arguments.Add(_config.Link);
    }

    /// <summary>
    /// Attempt to get a value from dict.
    /// </summary>
    protected dynamic? AttemptDict(string key)
    {
      return _config.Dynamic.TryGetValue(key, out var v) ? v : null;
    }
}
