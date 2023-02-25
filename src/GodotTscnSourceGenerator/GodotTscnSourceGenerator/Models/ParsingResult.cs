using System.Collections.Immutable;

namespace GodotTscnSourceGenerator.Models
{
    public class ParsingResult
    {
        public  ImmutableArray<Node> Nodes { get; }
        public ParsingResult(ImmutableArray<Node> nodes)
        {
            Nodes = nodes;
        }
    }
}
