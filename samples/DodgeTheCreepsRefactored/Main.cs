using Godot;

public partial class Main : Node
{
#pragma warning disable 649
    // We assign this in the editor, so we don't need the warning about not being assigned.
    [Export]
    public PackedScene MobScene { get; set; }
#pragma warning restore 649

    private int _score;

    public void GameOver()
    {
        Music.Instance.Stop();
        DeathSound.Instance.Play();
        MobTimer.Instance.Stop();
        ScoreTimer.Instance.Stop();
        HUD.Instance.ShowGameOver();
    }

    public void NewGame()
    {
        GetTree().CallGroup(Mob.Groups.Mobs, Node.MethodName.QueueFree);

        _score = 0;

        var player = Player.Instance;
        var startPosition = StartPosition.Instance;
        player.Start(startPosition.Position);

        GetNode<Timer>("StartTimer").Start();

        var hud = HUD.Instance;
        hud.UpdateScore(_score);
        hud.ShowMessage("Get Ready!");

        Music.Instance.Play();
    }

    private void OnStartTimerTimeout()
    {
        MobTimer.Instance.Start();
        ScoreTimer.Instance.Start();
    }

    private void OnScoreTimerTimeout()
    {
        _score++;
        HUD.Instance.UpdateScore(_score);
    }

    // We also specified this function name in PascalCase in the editor's connection window.
    private void OnMobTimerTimeout()
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
}
