using System.Collections.Generic;

namespace PSql.Deploy
{
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
}
