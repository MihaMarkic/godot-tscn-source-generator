using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using GodotTscnSourceGenerator.Models;
using Righthand.GodotTscnParser.Engine.Grammar;
using static Righthand.GodotTscnParser.Engine.Grammar.TscnParser;

namespace GodotTscnSourceGenerator
{
    public class TscnListener: TscnBaseListener
    {
        public List<Node> Nodes { get; } = new List<Node>();
        public Script? Script { get; private set; }
        public Dictionary<string, SubResource> SubResources { get; } = new Dictionary<string, SubResource>();

        public override void ExitNode([NotNull] NodeContext context)
        {
            var pairs = context.pair().GetStringPairs();
            if (pairs.TryGetValue("name", out var name) && pairs.TryGetValue("type", out var type))
            {
                if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(type))
                {
                    var complexPairs = context.complexPair().GetComplexPairs();
                    //var subResourceReferences = pairs.Where(p => )
                    var subResourceRefs = ExtractSubResourceReferences(complexPairs);
                    var subResources = subResourceRefs.Select(sr => new
                    {
                        Name = sr.Key,
                        SubResource = SubResources[sr.Value],
                    }).ToImmutableDictionary(p => p.Name, p => p.SubResource);
                    Nodes.Add(new Node(name, type, subResources));
                }
            }
            base.ExitNode(context);
        }

        ImmutableDictionary<string, string> ExtractSubResourceReferences(
            ImmutableDictionary<string, ComplexValueContext> complexPairs)
        {
            var result = new Dictionary<string, string>();
            var query = from cp in complexPairs
                        let srr = cp.Value.subResourceRef()
                        where srr is not null
                        select new 
                        {
                            Name = cp.Key,
                            Value = srr.resourceRef().GetString(),
                        };
            //{
            //    var objectArray = animationsContext.objectArray();
            //    if (objectArray is not null)
            //    {
            //        foreach (var o in objectArray.@object())
            //        {
            //            foreach (var p in o.property())
            //            {
            //                if (p.propertyName().GetString() == "name")
            //                {
            //                    var reference = p.complexValue().value().@ref().propertyName().GetString();
            //                    animations.Add(new Animation(reference));
            //                }
            //            }
            //        }
            //    }
            //}
            return query.ToImmutableDictionary(p => p.Name, p => p.Value);
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

        public override void ExitSubResource([NotNull] SubResourceContext context)
        {
            var pairs = context.pair().GetStringPairs();
            if (pairs.TryGetValue("id", out var id) && pairs.TryGetValue("type", out var type))
            {
                if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(type))
                {
                    var complexPairs = context.complexPair().GetComplexPairs();
                    var animations = ExtractAnimations(complexPairs);
                    var subResource = new SubResource(id, type, animations);
                    SubResources.Add(id, subResource);
                }
            }
            base.ExitSubResource(context);
        }

        public static ImmutableArray<Animation> ExtractAnimations(
            ImmutableDictionary<string, ComplexValueContext> complexPairs)
        {
            var animations = new List<Animation>();
            if (complexPairs.TryGetValue("animations", out var animationsContext))
            {
                var objectArray = animationsContext.objectArray();
                if (objectArray is not null)
                {
                    foreach (var o in objectArray.@object())
                    {
                        foreach (var p in o.property())
                        {
                            if (p.propertyName().GetString() == "name")
                            {
                                var reference = p.complexValue().value().@ref().propertyName().GetString();
                                animations.Add(new Animation(reference));
                            }
                        }
                    }
                }
            }
            return animations.ToImmutableArray();
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
        internal static ImmutableDictionary<string, string> GetStringPairs(this PairContext[] context)
            => context.EnumerateStringPairs().ToImmutableDictionary(p => p.Key, p => p.Value);
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
        internal static ImmutableDictionary<string, ComplexValueContext> GetComplexPairs(this ComplexPairContext[] context)
            => context.EnumerateComplexPairs().ToImmutableDictionary(p => p.Key, p => p.Value);
        internal static IEnumerable<KeyValuePair<string, ComplexValueContext>> EnumerateComplexPairs(
            this ComplexPairContext[] context)
        {
            foreach (var p in context)
            {
                string name = p.complexPairName().GetText();
                yield return new (name, p.complexValue());
                // checks if value is string
                //var terminal = p.value().children[0] as TerminalNodeImpl;
                //if (terminal != null && terminal.Symbol.Type == STRING)
                //{
                //    yield return new(p.children[0].GetText(), terminal.Symbol.Text.Trim('\"'));
                //}
            }
        }
        internal static string GetString(this RuleContext context)
        {
            var terminal = context.GetChild(0) as TerminalNodeImpl;
            if (terminal != null && terminal.Symbol.Type == STRING)
            {
                return terminal.Symbol.Text.Trim('\"');
            }
            throw new System.Exception($"{context.GetText()} is not a STRING");
        }
    }
}
