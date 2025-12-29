using System;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using GodotTscnSourceGenerator.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Righthand.GodotTscnParser.Engine.Grammar;

namespace GodotTscnSourceGenerator
{
    [Generator(LanguageNames.CSharp)]
    public class TscnTypesGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            ProcessGodotProjFile(context);
            ProcessTscnFiles(context);
            ProcessAllTscnFiles(context);
        }

        private void ProcessTscnFiles(IncrementalGeneratorInitializationContext context)
        {
            IncrementalValuesProvider<AdditionalText> tscnFiles = context.AdditionalTextsProvider
                .Where(static f => string.Equals(Path.GetExtension(f.Path), ".tscn", StringComparison.OrdinalIgnoreCase));
            
            IncrementalValuesProvider<(string File, string Content)> tscnFilesContents = 
                tscnFiles.Select((f, ct) => (File: f.Path, Content: f.GetText(ct)!.ToString()));

            context.RegisterSourceOutput(tscnFilesContents, ProcessTscnFiles);
            
        }
        private void ProcessTscnFiles(SourceProductionContext context, (string File, string Content) data)
        {
            try
            {
                // process only .tscn files with scripts
                var listener = RunTscnListener(data.Content, context.ReportDiagnostic, data.File);
                if (listener.Script is not null && listener.RootNode is not null)
                {
                    var sb = new CodeStringBuilder();
                    sb.AppendLine("using Godot;");
                    string safeClassName = listener.Script.ClassName.GetSafeName();
                    sb.AppendLine($"partial class {safeClassName}");
                    sb.AppendStartBlock();
                    PopulateNodeResources(sb, safeClassName, listener.RootNode);
                    sb.AppendEndBlock();
                    context.AddSource($"{listener.Script.ClassName}.g.cs", sb.ToString());
                }
            }
            catch (Exception ex)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "GTSG0001",
                        $"TSCN parsing error on {data.File}",
                        $"File {data.File}: {ex.Message}",
                        "Parsing tscn",
                        DiagnosticSeverity.Warning, true), null));
            }
        }

        private void ProcessAllTscnFiles(IncrementalGeneratorInitializationContext context)
        {
            IncrementalValuesProvider<AdditionalText> tscnFiles = context.AdditionalTextsProvider
                .Where(static f => string.Equals(Path.GetExtension(f.Path), ".tscn", StringComparison.OrdinalIgnoreCase));

             IncrementalValuesProvider<(string File, string Content)> tscnFilesContents = tscnFiles
                 .Select((f, ct) => (File: f.Path, Content: f.GetText(ct)!.ToString()));
            
             var allTscnFilesContents = tscnFilesContents.Collect();
             
             IncrementalValueProvider<string?> rootGodot = context
                 .AnalyzerConfigOptionsProvider
                 // Retrieve the RootNamespace property
                 .Select((AnalyzerConfigOptionsProvider c, CancellationToken _) =>
                     c.GlobalOptions.TryGetValue("build_property.TscnGeneratorGodotRoot", out var godotRelativeRoot)
                         ? godotRelativeRoot
                         : null);

            var metadataProvider = context.AnalyzerConfigOptionsProvider.Combine(rootGodot);
            
             var combined = allTscnFilesContents.Combine(metadataProvider);

            context.RegisterSourceOutput(combined, ProcessAllTscnFiles);
        }

        private void ProcessAllTscnFiles(SourceProductionContext context,
            (
                ImmutableArray<(string File, string Content)> TscnFiles, 
                (
                    AnalyzerConfigOptionsProvider AnalyzerConfigOptionsProvider, 
                    string? TscnGeneratorGodotRoot
                ) Meta
            ) data)
        {
            var (tscnFiles, (analyzerConfigOptionsProvider, rootGodot)) = data;
            if (analyzerConfigOptionsProvider.GlobalOptions.TryGetValue("build_property.projectdir", out var projectDir))
            {
                var scenesBuilder = new CodeStringBuilder();

                // when scenes are in a subdirectory, use <TscnGeneratorGodotRoot> csproj property to set relative directory
                if (!string.IsNullOrWhiteSpace(rootGodot))
                {
                    projectDir = Path.Combine(projectDir, rootGodot);
                    // makes sure it ends with directory separator
                    if (projectDir[projectDir.Length-1] != Path.DirectorySeparatorChar)
                    {
                        projectDir += Path.DirectorySeparatorChar;
                    }
                }
                var sceneNode = CreateSceneNodes(projectDir, [..tscnFiles.Select(f => f.File)]);
                string startingPath = "";
                if (sceneNode.Nodes.Count == 1 
                    && sceneNode.Scenes.Count == 0 
                    && sceneNode.Nodes.ContainsKey("Scenes"))
                {
                    sceneNode = sceneNode.Nodes.Values.Single();
                    startingPath = "Scenes";
                }
                scenesBuilder.AppendLine("using Godot;");

                if (!string.IsNullOrWhiteSpace(rootGodot))
                {
                    startingPath = Path.Combine(rootGodot, startingPath);
                }
                PopulateScenes(scenesBuilder, sceneNode, startingPath);
                context.AddSource($"PackedScenes.g.cs", scenesBuilder.ToString());
            }
        }

        private static void PopulateScenes(CodeStringBuilder sb, SceneNode sceneNode, string relativeDirectory)
        {
            string className = string.IsNullOrEmpty(sceneNode.Segment) ? "Scenes" : sceneNode.Segment;
            sb.AppendLine($"public static class {className}");
            sb.AppendStartBlock();
            foreach (var node in sceneNode.Nodes.Values)
            {
                PopulateScenes(sb, node, $"{relativeDirectory}/{node.Segment}");
            }
            foreach (string scene in sceneNode.Scenes)
            {
                string sceneName = Path.GetFileNameWithoutExtension(scene).ToPascalCase()!;
                string path = string.IsNullOrEmpty(relativeDirectory) ? "": $"{relativeDirectory}/";
                sb.AppendLine($"public static readonly StringName {sceneName} = \"res://{path}{scene}\";");
            }
            sb.AppendEndBlock();
        }

        private void ProcessGodotProjFile(IncrementalGeneratorInitializationContext context)
        {
            IncrementalValuesProvider<AdditionalText> godotFile = context.AdditionalTextsProvider
                .Where(static f =>
                    string.Equals(Path.GetFileName(f.Path), "project.godot", StringComparison.OrdinalIgnoreCase));
            
            IncrementalValuesProvider<(string File, string Content)> godotProjectContent = 
                godotFile.Select((f, cancellationToken) => (File: f.Path, Content: f.GetText(cancellationToken)!.ToString()));

            context.RegisterSourceOutput(godotProjectContent, ProcessGodotProjFile);
        }

        private void ProcessGodotProjFile(SourceProductionContext context, (string File, string Content) data)
        {
            try
            {
                var listener = RunGodotProjListener(data.Content);
                if (listener.InputActions.Count > 0)
                {
                    var sb = new CodeStringBuilder();
                    sb.AppendLine("using Godot;");
                    sb.AppendLine($"public static partial class InputActions");
                    sb.AppendStartBlock();
                    foreach (var ia in listener.InputActions)
                    {
                        sb.AppendLine($"public static StringName {ia.Name.ToPascalCase()} {{ get; }} = \"{ia.Name}\";");
                    }

                    sb.AppendEndBlock();
                    context.AddSource($"InputActions.g.cs", sb.ToString());
                }
            }
            catch (Exception ex)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "GTSG0002",
                        $"GODOTPROJ parsing error on {data.File}",
                        $"File {data.File}: {ex.Message}",
                        "Parsing GodotProj",
                        DiagnosticSeverity.Warning, true), null));
            }
        }

        /// <summary>
        /// Tries to convert Godot script types to C#
        /// </summary>
        /// <param name="typeName"></param>
        /// <returns></returns>
        public static string FixTypeName(string typeName)
        {
            if (typeName.Length < 4)
            {
                return typeName;
            }
            Span<char> chars = stackalloc char[typeName.Length];
            for (int i = 0; i < typeName.Length; i++)
            {
                char c = typeName[i];
                bool castToLowercase = char.IsUpper(c) && i > 0 && i < typeName.Length-1 && char.IsUpper(typeName[i + 1]); 
                chars[i] = castToLowercase ? char.ToLower(c): c;
            }

            return chars.ToString();
        }

        private static void PopulateNodeResources(CodeStringBuilder sb, string owner, Node n)
        {
            bool isNotRoot = n.Parent is not null;
            var animationResources = n.SubResources
                .Where(sr => sr.Value.Animations.Length > 0)
                .ToImmutableArray();
            var groups = n.Groups;
            string structName = $"{n.Name.GetSafeName()}Node";
            if (isNotRoot)
            {
                sb.AppendLine($"public record struct {structName}");
                sb.AppendStartBlock();
                sb.AppendLine($"readonly {owner} owner;");
            }
            foreach (var child in n.Children)
            {
                PopulateNodeResources(sb, owner, child);
            }
            if (isNotRoot)
            {
                var resourceName = n.ParentPath == "." ? n.Name : $"{n.ParentPath}/{n.Name}";
                var typeName = FixTypeName(n.Type);
                var text = $$"""
                              public {{typeName}} Instance
                              {
                                  get
                                  {
                                    using (var key = (NodePath)"{{resourceName}}")
                                    {
                                        return owner.GetNode<{{typeName}}>(key);
                                    }
                                  }
                              }
                              public string Name => "{{n.Name.GetSafeName()}}";
                              public string FullPath => "{{resourceName}}";
                              public {{structName}} ({{owner}} owner) => this.owner = owner;
                              """;
                sb.AppendLines(text);
            }
            foreach (var child in n.Children)
            {
                string childType = $"{child.Name.GetSafeName()}Node";
                string ownerInstance = isNotRoot ? "owner" : "this";
                sb.AppendLine($"public {childType} {child.Name} => new {childType}({ownerInstance});");
            }
            if (!animationResources.IsEmpty)
            {
                foreach (var res in animationResources)
                {
                    sb.AppendLine($"public static class {res.Key.ToPascalCase()}");
                    sb.AppendStartBlock();
                    foreach (var a in res.Value.Animations)
                    {
                        sb.AppendLine($"public static StringName {a.Name.ToPascalCase()} {{ get; }} = \"{a.Name}\";");
                    }
                    sb.AppendEndBlock();
                }
            }
            // creates Groups class with constants for each group
            if (groups.Count > 0)
            {
                CreateGroupConstants(sb, [..groups]);
            }
            if (isNotRoot)
            {
                sb.AppendEndBlock();
            }
        }

        private static void CreateGroupConstants(CodeStringBuilder sb, FrozenSet<string> groups)
        {
            sb.AppendLine($"public static class Groups");
            sb.AppendStartBlock();
            foreach (var g in groups.Where(g => !string.IsNullOrWhiteSpace(g)).OrderBy(g => g))
            {
                string propertyName = g.ToPascalCase()!;
                sb.AppendLine($"public static StringName {propertyName} {{ get; }} = \"{g}\";");
            }
            sb.AppendEndBlock();
        }

        static string GetSafeName(string text)
        {
            return text.Replace(" ", "_");
        }

        public static SceneNode CreateSceneNodes(string projectDir, ImmutableArray<string> paths)
        {
            var root = new SceneNode("");
            foreach (string path in paths)
            {
                string relativePath = path.Substring(projectDir.Length);
                string[] segments = relativePath.Split(Path.DirectorySeparatorChar);
                var current = root;
                for (int i = 0; i < segments.Length - 1; i++)
                {
                    string segment = segments[i];
                    if (current.Nodes.TryGetValue(segment, out var node))
                    {
                        current = node;
                    }
                    else
                    {
                        node = new SceneNode(segment);
                        current.Nodes.Add(segment, node);
                        current = node;
                    }
                }
                current.Scenes.Add(segments.Last());
            }
            return root;
        }

        TscnListener RunTscnListener(string text, Action<Diagnostic> reportDiagnostic, string filePath)
        {
            var input = new AntlrInputStream(text);
            var lexer = new TscnLexer(input);
            lexer.AddErrorListener(new SyntaxErrorListener());
            var tokens = new CommonTokenStream(lexer);
            var parser = new TscnParser(tokens)
            {
                BuildParseTree = true
            };
            parser.AddErrorListener(new ErrorListener());
            var tree = parser.file();
            var listener = new TscnListener(reportDiagnostic, filePath);
            ParseTreeWalker.Default.Walk(listener, tree);
            return listener;
        }
        GodotProjListener RunGodotProjListener(string text)
        {
            var input = new AntlrInputStream(text);
            var lexer = new GodotProjLexer(input);
            lexer.AddErrorListener(new SyntaxErrorListener());
            var tokens = new CommonTokenStream(lexer);
            var parser = new GodotProjParser(tokens)
            {
                BuildParseTree = true
            };
            parser.AddErrorListener(new ErrorListener());
            var tree = parser.file();
            var listener = new GodotProjListener();
            ParseTreeWalker.Default.Walk(listener, tree);
            return listener;
        }
    }
}

public class SyntaxErrorListener : IAntlrErrorListener<int>
{
    public void SyntaxError(TextWriter output, IRecognizer recognizer, int offendingSymbol,
        int line, int charPositionInLine, string msg, RecognitionException e)
    {
        throw new Exception(msg, e);
    }
}

public class ErrorListener : BaseErrorListener
{
    public override void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol,
        int line, int charPositionInLine, string msg, RecognitionException e)
    {
        throw new Exception(msg, e);
    }
}