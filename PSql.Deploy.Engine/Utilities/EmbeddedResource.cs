// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy;

internal abstract class EmbeddedResource
{
    internal static string LazyLoad(ref object? location, Type type, string name)
    {
        return Volatile.Read(ref location) as string
            ?? LazyLoadCore(ref location, type, name);
    }

    [ExcludeFromCodeCoverage(Justification = "timing-dependent")]
    private static string LazyLoadCore(ref object? location, Type type, string name)
    {
        var deferral = new TaskCompletionSource<string>();

        switch (Interlocked.CompareExchange(ref location, deferral.Task, null))
        {
            case string otherValue:
                // Another thread already loaded the resource
                return otherValue;

            case Task<string> otherTask:
                // Another thread is loading the resource
                return otherTask.Result;

            default:
                var value = Load(type, name);
                deferral.SetResult(value);
                Volatile.Write(ref location, value);
                return value;
        }
    }

    internal static string Load(Type type, string name)
    {
        using var stream = type.Assembly.GetManifestResourceStream(type, name);

        if (stream is null)
            throw new FileNotFoundException();

        using var reader = new StreamReader(stream);

        return reader.ReadToEnd();
    }
}
