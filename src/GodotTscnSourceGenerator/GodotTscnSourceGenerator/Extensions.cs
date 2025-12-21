using System.Text;

namespace GodotTscnSourceGenerator
{
    public static class Extensions
    {
        extension(string? text)
        {
            public string? ToPascalCase()
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
        }
        extension(string text)
        {
            public string GetSafeName()
            {
                return text.Replace(" ", "_");
            }
        }
        extension(StringBuilder sb)
        {
            public void AppendLine(int offset, string value)
            {
                for (int i=0; i < offset; i++)
                {
                    sb.Append('\t');
                }
                sb.AppendLine(value);
            }
        }
    }
}
