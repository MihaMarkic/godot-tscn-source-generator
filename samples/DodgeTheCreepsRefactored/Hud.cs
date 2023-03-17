using Godot;

public partial class Hud : CanvasLayer
{
	[Signal]
	public delegate void StartGameEventHandler();
	public override void _Ready()
	{
	}

	public void ShowMessage(string text)
	{
		var message = Message.Instance;
		message.Text = text;
		message.Show();

		MessageTimer.Instance.Start();
	}

	async public void ShowGameOver()
	{
		ShowMessage("Game Over");

		var messageTimer = MessageTimer.Instance;
		await ToSignal(messageTimer, "timeout");

		var message = Message.Instance;
		message.Text = "Dodge the\nCreeps!";
		message.Show();

		await ToSignal(GetTree().CreateTimer(1), "timeout");
		StartButton.Instance.Show();
	}
	public void UpdateScore(int score)
	{
		ScoreLabel.Instance.Text = score.ToString();
	}
	void OnMessageTimerTimeout()
	{
		Message.Instance.Hide();
	}


	void OnStartButtonPressed()
	{
		StartButton.Instance.Hide();
		EmitSignal(SignalName.StartGame);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
