using Godot;

public partial class Player : Area2D
{
	[Export]
	public int Speed = 400; // How fast the player will move (pixels/sec).
	[Signal]
	public delegate void HitEventHandler();

	public Vector2 ScreenSize; // Size of the game window.
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		ScreenSize = GetViewportRect().Size;
		Hide();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		var velocity = Vector2.Zero; // The player's movement vector.

		if (Input.IsActionPressed(InputActions.MoveRight))
		{
			velocity.X += 1;
		}

		if (Input.IsActionPressed(InputActions.MoveLeft))
		{
			velocity.X -= 1;
		}

		if (Input.IsActionPressed(InputActions.MoveDown))
		{
			velocity.Y += 1;
		}

		if (Input.IsActionPressed(InputActions.MoveUp))
		{
			velocity.Y -= 1;
		}
		
		var animatedSprite2D = AnimatedSprite2D.Instance;

		if (velocity.Length() > 0)
		{
			velocity = velocity.Normalized() * Speed;
			animatedSprite2D.Play();
		}
		else
		{
			animatedSprite2D.Stop();
		}

		Position += velocity * (float)delta;
		Position = new Vector2(
			x: Mathf.Clamp(Position.X, 0, ScreenSize.X),
			y: Mathf.Clamp(Position.Y, 0, ScreenSize.Y)
		);

		if (velocity.X != 0)
		{
			animatedSprite2D.Animation = "walk";
			animatedSprite2D.FlipV = false;
			// See the note below about boolean assignment.
			animatedSprite2D.FlipH = velocity.X < 0;
		}
		else if (velocity.Y != 0)
		{
			animatedSprite2D.Animation = "up";
			animatedSprite2D.FlipV = velocity.Y > 0;
		}
	}
	
	void OnBodyEntered(Node2D body)
	{
		Hide(); // Player disappears after being hit.
		EmitSignal(SignalName.Hit);
		// Must be deferred as we can't change physics properties on a physics callback.
		CollisionShape2D.Instance.SetDeferred(Godot.CollisionShape2D.PropertyName.Disabled, true);
	}

	public void Start(Vector2 pos)
	{
		Position = pos;
		Show();
		CollisionShape2D.Instance.Disabled = false;
	}
}
