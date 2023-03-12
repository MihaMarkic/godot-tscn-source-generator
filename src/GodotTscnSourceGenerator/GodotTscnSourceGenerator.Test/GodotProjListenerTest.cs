using System.Collections.Immutable;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using NUnit.Framework;
using Righthand.GodotTscnParser.Engine.Grammar;

namespace GodotTscnSourceGenerator.Test;

public class GodotProjListenerTest
{
    protected TListener Run<TListener, TContext>(string text, Func<GodotProjParser, TContext> run)
        where TListener : GodotProjBaseListener, new()
        where TContext : ParserRuleContext
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
        var tree = run(parser);
        var listener = new TListener();
        ParseTreeWalker.Default.Walk(listener, tree);
        return listener;
    }

    protected GodotProjListener Run(string text)
    {
        return Run<GodotProjListener, GodotProjParser.FileContext>(text, p => p.file());
    }
    protected string LoadSample(string fileName)
    {
        var projectDirectory = Path.Combine(TestContext.CurrentContext.TestDirectory, "Samples");
        string path = Path.Combine(projectDirectory, $"{fileName}.godot");
        return File.ReadAllText(path);
    }
    [TestFixture]
    public class InputActionsParsing : GodotProjListenerTest
    {
        [Test]
        public void GivenSample_CollectsAllInputActions()
        {
            var actual = Run(LoadSample("project")).InputActions;

            var hash = actual.Select(a => a.Name).ToImmutableHashSet();
            var expected = ImmutableHashSet<string>.Empty
                .Add("move_left")
                .Add("move_right")
                .Add("move_up")
                .Add("move_down")
                .Add("start_game");
            Assert.That(hash, Is.EquivalentTo(expected));
        }
        [Test]
        public void WhenNoInputSection_NoInputActionsAreCollected()
        {
            const string input = "config_version = 5";

            var actual = Run(input).InputActions;

            Assert.That(actual, Is.Empty);
        }
        [Test]
        public void WhenEmptyInputSection_NoInputActionsAreCollected()
        {
            const string input = """
                config_version = 5

                [input]

                """;

            var actual = Run(input).InputActions;

            Assert.That(actual, Is.Empty);
        }
    }
}
