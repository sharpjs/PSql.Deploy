// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy;

internal static class Space
{
    private static string?[]       _spaces = new string[8];
    private static readonly object _lock   = new object();

    internal static string Pad(string s, int width)
    {
        return Get(width - s.Length);
    }

    internal static (string Left, string Right) Center(string s, int width)
    {
        return GetCentering(width - s.Length);
    }

    internal static (string Left, string Right) GetCentering(int n)
    {
        var half = n >> 1;
        return (Get(n - half), Get(half));
    }

    internal static string Get(int n)
    {
        if (n <= 0)
            return string.Empty;

        var index = n - 1;

        if (index < _spaces.Length && _spaces[index] is { } space)
            return space;

        lock (_lock)
        {
            if (index >= _spaces.Length)
                Array.Resize(ref _spaces, MathEx.GetNextPowerOf2Saturating(n));

            return _spaces[index] ??= new(' ', n);
        }
    }
}
