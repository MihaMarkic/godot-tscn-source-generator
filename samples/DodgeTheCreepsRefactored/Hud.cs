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
		var message = GetMessageNode();
		message.Text = text;
		message.Show();

		GetMessageTimerNode().Start();
	}

	async public void ShowGameOver()
	{
		ShowMessage("Game Over");

		var messageTimer = GetMessageTimerNode();
		await ToSignal(messageTimer, "timeout");

		var message = GetMessageNode();
		message.Text = "Dodge the\nCreeps!";
		message.Show();

		await ToSignal(GetTree().CreateTimer(1), "timeout");
		GetStartButtonNode().Show();
	}
	public void UpdateScore(int score)
	{
		GetScoreLabelNode().Text = score.ToString();
	}
	void OnMessageTimerTimeout()
	{
		GetMessageNode().Hide();
	}


	void OnStartButtonPressed()
	{
		GetStartButtonNode().Hide();
		EmitSignal(SignalName.StartGame);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
