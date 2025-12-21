using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace GodotTscnSourceGenerator.Models
{
    [DebuggerDisplay("{Name,nq}")]
    public class Node
    {
        public string Name { get; }
        public string Type { get; }
        public string? ParentPath { get; }
        public FrozenSet<string> Groups { get; }
        public Node? Parent {  get; }
        public List<Node> Children { get; }
        public FrozenDictionary<string, SubResource> SubResources { get; }
        public Node(string name, string type, Node? parent, string? parentPath,
            FrozenDictionary<string, SubResource>? subResources = null,
            FrozenSet<string>? groups = null)
        {
            Name = name;
            Type = type;
            Parent = parent;
            ParentPath = parentPath;
            Groups = groups ?? FrozenSet<string>.Empty;
            Children= new List<Node>();
            SubResources = subResources ?? FrozenDictionary<string, SubResource>.Empty;
        }

        public string FullName => !string.IsNullOrWhiteSpace(ParentPath) && ParentPath != "."
            ? $"{ParentPath}/{Name}": Name;

        public Node? SelectChild(string path)
        {
            var segments = path.Split('/');
            return SelectChild(segments, 0);
        }

        private Node? SelectChild(string[] pathSegments, int index)
        {
            var child = Children
                .FirstOrDefault(c => string.Equals(c.Name, pathSegments[index], StringComparison.Ordinal));
            if (child is null)
            {
                return null;
            }
            index++;
            if (pathSegments.Length == index)
            {
                return child;
            }
            return child.SelectChild(pathSegments, index);
        }

        public IEnumerable<Node> AllChildren
        {
            get
            {
                foreach (var c in Children )
                {
                    yield return c;
                    foreach (var grandChild in c.AllChildren)
                    {
                        yield return grandChild;
                    }
                }
            }
        }
    }
}
