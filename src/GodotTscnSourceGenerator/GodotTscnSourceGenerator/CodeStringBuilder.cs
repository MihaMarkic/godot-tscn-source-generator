using System;
using System.IO;
using System.Text;

namespace GodotTscnSourceGenerator
{
    public class CodeStringBuilder
    {
        private readonly StringBuilder _sb = new();
        private int _offset;

        public void AppendStartBlock()
        {
            _sb.AppendLine(_offset, "{");
            _offset++;
        }
        public void AppendEndBlock()
        {
            _offset--;
            _sb.AppendLine(_offset, "}");
        }
        public void AppendLine(string value) => _sb.AppendLine(_offset, value);

        public void AppendLines(string value)
        {
            using var reader = new StringReader(value);
            while (reader.ReadLine() is { } line)
            {
                AppendLine(line);
            }
        }
        public void IncOffset() => _offset++;
        public void DecOffset() => _offset--;

        public override string ToString() => _sb.ToString();
    }
}
