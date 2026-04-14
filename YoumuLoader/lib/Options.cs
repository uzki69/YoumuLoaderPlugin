using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace YoumuLoader.Lib;

/// <summary>
/// Arguments for passing in process.
/// </summary>
public class Options
{
    private string _options = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="Options"/> class.
    /// </summary>
    public Options()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Options"/> class.
    /// </summary>
    /// <param name="options">options class.</param>
    public Options(string options)
    {
        _options = options;
    }

    /// <summary>
    /// Gets or sets your custom flag.
    /// </summary>
    public int Flag { get; set; } = 0;

    /// <summary>
    /// Adds option to options list.
    /// </summary>
    /// <param name="args">option(s) to add.</param>
    public void Add(params string[] args)
    {
        foreach (var arg in args)
        {
            _options += arg + ' ';
        }
    }

    /// <summary>
    /// remove all options and flags.
    /// </summary>
    public void Flush()
    {
        _options = string.Empty;
        Flag = 0;
    }

    /// <summary>
    /// Parse options to startinfo.
    /// </summary>
    /// <param name="startInfo">process startinfo.</param>
    public void ParseOptionsToProcess(ProcessStartInfo startInfo)
    {
        foreach (var arg in _options.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            startInfo.ArgumentList.Add(arg);
        }
    }

    /// <summary>
    /// Gets last option.
    /// </summary>
    /// <returns>last option.</returns>
    public string Peek()
    {
        return _options.Substring(_options.TrimEnd().LastIndexOf(' '));
    }
}
