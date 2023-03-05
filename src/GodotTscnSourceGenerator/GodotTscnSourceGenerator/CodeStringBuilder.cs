using System.Text;

namespace GodotTscnSourceGenerator
{
    public class CodeStringBuilder
    {
        readonly StringBuilder sb = new StringBuilder();
        int offset;

        public void AppendStartBlock()
        {
            sb.AppendLine(offset, "{");
            offset++;
        }
        public void AppendEndBlock()
        {
            offset--;
            sb.AppendLine(offset, "}");
        }
        public void AppendLine(string value) => sb.AppendLine(offset, value);
        public void IncOffset() => offset++;
        public void DecOffset() => offset--;

        public override string ToString() => sb.ToString();
    }
}
