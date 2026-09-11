using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using YoumuLoader.Lib;

/// <summary>
/// Utils class
/// </summary>
public static class Utils
{
  /// <summary>
  /// Starts a new process
  /// </summary>
  public static Task? StartProcess(
      string path,
      string workingDirectory,
      Options? options = null,
      DataReceivedEventHandler? stdout = null,
      DataReceivedEventHandler? stderr = null,
      ILogger? logger = null
      ) 
  {
      ProcessStartInfo info = new ProcessStartInfo
      {
          FileName = path,
          WorkingDirectory = workingDirectory,
          CreateNoWindow = true,
          RedirectStandardOutput = stdout != null || logger != null,
          RedirectStandardError = stderr != null || logger != null,
          UseShellExecute = false
      };

      options?.ParseOptionsToProcess(info);

      var process = Process.Start(info);

      logger?.LogInformation($"Starting process: {info.FileName} {string.Join(" ", info.ArgumentList)} on directory: {info.WorkingDirectory}");


      if (process == null) {
        logger?.LogError($"could not start process {info.FileName}");
        return null;
      }

      if (stdout != null)
      {
        process.OutputDataReceived += stdout;
      }
      else if (logger != null)
      {
        process.OutputDataReceived += (_, arg) => logger.LogInformation(arg.Data);
      }

      if (stderr != null) 
      {
        process.ErrorDataReceived += stderr;
      }
      else if (logger != null)
      {
        process.ErrorDataReceived += (_, arg) => logger.LogError(arg.Data);
      }

      process.BeginOutputReadLine();
      process.BeginErrorReadLine();

    return process.WaitForExitAsync();
  }
}

