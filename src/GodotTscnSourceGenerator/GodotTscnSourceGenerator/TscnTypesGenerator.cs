using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using GodotTscnSourceGenerator.Models;
using Microsoft.CodeAnalysis;
using Righthand.GodotTscnParser.Engine.Grammar;

namespace GodotTscnSourceGenerator
{
    [Generator]
    public class TscnTypesGenerator : ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context)
        {
            ProcessGodotProjFile(context);
            ProcessTscnFiles(context);
        }

        public void ProcessTscnFiles(GeneratorExecutionContext context)
        {
            var tscnFiles = context.AdditionalFiles
                .Where(f => string.Equals(Path.GetExtension(f.Path), ".tscn", StringComparison.OrdinalIgnoreCase));
            foreach (var file in tscnFiles)
            {
                try
                {
                    string tscnContent = file.GetText()!.ToString();
                    // process only .tscn files with scripts
                    var listener = RunTscnListener(tscnContent, context.ReportDiagnostic, file.Path);
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
                            $"TSCN parsing error on {file.Path}",
                            $"File {file.Path}: {ex.Message}",
                            "Parsing tscn",
                            DiagnosticSeverity.Warning, true), null));
                    return;
                }
            }
        }
        public void ProcessGodotProjFile(GeneratorExecutionContext context)
        {
            var godotFile = context.AdditionalFiles
                .Where(f => string.Equals(Path.GetFileName(f.Path), "project.godot", StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();
            if (godotFile is not null)
            {
                try
                {
                    string content = godotFile.GetText()!.ToString();
                    var listener = RunGodotProjListener(content);
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
                            $"GODOTPROJ parsing error on {godotFile.Path}",
                            $"File {godotFile.Path}: {ex.Message}",
                            "Parsing GodotProj",
                            DiagnosticSeverity.Warning, true), null));
                    return;
                }
            }
        }

        public static void PopulateNodeResources(CodeStringBuilder sb, string owner, Node n)
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
                string resourceName = n.ParentPath == "." ? n.Name : $"{n.ParentPath}/{n.Name}";
                sb.AppendLine($"public {n.Type} Instance => owner.GetNode<{n.Type}>(\"{resourceName}\");");
                sb.AppendLine($"public {structName} ({owner} owner) => this.owner = owner;");
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
                CreateGroupConstants(sb, groups);
            }
            if (isNotRoot)
            {
                sb.AppendEndBlock();
            }
        }

        internal static void CreateGroupConstants(CodeStringBuilder sb, HashSet<string> groups)
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
        public void Initialize(GeneratorInitializationContext context)
        {
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