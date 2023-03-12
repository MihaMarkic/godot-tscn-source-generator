using System.Collections.Generic;
using System.Collections.Immutable;

namespace GodotTscnSourceGenerator.Models
{
    public class Node
    {
        public string Name { get; }
        public string Type { get; }
        public HashSet<string> Groups { get; }
        public ImmutableDictionary<string, SubResource> SubResources { get; }
        public Node(string name, string type, 
            ImmutableDictionary<string, SubResource>? subResources = null,
            HashSet<string>? groups = null)
        {
            Name = name;
            Type = type;
            Groups = groups ?? new HashSet<string>();
            SubResources = subResources ?? ImmutableDictionary<string, SubResource>.Empty;
        }
    }
}
