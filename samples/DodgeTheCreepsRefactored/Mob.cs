using Godot;

public partial class Mob : RigidBody2D
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		var animSprite2D = GetAnimatedSprite2DNode();
		animSprite2D.Play();
		string[] mobTypes = animSprite2D.SpriteFrames.GetAnimationNames();
		animSprite2D.Animation = mobTypes[GD.Randi() % mobTypes.Length];
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	void OnVisibleOnScreenNotifier2DScreenExited()
	{
		QueueFree();
	}

}
