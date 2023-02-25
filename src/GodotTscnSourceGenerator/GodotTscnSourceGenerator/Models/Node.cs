namespace GodotTscnSourceGenerator.Models
{
    public class Node
    {
        public string Name { get; }
        public string Type { get; }
        public Node(string name, string type)
        {
            Name = name;
            Type = type;
        }
    }
}
