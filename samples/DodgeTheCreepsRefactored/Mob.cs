using Godot;

public partial class Mob : RigidBody2D
{
    public override void _Ready()
    {
        var animSprite2D = AnimatedSprite2D.Instance;
        animSprite2D.Play();
        string[] mobTypes = animSprite2D.SpriteFrames.GetAnimationNames();
        animSprite2D.Animation = mobTypes[GD.Randi() % mobTypes.Length];
    }

    public void OnVisibleOnScreenNotifier2DScreenExited()
    {
        QueueFree();
    }
}
