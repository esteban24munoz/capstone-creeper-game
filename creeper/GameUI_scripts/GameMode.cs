using Godot;
using System;

public partial class GameMode : Control
{
	private UIManager _ui;

	public override void _Ready()
	{
		// Get the global reference
		_ui = UIManager.Instance;

		if (_ui == null)
		{
			GD.PrintErr("GameMode: UIManager Instance is null! Is MainUI.tscn loaded?");
			return;
		}

		// Now these connections will actually happen:
		GetNode<Button>("%SinglePlayer").Pressed += () => 
			_ui.ShowScreen("res://GameUI_scenes/aiDifficulty.tscn");

		GetNode<Button>("%Multiplayer").Pressed += () => 
			_ui.ShowScreen("res://GameUI_scenes/teamSelection.tscn");
		
		GetNode<Button>("%OnlineGame").Pressed += () => 
			_ui.ShowScreen("res://GameUI_scenes/tutorialScreen.tscn");

	}
}
