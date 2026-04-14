using Godot;
using System;
using System.Threading.Tasks;

public partial class WinnerScreen : Control
{
	private UIManager _ui;
	RichTextLabel drawReason;

	public override void _Ready()
	{
		_ui = UIManager.Instance;

		GetNode<Button>("%PlayAgain").Pressed += async () =>
		{
			Visible = false; // Hide this options menu
			
			// Destroys the game and unhides the UIManager, 
			// automatically showing whatever screen was there before!
			await _ui.RestartGame();
		};

		GetNode<Button>("%ReturnMenu").Pressed += async () =>
		{
			Visible = false; // Hide this menu
			// Pass the path to your main menu scene here
			await _ui.ReturnToMenu("res://GameUI_scenes/mainMenu.tscn");
		};
		
		CheckForDrawState();
	}
	
	private async void CheckForDrawState()
	{
		try
		{
			while (!Visible)
			{
				await Task.Delay(50);
			}
		}
		catch
		{
			return;
		}
		GD.Print($"Winner: {Globals.winner}");
		if (Globals.winner == "draw")
			UpdateDrawReason();
		
		return;
	}
	
	private void UpdateDrawReason()
	{
		drawReason = GetNode<RichTextLabel>("%FlavorText");
		if (Globals.isRepitionDraw)
			drawReason.Text = "Gollum has stolen the ring!\nYou had too many repeative moves.";
		else
			drawReason.Text = "Gollum has stolen the ring!\nAll the characters were killed.";
			
		return;
	}
}
