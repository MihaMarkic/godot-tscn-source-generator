using System.Text;
using Antlr4.Runtime.Misc;

namespace GodotTscnSourceGenerator
{
    public static class Extensions
    {
        public static string? ToPascalCase(this string? text)
        {
            if (text is null)
            {
                return null;
            }
            var sb = new StringBuilder(text.Length);
            bool isUpper = true;
            foreach (var c in text)
            {
                if (!char.IsLetterOrDigit(c))
                {
                    isUpper = true;
                }
                else
                {
                    if (isUpper)
                    {
                        sb.Append(char.ToUpper(c));
                        isUpper = false;
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
            }
            return sb.ToString();
        }
        public static string GetSafeName(this string text)
        {
            return text.Replace(" ", "_");
        }
        public static void AppendLine(this StringBuilder sb, int offset, string value)
        {
            for (int i=0; i < offset; i++)
            {
                sb.Append('\t');
            }
            sb.AppendLine(value);
        }
    }
}
