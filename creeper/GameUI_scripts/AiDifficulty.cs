using Godot;
using System;

public partial class AiDifficulty : Control
{
	private UIManager _ui;

	public override void _Ready()
	{
		// Get the global reference
		_ui = UIManager.Instance;

		if (_ui == null)
		{
			GD.PrintErr("AiDifficulty: UIManager Instance is null! Is MainUI.tscn loaded?");
			return;
		}

		// Now these connections will actually happen:
		GetNode<Button>("%Next").Pressed += () => 
			_ui.ShowScreen("res://GameUI_scenes/teamSelection.tscn");
	}
}
