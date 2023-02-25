using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using GodotTscnSourceGenerator.Models;
using Righthand.GodotTscnParser.Engine.Grammar;
using System.Collections.Generic;
using System.Linq;
using static Righthand.GodotTscnParser.Engine.Grammar.TscnParser;

namespace GodotTscnSourceGenerator
{
    public class TscnListener: TscnBaseListener
    {
        public List<Node> Nodes { get; } = new List<Node>();
        public override void EnterNode([NotNull] NodeContext context)
        {
            var pairs = context.pair();
            var namePair = pairs.GetStringPairs().FirstOrDefault(p => p.Key == "name");
            var typePair = pairs.GetStringPairs().FirstOrDefault(p => p.Key == "type");
            if (!string.IsNullOrEmpty(namePair.Value) && !string.IsNullOrEmpty(typePair.Value))
            {
                Nodes.Add(new Node(namePair.Value, typePair.Value));
            }
            base.EnterNode(context);
        }
    }

    public static class ListenerExtensions
    {
        internal static IEnumerable<(string Key, string Value)> GetStringPairs(this PairContext[] context)
        {
            foreach (var p in context)
            {
                // checks if value is string
                var terminal = p.value().children[0] as TerminalNodeImpl;
                if (terminal != null && terminal.Symbol.Type == STRING)
                {
                    yield return (p.children[0].GetText(), terminal.Symbol.Text.Trim('\"'));
                }
            }
        }
    }
}
