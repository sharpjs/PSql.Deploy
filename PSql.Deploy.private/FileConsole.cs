// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

using System.Collections;

namespace PSql.Deploy;

internal sealed class FileConsole : IConsole, IDisposable
{
    private readonly StreamWriter _writer;

    public FileConsole(string path)
    {
        _writer = new StreamWriter(path);
    }

    /// <inheritdoc/>
    public void WriteObject(object obj, bool enumerate)
    {
        if (!enumerate || obj is not IEnumerable collection)
            WriteObject(obj);
        else
            foreach (var item in collection)
                if (item is not null)
                    WriteObject(item, enumerate: true);
    }

    /// <inheritdoc/>
    public void WriteObject(object obj)
        => _writer.WriteLine(obj);

    /// <inheritdoc/>
    public void WriteError(ErrorRecord record)
        => _writer.WriteLine("ERROR: " + record.ToString());

    /// <inheritdoc/>
    public void WriteWarning(string text)
        => _writer.WriteLine("WARNING: " + text);

    /// <inheritdoc/>
    public void WriteVerbose(string text)
        => _writer.WriteLine(text);

    /// <inheritdoc/>
    public void WriteDebug(string text)
        => _writer.WriteLine(text);

    /// <inheritdoc/>
    public void WriteHost(
        string        text,
        bool          newLine         = true,
        ConsoleColor? foregroundColor = null,
        ConsoleColor? backgroundColor = null)
    {
        if (newLine)
            _writer.WriteLine(text);
        else
            _writer.Write(text);
    }

    /// <inheritdoc/>
    public void WriteInformation(InformationRecord record)
        => _writer.WriteLine(record);

    /// <inheritdoc/>
    public void WriteInformation(object data, string[] tags)
    {
        _writer.Write(data);

        foreach (var tag in tags)
        {
            _writer.Write(' ');
            _writer.Write(tag);
        }

        _writer.WriteLine();
    }

    /// <inheritdoc/>
    public void WriteProgress(ProgressRecord record)
        => _writer.WriteLine(record);

    /// <inheritdoc/>
    public void WriteCommandDetail(string text)
        => _writer.WriteLine(text);

    /// <inheritdoc/>
    public bool ShouldContinue(string query, string caption)
        =>  true;

    /// <inheritdoc/>
    public bool ShouldContinue(string query, string caption, ref bool yesToAll, ref bool noToAll)
        =>  true;

    /// <inheritdoc/>
    public bool ShouldContinue(string query, string caption, bool hasSecurityImpact, ref bool yesToAll, ref bool noToAll)
        =>  true;

    /// <inheritdoc/>
    public bool ShouldProcess(string target)
        =>  true;

    /// <inheritdoc/>
    public bool ShouldProcess(string target, string action)
        =>  true;

    /// <inheritdoc/>
    public bool ShouldProcess(string description, string query, string caption)
        =>  true;

    /// <inheritdoc/>
    public bool ShouldProcess(string description, string query, string caption, out ShouldProcessReason reason)
        => (r: true, reason = default).r;

    /// <inheritdoc/>
    public void ThrowTerminatingError(ErrorRecord record)
        => throw new NotSupportedException() { Data = { ["ErrorRecord"] = record } };

    /// <inheritdoc/>
    public void Dispose()
        => _writer.Dispose();
}
