// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy;

internal ref struct Column
{
    public Column(string header)
        : this(header.Length) { }

    public Column(int width)
        => Width = width;

    public int Width { get; set; }

    public void Fit(string value)
    {
        Fit(value.Length);
    }

    public void Fit(int width)
    {
        if (Width < width)
            Width = width;
    }

    public string GetPadding(string value)
    {
        return Space.Pad(value, Width);
    }
}
