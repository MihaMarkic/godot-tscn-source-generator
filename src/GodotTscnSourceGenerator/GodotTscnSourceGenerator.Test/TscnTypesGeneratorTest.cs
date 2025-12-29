using System.Collections.Immutable;
using NUnit.Framework;

namespace GodotTscnSourceGenerator.Test;

public class TscnTypesGeneratorTest
{
    [TestFixture]
    public class FixTypeName: TscnTypesGeneratorTest
    {
        [TestCase("GPUParticles2D", ExpectedResult = "GpuParticles2D")]
        [TestCase("HUD", ExpectedResult = "HUD")]
        public string GivenInput_OutputsCorrect(string input)
        {
            return TscnTypesGenerator.FixTypeName(input);
        }
    }

    [TestFixture]
    public class CreateSceneNodes : TscnTypesGeneratorTest
    {
        [Test]
        public void Test()
        {
            ImmutableArray<string> input = ["/home/miha/projects/rthand/grave-defensor/godot-game/src/GraveDefensor/src/Scenes/game.tscn"];
                //"/home/miha/projects/rthand/grave-defensor/godot-game/src/GraveDefensor/src/Scenes/game.tscn; /home/miha/projects/rthand/grave-defensor/godot-game/src/GraveDefensor/src/Scenes/hud.tscn; /home/miha/projects/rthand/grave-defensor/godot-game/src/GraveDefensor/src/Scenes/path_tracker.tscn; /home/miha/projects/rthand/grave-defensor/godot-game/src/GraveDefensor/src/Scenes/tracker_run.tscn; /home/miha/projects/rthand/grave-defensor/godot-game/src/GraveDefensor/src/Scenes/enemy_path.tscn; /home/miha/projects/rthand/grave-defensor/godot-game/src/GraveDefensor/src/Scenes/amount_label.tscn; /home/miha/projects/rthand/grave-defensor/godot-game/src/GraveDefensor/src/Scenes/level.tscn; /home/miha/projects/rthand/grave-defensor/godot-game/src/GraveDefensor/src/Scenes/start.tscn; /home/miha/projects/rthand/grave-defensor/godot-game/src/GraveDefensor/src/Scenes/linear_gauge.tscn; /home/miha/projects/rthand/grave-defensor/godot-game/src/GraveDefensor/src/Scenes/Enemies/creepy_worm.tscn; /home/miha/projects/rthand/grave-defensor/godot-game/src/GraveDefensor/src/Scenes/Enemies/enemy_set.tscn; /home/miha/projects/rthand/grave-defensor/godot-game/src/GraveDefensor/src/Scenes/Enemies/tom.tscn; /home/miha/projects/rthand/grave-defensor/godot-game/src/GraveDefensor/src/Scenes/Enemies/wave.tscn; /home/miha/projects/rthand/grave-defensor/godot-game/src/GraveDefensor/src/Scenes/Levels/AttackOnGradnikove/level.tscn; /home/miha/projects/rthand/grave-defensor/godot-game/src/GraveDefensor/src/Scenes/Weapons/minigun.tscn; /home/miha/projects/rthand/grave-defensor/godot-game/src/GraveDefensor/src/Scenes/Weapons/weapon_capability.tscn; /home/miha/projects/rthand/grave-defensor/godot-game/src/GraveDefensor/src/Scenes/Weapons/weapon_picker_dialog_item.tscn; /home/miha/projects/rthand/grave-defensor/godot-game/src/GraveDefensor/src/Scenes/Weapons/weapon_pod.tscn; /home/miha/projects/rthand/grave-defensor/godot-game/src/GraveDefensor/src/Scenes/Weapons/weapon_upgrade_picker.tscn; /home/miha/projects/rthand/grave-defensor/godot-game/src/GraveDefensor/src/Scenes/Weapons/weapons_picker_dialog.tscn";
            const string projectDir = "/home/miha/projects/rthand/grave-defensor/godot-game/src/GraveDefensor/src/";
                
            var actual = TscnTypesGenerator.CreateSceneNodes(projectDir, input);

            Assert.That(actual.Scenes.Count, Is.Zero);
            Assert.That(actual.Nodes.Count, Is.EqualTo(1));
            var node =  actual.Nodes["Scenes"];
            Assert.That(node.Scenes.Count, Is.EqualTo(1));
            var scene = node.Scenes.Single();
            Assert.That(scene, Is.EqualTo("game.tscn"));
        }
    }
}