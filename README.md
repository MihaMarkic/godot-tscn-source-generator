# godot-tscn-source-generator

[![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/Righthand.GodotSourceGenerator)](https://www.nuget.org/packages/Righthand.GodotSourceGenerator/)

Generates C# source code within Visual Studio 2022 based on Godot project.godot and .tscn files.
Based on [Righthand.GodotTscnParser](https://github.com/MihaMarkic/godot-tscn-parser) which in turn uses [ANTLR](https://www.antlr.org/).

**NOTE**: Currently in alpha stage. If you have pull requests, problems or improvements ideas, feel free to create Issues.
Also .tscn parser is probably not yet complete. If you find a case when generator doesn't create any code it could be that it failed parsing .tscn file (check build output).
In such case I'd be interested in your .tscn file so I can update the parser.

## Quick start

1. Open a Godot solution in Visual Studio.
2. Mark all .tscn files that will be used for code generation as "C# analyzer additional file" - 
select file in Solution Explorer and set its `Build Action` to `C# analyzer additional file`.
Or alternatively add entry manually to .csproj as 
```xml
<ItemGroup>
	<AdditionalFiles Include="FILENAME.cs" />
</ItemGroup>
```
where FILENAME should be replaced with relative path to file. Add `AdditionalFiles` node for each file.

3. Install Righthand.GodotSourceGenerator from [NuGet](https://www.nuget.org/packages/Righthand.GodotSourceGenerator/).
Make sure you check "Include prerelease" as it is currently marked so.
4. Build project.
5. Use generated strong typed calls.

Sample generated code:
```csharp
partial class Player
{
	public record struct AnimatedSprite2DNode
	{
		// ...
		public AnimatedSprite2D Instance => owner.GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		// ...
		public static class SpriteFrames
		{
			public static StringName Up { get; } = "up";
			public static StringName Walk { get; } = "walk";
		}
	}
	public AnimatedSprite2DNode AnimatedSprite2D => new AnimatedSprite2DNode(this);
}

public static partial class InputActions
{
	public static StringName MoveLeft { get; } = "move_left";
	public static StringName MoveRight { get; } = "move_right";
	public static StringName MoveUp { get; } = "move_up";
	public static StringName MoveDown { get; } = "move_down";
	public static StringName StartGame { get; } = "start_game";
}

public static class Scenes
{
	public static readonly StringName Game = "res://Scenes/game.tscn";
	public static readonly StringName Hud = "res://Scenes/hud.tscn";
	public static readonly StringName Level = "res://Scenes/level.tscn";
	public static readonly StringName Start = "res://Scenes/start.tscn";
}
```
If you have a class named `Player` you can use 

a) `AnimatedSprite2D.Instance` method without explicilty using generic argument or node name.

b) `node.Animation = AnimatedSprite2DNode.SpriteFrames.Up` instead of `node.Animation = "up"`

c) `if (Input.IsActionPressed(InputActions.MoveRight))` instead of `if (Input.IsActionPressed("move_right))`.
Note that InputActions are global constants.

d) `Scenes.Level` instead of `"res://Scenes/level.tscn"`. If scenes are not within a Scenes directory, that root 
class will be PackedScenes and access will be like `PackedScenes.SomeDirectory.SomeScene`.

Besides providing Intellisense auto completition, it also helps when nodes are renamed in .tscn file.
If this happens, code won't compile anymore and it would require update.

## Generated code

* Strong typed GetNode<T>(name) calls
* Animation name constants
* Input actions names
* Node groups
* Supports node nesting

## Sample

* See refactored `Dodge The Creeps Refactored`

## Roadmap

More code generation.