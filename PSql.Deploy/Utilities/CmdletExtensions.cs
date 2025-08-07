// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy;

/// <summary>
///   Extension methods for <see cref="Cmdlet"/> and <see cref="PSCmdlet"/>.
/// </summary>
internal static class CmdletExtensions
{
    public static bool IsWhatIf(this PSCmdlet cmdlet)
    {
        ArgumentNullException.ThrowIfNull(cmdlet);

        return cmdlet.MyInvocation.BoundParameters.TryGetValue("WhatIf", out var whatIf)
            ? whatIf is (SwitchParameter { IsPresent: true } or true)
            : cmdlet.GetVariableValue("WhatIfPreference") is true;
    }

    public static string GetCurrentPath(this PSCmdlet cmdlet)
    {
        ArgumentNullException.ThrowIfNull(cmdlet);

        return cmdlet.SessionState.Path.CurrentFileSystemLocation.Path;
    }

    public static string GetFullPath(this PSCmdlet cmdlet, string? path = null)
    {
        ArgumentNullException.ThrowIfNull(cmdlet);

        path ??= "";

        return Path.IsPathFullyQualified(path)
            ? path
            : Path.GetFullPath(path, basePath: cmdlet.GetCurrentPath());
    }

    /// <summary>
    ///   Writes the specified message as a host information message.
    /// </summary>
    /// <param name="cmdlet">
    ///   The cmdlet that should write the message.
    /// </param>
    /// <param name="message">
    ///   The message to write.
    /// </param>
    /// <param name="newLine">
    ///   Whether a newline should follow the message.
    /// </param>
    /// <param name="foregroundColor">
    ///   The foreground color to use.
    /// </param>
    /// <param name="backgroundColor">
    ///   The background color to use.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="cmdlet"/> is <see langword="null"/>.
    /// </exception>
    /// <remarks>
    ///   This method is similar to the PowerShell <c>Write-Host</c> cmdlet.
    /// </remarks>
    public static void WriteHost(
        this Cmdlet   cmdlet,
        string?       message,
        bool          newLine         = true,
        ConsoleColor? foregroundColor = null,
        ConsoleColor? backgroundColor = null)
    {
        if (cmdlet is null)
            throw new ArgumentNullException(nameof(cmdlet));

        // Technique learned from PSv5+ Write-Host implementation, which works
        // by sending specially-marked messages to the information stream.
        //
        // https://github.com/PowerShell/PowerShell/blob/v7.5.2/src/Microsoft.PowerShell.Commands.Utility/commands/utility/WriteConsoleCmdlet.cs

        var data = new HostInformationMessage
        {
            Message         = message ?? "",
            NoNewLine       = !newLine,
            ForegroundColor = foregroundColor,
            BackgroundColor = backgroundColor,
        };

        cmdlet.WriteInformation(data, HostTag);
    }

    private static readonly string[] HostTag = ["PSHOST"];
}
