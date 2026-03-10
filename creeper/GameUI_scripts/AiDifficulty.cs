using Godot;
using System;

public partial class AiDifficulty : Control
{
	private UIManager _ui;

	public override void _Ready()
	{
		Globals.gameType = Globals.GameType.AI;
		//TODO:: Make AIPlayer according to team selected. It's currently hard coded to be the enemy
		Constants.EnemyPlayer = new AIPlayer();
		// Get the global reference
		_ui = UIManager.Instance;

		if (_ui == null)
		{
			GD.PrintErr("AiDifficulty: UIManager Instance is null! Is MainUI.tscn loaded?");
			return;
		}

		// Now these connections will actually happen:
		GetNode<Button>("%EasyButton").Pressed += () => 
			_ui.ShowScreen("res://GameUI_scenes/teamSelection.tscn");
			
		GetNode<Button>("%MediumButton").Pressed += () => 
			_ui.ShowScreen("res://GameUI_scenes/teamSelection.tscn");
			
		GetNode<Button>("%ExpertButton").Pressed += () => 
			_ui.ShowScreen("res://GameUI_scenes/teamSelection.tscn");
	}
}
