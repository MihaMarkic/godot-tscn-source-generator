using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using GodotTscnSourceGenerator.Models;
using Righthand.GodotTscnParser.Engine.Grammar;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static Righthand.GodotTscnParser.Engine.Grammar.TscnParser;

namespace GodotTscnSourceGenerator
{
    public class TscnListener: TscnBaseListener
    {
        public List<Node> Nodes { get; } = new List<Node>();
        public Script? Script { get; private set; }
        public override void EnterNode([NotNull] NodeContext context)
        {
            var pairs = context.pair().GetStringPairs();
            if (pairs.TryGetValue("name", out var name) && pairs.TryGetValue("type", out var type))
            {
                if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(type))
                {
                    Nodes.Add(new Node(name, type));
                }
            }
            base.EnterNode(context);
        }

        public override void EnterExtResource([NotNull] ExtResourceContext context)
        {
            var pairs = context.pair().GetStringPairs();
            if (pairs.TryGetValue("type", out var type))
            {
                switch (type)
                {
                    case "Script":
                        if (pairs.TryGetValue("path", out var path))
                        {
                            string className = GetClassName(path);
                            Script = new Script(className, path);
                        }
                        break;
                }
            }
            base.EnterExtResource(context);
        }

        public static string GetClassName(string fileName)
        {
            string rawName = Path.GetFileNameWithoutExtension(fileName);
            if (rawName.Contains('.'))
            {
                int index = rawName.IndexOf('.');
                return rawName.Substring(0, index);
            }
            else
            {
                return rawName;
            }
        }
    }

    public static class ListenerExtensions
    {
        internal static Dictionary<string, string> GetStringPairs(this PairContext[] context)
            => context.EnumerateStringPairs().ToDictionary(p => p.Key, p => p.Value);
        internal static IEnumerable<KeyValuePair<string, string>> EnumerateStringPairs(this PairContext[] context)
        {
            foreach (var p in context)
            {
                // checks if value is string
                var terminal = p.value().children[0] as TerminalNodeImpl;
                if (terminal != null && terminal.Symbol.Type == STRING)
                {
                    yield return new (p.children[0].GetText(), terminal.Symbol.Text.Trim('\"'));
                }
            }
        }
    }
}
