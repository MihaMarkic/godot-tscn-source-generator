using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using GodotTscnSourceGenerator.Models;
using Microsoft.CodeAnalysis;
using Righthand.GodotTscnParser.Engine.Grammar;
using static Righthand.GodotTscnParser.Engine.Grammar.TscnParser;

namespace GodotTscnSourceGenerator
{
    public class TscnListener : TscnBaseListener
    {
        public Node? RootNode { get; private set; }
        public Script? Script { get; private set; }
        public Dictionary<string, SubResource> SubResources { get; } = new();
        private readonly Action<Diagnostic> _reportDiagnostic;
        private readonly string _fileName;
        private Dictionary<string, Script> Scripts { get; } = new();
        private Dictionary<string, ExtResource> ExtResources { get; } = new();
        private Node? _lastNode;
        public TscnListener(Action<Diagnostic> reportDiagnostic, string fileName)
        {
            _reportDiagnostic = reportDiagnostic;
            _fileName = fileName;
        }
        public override void ExitNode([NotNull] NodeContext context)
        {
            var pairs = context.complexPair().GetComplexPairs();
            if (pairs.TryGetValue("name", out var nameValue))
            {
                string? name = nameValue.value()?.GetString();
                Script? script = null;
                if (!string.IsNullOrEmpty(name))
                {
                    string? type = null;
                    if (pairs.TryGetValue("script", out var scriptExtResourceValue))
                    {
                        script = GetExtResourceScript(scriptExtResourceValue);
                        type = script?.ClassName;
                    }
                    else if (pairs.TryGetValue("type", out var typeValue))
                    {
                        type = typeValue.value()?.GetString();
                    }
                    else if (pairs.TryGetValue("instance", out var instanceValue))
                    {
                        type = GetClassNameFromInstance(instanceValue);
                    }
                    if (!string.IsNullOrEmpty(type))
                    {
                        // do not add root node as node
                        var complexPairs = context.complexPair().GetComplexPairs();
                        //var subResourceReferences = pairs.Where(p => )
                        var subResourceRefs = ExtractSubResourceReferences(complexPairs);
                        var subResources = subResourceRefs.Select(sr => new
                        {
                            Name = sr.Key,
                            SubResource = SubResources[sr.Value],
                        }).ToFrozenDictionary(p => p.Name, p => p.SubResource);
                        HashSet<string> groups = new HashSet<string>();
                        if (pairs.TryGetValue("groups", out var groupsValue))
                        {
                            var groupStrings =
                                from cv in groupsValue.complexValueArray()?.complexValue()
                                let g = cv.value()?.GetString()
                                where !string.IsNullOrWhiteSpace(g)
                                select g;
                            foreach (var g in groupStrings)
                            {
                                groups.Add(g);
                            }
                        }
                        if (pairs.TryGetValue("parent", out var parentValue))
                        {
                            string parentPath = parentValue.value().GetString();
                            Node? parent = _lastNode;
                            if (string.Equals(parentPath, ".", StringComparison.Ordinal))
                            {
                                parent = RootNode;
                            }
                            else if (!string.Equals(_lastNode?.FullName, parentPath, StringComparison.Ordinal))
                            {
                                while (parent is not null && !string.Equals(parent.FullName, parentPath, StringComparison.Ordinal))
                                {
                                    parent = parent.Parent;
                                }
                            }
                            if (parent is not null)
                            {
                                _lastNode = new Node(name!, type!, parent, parentPath, subResources, [..groups]);
                                parent.Children.Add(_lastNode);
                            }
                            else
                            {
                                _reportDiagnostic(Diagnostic.Create(
                                    new DiagnosticDescriptor(
                                        "GTSG0002",
                                        $"TSCN parsing error on {_fileName}",
                                        $"File {_fileName}: Could not find parent node for node {name} with parent path {parentPath}",
                                        "Parsing tscn",
                                        DiagnosticSeverity.Warning, true), null));
                            }
                        }
                        else if (script is not null)
                        {
                            RootNode = _lastNode = new Node(name!, type!, null, null, subResources, [..groups]);
                            Script = script;
                        }
                    }
                }
            }
            base.ExitNode(context);
        }

        private string? GetClassNameFromInstance(ComplexValueContext context)
        {
            var extResourceRef = context.extResourceRef()?.resourceRef()?.GetString();
            if (extResourceRef is not null)
            {
                if (ExtResources.TryGetValue(extResourceRef, out var extResource))
                {
                    return GetClassName(extResource.Path);
                }
            }
            return null;
        }

        private Script? GetExtResourceScript(ComplexValueContext context)
        {
            var extResourceRef = context.extResourceRef()?.resourceRef()?.GetString();
            if (extResourceRef is not null)
            {
                if (Scripts.TryGetValue(extResourceRef, out var script))
                {
                    return script;
                }
            }
            return null;
        }

        ImmutableDictionary<string, string> ExtractSubResourceReferences(
            ImmutableDictionary<string, ComplexValueContext> complexPairs)
        {
            var query = from cp in complexPairs
                        let srr = cp.Value.subResourceRef()
                        where srr is not null
                        select new
                        {
                            Name = cp.Key,
                            Value = srr.resourceRef().GetString(),
                        };
            return query.ToImmutableDictionary(p => p.Name, p => p.Value);
        }

        public override void EnterExtResource([NotNull] ExtResourceContext context)
        {
            var pairs = context.pair().GetStringPairs();
            if (pairs.TryGetValue("type", out var type))
            {
                if (pairs.TryGetValue("path", out var path) && !string.IsNullOrEmpty(path)
                    && pairs.TryGetValue("id", out var id) && !string.IsNullOrEmpty(id))
                {
                    switch (type)
                    {
                        case "Script":
                            string className = GetClassName(path);
                            Scripts.Add(id, new Script(className, path));
                            break;
                        default:
                            if (pairs.TryGetValue("uid", out var uid) && !string.IsNullOrEmpty(uid))
                            {
                                ExtResources.Add(id, new ExtResource(uid, id, type, path));
                            }
                            break;
                    }
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

        private static ImmutableArray<Animation> ExtractAnimations(
            ImmutableDictionary<string, ComplexValueContext> complexPairs)
        {
            var animations = new List<Animation>();
            if (complexPairs.TryGetValue("animations", out var animationsContext))
            {
                foreach (var o in GetAllObjectContexts(animationsContext))
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
            return [..animations];
        }

        private static IEnumerable<ObjectContext> GetAllObjectContexts(ComplexValueContext complexValue)
        {
            var o = complexValue.@object(); 
            if (o is not null)
            {
                yield return o;
            }
            else
            {
                var values = complexValue.complexValueArray()?.children;
                if (values is not null)
                {
                    foreach (var childValue in values.OfType<ComplexValueContext>())
                    {
                        foreach (var childObject in GetAllObjectContexts(childValue))
                        {
                            yield return childObject;
                        }
                    }
                }
            }
        }

        public static string GetClassName(string fileName)
        {
            string rawName = Path.GetFileNameWithoutExtension(fileName);
            string className;
            if (rawName.Contains('.'))
            {
                int index = rawName.IndexOf('.');
                className = rawName.Substring(0, index);
            }
            else
            {
                className = rawName;
            }
            return className.ToPascalCase()!;
        }
    }

    public static class ListenerExtensions
    {
        extension(PairContext[] context)
        {
            internal ImmutableDictionary<string, string> GetStringPairs()
                => context.EnumerateStringPairs().ToImmutableDictionary(p => p.Key, p => p.Value);

            private IEnumerable<KeyValuePair<string, string>> EnumerateStringPairs()
            {
                foreach (var p in context)
                {
                    // checks if value is string
                    if (p.complexValue().children.SingleOrDefault() is ValueContext value)
                    {
                        if (value.children.SingleOrDefault() is TerminalNodeImpl terminal && terminal.Symbol.Type == STRING)
                        {
                            yield return new(p.children[0].GetText(), terminal.Symbol.Text.Trim('\"'));
                        }
                    }
                }
            }
        }

        extension(ComplexPairContext[] context)
        {
            internal ImmutableDictionary<string, ComplexValueContext> GetComplexPairs()
                => context.EnumerateComplexPairs().ToImmutableDictionary(p => p.Key, p => p.Value);

            private IEnumerable<KeyValuePair<string, ComplexValueContext>> EnumerateComplexPairs()
            {
                foreach (var p in context)
                {
                    var name = p.complexPairName().GetText();
                    yield return new(name, p.complexValue());
                    // checks if value is string
                    //var terminal = p.value().children[0] as TerminalNodeImpl;
                    //if (terminal != null && terminal.Symbol.Type == STRING)
                    //{
                    //    yield return new(p.children[0].GetText(), terminal.Symbol.Text.Trim('\"'));
                    //}
                }
            }
        }

        internal static string GetString(this RuleContext context)
        {
            if (context.GetChild(0) is TerminalNodeImpl terminal && terminal.Symbol.Type == STRING)
            {
                return terminal.Symbol.Text.Trim('\"');
            }
            throw new Exception($"{context.GetText()} is not a STRING");
        }
    }
}
