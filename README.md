# godot-tscn-source-generator

![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/Righthand.GodotSourceGenerator)

Generates C# source code within Visual Studio 2022 based on Godot .tscn files.
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
	public static class AnimatedSprite2dNode
	{
		public static class SpriteFrames
		{
			public static StringName Up { get; } = "up";
			public static StringName Walk { get; } = "walk";
		}
	}
	public Area2D GetAlienNode() => GetNode<Area2D>("Alien");
}
```
If you have a class named `Player` you can use 
a) `GetAlienNode()` method without explicilty using generic argument or node name.
b) `node.Animation = AnimatedSprite2dNode.SpriteFrames.Up` instead of `node.Animation = "up"`
Besides providing Intellisense auto completition, it also helps when nodes are renamed in .tscn file.
If this happens, code won't compile anymore and it would require update.

## Generated code

* Strong typed GetNode<T>(name) calls
* Animation name constants

## Roadmap

More code generation.