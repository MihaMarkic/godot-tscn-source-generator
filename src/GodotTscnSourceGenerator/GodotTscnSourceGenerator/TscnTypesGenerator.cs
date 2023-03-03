using System;
using System.IO;
using System.Text;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Microsoft.CodeAnalysis;
using Righthand.GodotTscnParser.Engine.Grammar;

namespace GodotTscnSourceGenerator
{
    [Generator]
    public class TscnTypesGenerator : ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context)
        {
            foreach (var file in context.AdditionalFiles)
            {
                try
                {
                    string tscnContent = file.GetText()!.ToString();
                    var listener = Run(tscnContent);
                    // process only .tscn files with scripts
                    if (listener.Script is not null)
                    {
                        var sb = new StringBuilder();
                        sb.AppendLine("using Godot;");
                        string safeClassName = GetSafeName(listener.Script.ClassName);
                        sb.AppendLine($"partial class {safeClassName}");
                        sb.AppendLine("{");
                        foreach (var n in listener.Nodes)
                        {
                            sb.AppendLine($"\tpublic {n.Type} Get{GetSafeName(n.Name)}Node() => GetNode<{n.Type}>(\"{n.Name}\");");
                        }
                        sb.AppendLine("}");
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
                            "Parsing",
                            DiagnosticSeverity.Warning, true), null));
                    return;
                }
            }
        }
        static string GetSafeName(string text)
        {
            return text.Replace(" ", "_");
        }
        public void Initialize(GeneratorInitializationContext context)
        {
        }

        TscnListener Run(string text)
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