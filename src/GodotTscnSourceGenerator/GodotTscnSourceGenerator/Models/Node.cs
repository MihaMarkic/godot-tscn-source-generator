using System.Collections.Immutable;

namespace GodotTscnSourceGenerator.Models
{
    public class Node
    {
        public string Name { get; }
        public string Type { get; }
        public ImmutableDictionary<string, SubResource> SubResources { get; }
        public Node(string name, string type, 
            ImmutableDictionary<string, SubResource>? subResources = null)
        {
            Name = name;
            Type = type;
            SubResources = subResources ?? ImmutableDictionary<string, SubResource>.Empty;
        }
    }
}
