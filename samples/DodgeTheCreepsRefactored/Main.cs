using Godot;
using System;

public partial class Main : Node
{
#pragma warning disable 649
	// We assign this in the editor, so we don't need the warning about not being assigned.
	[Export]
	public PackedScene MobScene;
#pragma warning restore 649
	public int Score;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
	
	void GameOver()
	{
		GetMusicNode().Stop();
		GetDeathSoundNode().Play();
		GetMobTimerNode().Stop();
		GetScoreTimerNode().Stop();
		GetHudNode().ShowGameOver();
	}
	
	public void NewGame()
	{
		GetTree().CallGroup(Mob.Groups.Mobs, Node.MethodName.QueueFree);

		Score = 0;

		var player = GetPlayerNode();
		var startPosition = GetStartPositionNode();
		player.Start(startPosition.Position);

		GetNode<Timer>("StartTimer").Start();

		var hud = GetHudNode();
		hud.UpdateScore(Score);
		hud.ShowMessage("Get Ready!");

		GetMusicNode().Play();
	}

	void OnMobTimerTimeout()
	{
		// Note: Normally it is best to use explicit types rather than the `var`
		// keyword. However, var is acceptable to use here because the types are
		// obviously Mob and PathFollow2D, since they appear later on the line.

		// Create a new instance of the Mob scene.
		var mob = MobScene.Instantiate<Mob>();

		// Choose a random location on Path2D.
		var mobSpawnLocation = GetNode<PathFollow2D>("MobPath/MobSpawnLocation");
		mobSpawnLocation.ProgressRatio = GD.Randf();

		// Set the mob's direction perpendicular to the path direction.
		float direction = mobSpawnLocation.Rotation + Mathf.Pi / 2;

		// Set the mob's position to a random location.
		mob.Position = mobSpawnLocation.Position;

		// Add some randomness to the direction.
		direction += (float)GD.RandRange(-Mathf.Pi / 4, Mathf.Pi / 4);
		mob.Rotation = direction;

		// Choose the velocity.
		var velocity = new Vector2((float)GD.RandRange(150.0, 250.0), 0);
		mob.LinearVelocity = velocity.Rotated(direction);

		// Spawn the mob by adding it to the Main scene.
		AddChild(mob);
	}


	void OnScoreTimerTimeout()
	{
		Score++;
		GetHudNode().UpdateScore(Score);
	}


	void OnStartTimerTimeout()
	{
		GetMobTimerNode().Start();
		GetScoreTimerNode().Start();
	}

}
