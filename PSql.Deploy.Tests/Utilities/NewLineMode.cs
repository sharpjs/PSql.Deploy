/*
    Copyright 2022 Jeffrey Sharp

    Permission to use, copy, modify, and distribute this software for any
    purpose with or without fee is hereby granted, provided that the above
    copyright notice and this permission notice appear in all copies.

    THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES
    WITH REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF
    MERCHANTABILITY AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR
    ANY SPECIAL, DIRECT, INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES
    WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS, WHETHER IN AN
    ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT OF
    OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THIS SOFTWARE.
*/

namespace PSql.Deploy;

public sealed class NewLineMode
{
    public static NewLineMode CrLf { get; } = new("CR+LF", "\r\n");
    public static NewLineMode   Lf { get; } = new(   "LF",   "\n");

    public static IReadOnlyList<NewLineMode> All { get; } = new[] { CrLf, Lf };

    private NewLineMode(string name, string s)
    {
        Name          = name;
        NewLineString = s;
    }

    public string Name          { get; }
    public string NewLineString { get; }

    public override string ToString() => Name;
}
