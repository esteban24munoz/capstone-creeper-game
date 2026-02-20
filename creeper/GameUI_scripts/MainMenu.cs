using Godot;
using System;

public partial class MainMenu : Control
{
	private UIManager _ui;

	public override void _Ready()
	{
		// Get the global reference we just created
		_ui = UIManager.Instance;

		if (_ui == null)
		{
			GD.PrintErr("MainMenu: UIManager Instance is null! Is MainUI.tscn loaded?");
			return;
		}

		// Now these connections will actually happen:
		GetNode<Button>("%StartButton").Pressed += () => 
			_ui.ShowScreen("res://GameUI_scenes/gameMode.tscn");

		GetNode<Button>("%TutorialButton").Pressed += () => 
			_ui.ShowScreen("res://GameUI_scenes/tutorialScreen.tscn");

		GetNode<Button>("%QuitButton").Pressed += () => GetTree().Quit();
	}
}
