using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using J2N.Collections.Generic.Extensions;

namespace YoumuLoader.Lib;

/// <summary>
/// Arguments for passing in process.
/// </summary>
public class Options
{
    private List<string> _options = [];

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
    public Options(string[] options)
    {
        _options = [.. options];
    }

    /// <summary>
    /// Gets or sets your custom flag.
    /// </summary>
    public int Flags { get; set; } = 0;

    /// <summary>
    /// Adds option to options list.
    /// </summary>
    /// <param name="args">option(s) to add.</param>
    public void Add(params string[] args)
    {
        _options.AddRange(args);
    }

    /// <summary>
    /// remove all options and flags.
    /// </summary>
    public void Flush()
    {
        _options = [];
        Flags = 0;
    }

    /// <summary>
    /// Parse options to startinfo.
    /// </summary>
    /// <param name="startInfo">process startinfo.</param>
    public void ParseOptionsToProcess(ProcessStartInfo startInfo)
    {
        foreach (var arg in _options)
        {
            startInfo.ArgumentList.Add(arg);
        }
    }

    /// <summary>
    /// Adds options separated by semicolons.
    /// </summary>
    /// <param name="options">options.</param>
    public void AddOptionsString(string options)
    {
        foreach (var arg in options.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            _options.Add(arg);
        }
    }

    /// <summary>
    /// Gets last option.
    /// </summary>
    /// <returns>last option.</returns>
    public string Peek()
    {
        return _options.Last();
    }
}
