using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using GodotTscnSourceGenerator.Models;
using NUnit.Framework;
using Righthand.GodotTscnParser.Engine.Grammar;

namespace GodotTscnSourceGenerator.Test;

internal class TscnListenerTest
{
    protected TListener Run<TListener, TContext>(string text, Func<TscnParser, TContext> run)
        where TListener : TscnBaseListener, new()
        where TContext : ParserRuleContext
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
        var tree = run(parser);
        var listener = new TListener();
        ParseTreeWalker.Default.Walk(listener, tree);
        return listener;
    }

    protected TscnListener Run(string text)
    {
        return Run<TscnListener, TscnParser.FileContext>(text, p => p.file());
    }
    protected string LoadSample(string fileName)
    {
        var projectDirectory = Path.Combine(TestContext.CurrentContext.TestDirectory, "Samples");
        string path = Path.Combine(projectDirectory, $"{fileName}.tscn");
        return File.ReadAllText(path);
    }
    [TestFixture]
    public class NodeParsing: TscnListenerTest
    {
        [Test]
        public void GivenSingleNodeSample_CollectsNodeProperly()
        {
            const string input = """
                    [gd_scene load_steps=8 format=3]
                    [node name="Player" type="Area2D"]
                    script = ExtResource("1_8162q")
                    metadata/_edit_group_ = true
                    """;

            var actual = Run(input).Nodes;

            Assert.That(actual.Count, Is.EqualTo(1));
            Assert.That(actual.Single(), Is.EqualTo(new Node("Player", "Area2D")).UsingNodeComparer());
        }
        [Test]
        public void GivenPlayerSample_CollectsNodesProperly()
        {
            var actual = Run(LoadSample("Player")).Nodes;

            Assert.That(actual, Has.Exactly(3).Items);
            Assert.That(actual, Contains.Item(new Node("Player", "Area2D"))
                .UsingNodeComparer());
            Assert.That(actual, Contains.Item(new Node("AnimatedSprite2d", "AnimatedSprite2D"))
                .UsingNodeComparer());
            Assert.That(actual, Contains.Item(new Node("CollisionShape2d", "CollisionShape2D"))
                .UsingNodeComparer());
        }
    }
    [TestFixture]
    public class ExtResourceParsing: TscnListenerTest
    {
        [Test]
        public void GivenSingleNodeSample_CollectsScriptProperly()
        {
            const string input = """
                [gd_scene load_steps=8 format=3]
                [ext_resource type="Script" path="res://Player.cs" id="1_7w55o"]
            """;

            var actual = Run(input).Script;

            Assert.That(actual, Is.EqualTo(new Script("Player", "res://Player.cs")).UsingScriptComparer());
        }
    }
    [TestFixture]
    public class GetClassName : TscnListenerTest
    {
        [TestCase("res://Player.cs", ExpectedResult = "Player")]
        [TestCase("res://Kiko/Player.cs", ExpectedResult = "Player")]
        public string GivenInput_ExtractsNameCorrectly(string path)
        {
            return TscnListener.GetClassName(path);
        }
    }
}
