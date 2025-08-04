// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace PSql.Deploy;

/// <summary>
///   Cached information about the current operating system process.
/// </summary>
internal sealed class ProcessInfo
{
    /// <summary>
    ///   Gets the singleton <see cref="ProcessInfo"/> instance.
    /// </summary>
    public static ProcessInfo Instance { get; } = new();

    private ProcessInfo()
    {
        var process = Process.GetCurrentProcess();

        ProcessId   = process.Id;
        ProcessName = process.ProcessName;
    }

    /// <inheritdoc cref="Environment.MachineName"/>
    public string MachineName = Environment.MachineName;

    /// <inheritdoc cref="Environment.ProcessorCount"/>
    public int ProcessorCount = Environment.ProcessorCount;

    /// <inheritdoc cref="RuntimeInformation.OSDescription"/>
    public string OSDescription = RuntimeInformation.OSDescription;

    /// <inheritdoc cref="RuntimeInformation.OSArchitecture"/>
    public Architecture OSArchitecture = RuntimeInformation.OSArchitecture;

    /// <inheritdoc cref="Environment.UserName"/>
    public string UserName = Environment.UserName;

    /// <inheritdoc cref="RuntimeInformation.FrameworkDescription"/>
    public string FrameworkDescription = RuntimeInformation.FrameworkDescription;

    /// <inheritdoc cref="Process.Id"/>
    public int ProcessId { get; }

    /// <inheritdoc cref="Process.ProcessName"/>
    public string ProcessName { get; }

    /// <inheritdoc cref="RuntimeInformation.ProcessArchitecture"/>
    public Architecture ProcessArchitecture = RuntimeInformation.ProcessArchitecture;
}
