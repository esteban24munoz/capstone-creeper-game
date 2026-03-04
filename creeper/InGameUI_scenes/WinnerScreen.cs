using Godot;
using System;

public partial class WinnerScreen : Control
{
	private UIManager _ui;

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
	}
}
