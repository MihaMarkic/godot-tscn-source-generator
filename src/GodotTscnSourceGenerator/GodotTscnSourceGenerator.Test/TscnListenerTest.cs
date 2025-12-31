using System.Collections.Frozen;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using GodotTscnSourceGenerator.Models;
using NUnit.Framework;
using Righthand.GodotTscnParser.Engine.Grammar;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace GodotTscnSourceGenerator.Test;

internal class TscnListenerTest
{
    private static TscnListener Run<TContext>(string text, Func<TscnParser, TContext> run, Action<Diagnostic>? reportDiagnostic = null)
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
        reportDiagnostic ??= NullReportDiagnostic;
        var listener = new TscnListener(reportDiagnostic, "file");
        ParseTreeWalker.Default.Walk(listener, tree);
        return listener;
    }

    private static void NullReportDiagnostic(Diagnostic diagnostic)
    { }

    private static TscnListener Run(string text, Action<Diagnostic>? reportDiagnostic = null)
    {
        return Run<TscnParser.FileContext>(text, p => p.file(), reportDiagnostic);
    }

    private string LoadSample(string fileName)
    {
        var projectDirectory = Path.Combine(TestContext.CurrentContext.TestDirectory, "Samples");
        string path = Path.Combine(projectDirectory, $"{fileName}.tscn");
        return File.ReadAllText(path);
    }
    [TestFixture]
    public class NodeParsing: TscnListenerTest
    {
        [Test]
        public void GivenSingleNonRootNodeSample_CollectsNodeProperly()
        {
            const string input = """
                    [gd_scene load_steps=8 format=3]
                    [ext_resource type="Script" path="res://Main.cs" id="1_4hmc7"]
                    [node name="Player"]
                    script = ExtResource("1_4hmc7")
                    """;

            var actual = Run(input).RootNode;

            Assert.That(actual?.Children.Count, Is.EqualTo(0));
            Assert.That(actual,
                Is.EqualTo(new Node("Player", "Main", null, null)).UsingNodeComparer());
        }
        [Test]
        public void GivenPlayerSample_CollectsNodesProperly()
        {
            var actual = Run(LoadSample("Player")).RootNode;

            Assert.That(actual, Is.Not.Null);
            Assert.That(actual!.Children, Has.Exactly(2).Items);
            Assert.That(actual.Children, Contains.Item(new Node("AnimatedSprite2d", "AnimatedSprite2D", 
                parent: actual, "."))
                .UsingNodeComparer());
            Assert.That(actual.Children, Contains.Item(new Node("CollisionShape2d", "CollisionShape2D", 
                parent: actual, "."))
                .UsingNodeComparer());
        }
        [Test]
        public void GivenMainSample_CollectsNodesProperly()
        {
            var actual = Run(LoadSample("Main")).RootNode;

            Assert.That(actual, Is.Not.Null);
            Assert.That(actual!.Children, Has.Exactly(10).Items);
            Assert.That(actual.Children, Contains.Item(new Node("Player", "Player", parent: actual,"."))
                .UsingNodeComparer());
            Assert.That(actual.Children, Contains.Item(new Node("MobTimer", "Timer", parent: actual,"."))
                .UsingNodeComparer());
        }
        [Test]
        public void GivenNestedSample_CollectsNodesProperly()
        {
            var actual = Run(LoadSample("Nested")).RootNode;

            Assert.That(actual, Is.Not.Null);
            Assert.That(actual!.Children, Has.Exactly(2).Items);

            var first = actual.SelectChild("TextureRect/First");
            Assert.That(first, Is.Not.Null);
            Assert.That(first!.Children, Contains.Item(new Node("FirstFollow", "PathFollow2D", first, "TextureRect/First"))
                .UsingNodeComparer());
            var grunti = actual.SelectChild("TextureRect/First/FirstFollow/Grunti");
            Assert.That(grunti, Is.Not.Null);
            Assert.That(grunti!.Children, Contains.Item(new Node("GruntiSprite", "Sprite2D", grunti, 
                "TextureRect/First/FirstFollow/Grunti"))
                .UsingNodeComparer());
        }
        [Test]
        public void GivenSampleWithSubResourceSpriteFrames_LinksSubResourceToNodeCorrectly()
        {
            const string input = """
                [gd_scene load_steps=8 format=3]
                [ext_resource type="Script" path="res://Main.cs" id="1_4hmc7"]
                [sub_resource type="SpriteFrames" id="SpriteFrames_707dc"]
                animations = [{
                "loop": true,
                "name": &"up",
                "speed": 5.0
                }, {
                "loop": true,
                "name": &"walk",
                "speed": 5.0
                }]
                [node name="AnimatedSprite2d" type="AnimatedSprite2D"]
                script = ExtResource("1_4hmc7")
                scale = Vector2(0.5, 0.5)
                sprite_frames = SubResource("SpriteFrames_707dc")
                animation = &"up"
                """;

            var listener = Run(input);
            var actual = listener.RootNode;

            Assert.That(actual, Is.Not.Null);
            Assert.That(actual!.SubResources.Count, Is.EqualTo(1));
            Assert.That(actual.SubResources.Keys.Single(), Is.EqualTo("sprite_frames"));
            Assert.That(actual.SubResources.Values.Single(),
                 Is.SameAs(listener.SubResources.Values.Single()));
        }
        [Test]
        public void WhenNodeIsResourceInstance_GetsCorrectNameFromTscn()
        {
            const string input = """
                [gd_scene load_steps=8 format=3]
                [ext_resource type="Script" path="res://Main.cs" id="1_4hmc7"]
                [ext_resource type="PackedScene" uid="uid://g76r1u8cf6n7" path="res://player.tscn" id="3_s3hlu"]
                [node name="AnimatedSprite2d" type="AnimatedSprite2D"]
                script = ExtResource("1_4hmc7")
                [node name="Player" parent="." instance=ExtResource("3_s3hlu")]
            """;
            var actual = Run(input).RootNode;

            Assert.That(actual, Is.Not.Null);
            var player = actual!.SelectChild("Player");
            Assert.That(player, Is.Not.Null);
            Assert.That(player, Is.EqualTo(new Node("Player", "Player", actual, ".")).UsingNodeComparer());
        }

        [Test]
        public void GivenSingleNonRootNodeWithGroupsSample_CollectsNodeProperly()
        {
            const string input = """
            [gd_scene load_steps=8 format=3]
            [ext_resource type="Script" path="res://Main.cs" id="1_4hmc7"]
            [node name="AnimatedSprite2d" type="AnimatedSprite2D"]
            script = ExtResource("1_4hmc7")
            [node name="Player" type="Area2D" parent="." groups=["alfa", "beta"]]
            metadata/_edit_group_ = true
            """;

            var actual = Run(input).RootNode;

            var expectedGroups = new HashSet<string>
            {
                "alfa",
                "beta"
            }.ToFrozenSet();

            Assert.That(actual, Is.Not.Null);
            var player = actual!.SelectChild("Player");
            Assert.That(player, Is.Not.Null);
            Assert.That(player,
                Is.EqualTo(new Node("Player", "Area2D", actual, ".", groups: expectedGroups)).UsingNodeComparer());
        }
        [Test]
        public void WhenNodeHasParent_ReadsParentCorrectly()
        {
            const string input = """
                [gd_scene load_steps=8 format=3]
                [ext_resource type="Script" path="res://Main.cs" id="1_4hmc7"]
                [node name="AnimatedSprite2d" type="AnimatedSprite2D"]
                script = ExtResource("1_4hmc7")
                [node name="TextureRect" type="TextureRect" parent="."]
                [node name="First" type="Path2D" parent="TextureRect"]
                [node name="FirstFollow" type="PathFollow2D" parent="TextureRect/First"]
                """;

            var actual = Run(input).RootNode;

            Assert.That(actual, Is.Not.Null);
            var allChildren = actual!.AllChildren.ToImmutableArray();
            Assert.That(allChildren.Length, Is.EqualTo(3));
            Assert.That(allChildren.Last().ParentPath, Is.EqualTo("TextureRect/First"));
        }
    }
    [TestFixture]
    public class ExtResourceParsing : TscnListenerTest
    {
        [Test]
        public void GivenSingleRootNodeSample_CollectsScriptProperly()
        {
            const string input = """
                [gd_scene load_steps=8 format=3]
                [ext_resource type="Script" path="res://Player.cs" id="1_7w55o"]
                [node name="Player"]
                script = ExtResource("1_7w55o")
            """;

            var actual = Run(input).Script;

            Assert.That(actual, Is.EqualTo(new Script("Player", "res://Player.cs")).UsingScriptComparer());
        }
    }
    [TestFixture]
    public class SubResourceParsing: TscnListenerTest
    {
        [Test]
        public void GivenSampleSpriteFrames_CollectsAnimationsCorrectly()
        {
            const string input = """
                [gd_scene load_steps=8 format=3]
                [sub_resource type="SpriteFrames" id="SpriteFrames_707dc"]
                animations = [{
                "frames": [{
                "duration": 1.0,
                "texture": ExtResource("1_d8csi")
                }, {
                "duration": 1.0,
                "texture": ExtResource("2_ljnug")
                }],
                "loop": true,
                "name": &"up",
                "speed": 5.0
                }, {
                "frames": [{
                "duration": 1.0,
                "texture": ExtResource("3_krmrv")
                }, {
                "duration": 1.0,
                "texture": ExtResource("4_jrmwk")
                }],
                "loop": true,
                "name": &"walk",
                "speed": 5.0
                }]
                """;

            var actual = Run(input).SubResources;

            var expectedKey = "SpriteFrames_707dc";
            var expectedResource = new SubResource("SpriteFrames_707dc", "SpriteFrames",
                        ImmutableArray<Animation>.Empty
                            .Add(new Animation("up"))
                            .Add(new Animation("walk")));
            Assert.That(actual.Count, Is.EqualTo(1));
            Assert.That(actual.ContainsKey(expectedKey), Is.True);
            Assert.That(actual[expectedKey], Is.EqualTo(expectedResource).UsingSubResourceComparer());
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

    [Test]
    public void WhenNodeWithParentDot_ParsesCorrectly()
    {
        const string input = """
                             [gd_scene load_steps=2 format=3 uid="uid://bi4yp1vixeaj7"]
                             
                             [ext_resource type="Texture2D" uid="uid://6y5njrvvpry3" path="res://src/Assets/Weapons/weapon_upgrade_button.svg" id="1_s245d"]
                             
                             [node name="WeaponUpgradePicker" type="PanelContainer"]
                             
                             [node name="UpgradeButton" type="Button" parent="."]
                             layout_mode = 2
                             icon = ExtResource("1_s245d")
                             """;
        List<Diagnostic> errors = new();
        Run(input, d => errors.Add(d));
        
        Assert.That(errors, Is.Empty);
    }
}
                             