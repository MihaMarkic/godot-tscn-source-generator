namespace GodotTscnSourceGenerator.Models
{
    public sealed class Script
    {
        public string Path { get; }
        public string ClassName { get; }
        public Script(string className, string path)
        {
            Path = path;
            ClassName = className;
        }
    }
}
