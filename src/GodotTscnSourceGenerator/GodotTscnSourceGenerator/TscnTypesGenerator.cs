using System;
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
                    //context.AddSource($"{Path.GetFileName(file.Path)}.g.cs", $"/// {DateTime.Now}");
                    // process only .tscn files with scripts
                    var listener = RunTscnListener(tscnContent);
                    if (listener.Script is not null)
                    {
                        var sb = new CodeStringBuilder();
                        sb.AppendLine("using Godot;");
                        string safeClassName = listener.Script.ClassName.GetSafeName();
                        sb.AppendLine($"partial class {safeClassName}");
                        sb.AppendStartBlock();
                        foreach (var n in listener.Nodes)
                        {
                            PopulateNodeConstants(sb, n);
                        }
                        foreach (var n in listener.Nodes)
                        {
                            sb.AppendLine($"public {n.Type} Get{n.Name.GetSafeName()}Node() => GetNode<{n.Type}>(\"{n.Name}\");");
                        }
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

        public static void PopulateNodeConstants(CodeStringBuilder sb, Node n)
        {
            var animationResources = n.SubResources
                .Where(sr => sr.Value.Animations.Length > 0)
                .ToImmutableArray();
            if (!animationResources.IsEmpty)
            {
                sb.AppendLine($"public static class {n.Name.GetSafeName()}Node");
                sb.AppendStartBlock();
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
                sb.AppendEndBlock();
            }
        }
        static string GetSafeName(string text)
        {
            return text.Replace(" ", "_");
        }
        public void Initialize(GeneratorInitializationContext context)
        {
        }

        TscnListener RunTscnListener(string text)
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
            var listener = new TscnListener();
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