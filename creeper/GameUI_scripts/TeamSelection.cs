using Godot;
using System;

public partial class TeamSelection : Control
{
	Label Team;
	bool isTeamChosen = false;
	
	string greenHex = "66dc40";
	string redHex = "f25625";

	// 1. ADD REFERENCES FOR YOUR ARROW BUTTONS
	Button _hobbitArrow;
	Button _sauronArrow;

	public override void _Ready() { 
		Team = GetNode<Label>("%ChosenTeam");
		
		// 2. INITIALIZE THE ARROW BUTTONS 
		// (Make sure to check "Access as Unique Name" on your arrow nodes in the editor 
		// and replace these string names if yours are named differently)
		_hobbitArrow = GetNode<Button>("%HobbitArrow");
		_sauronArrow = GetNode<Button>("%SauronArrow");

		GetNode<Button>("%StartGame").Pressed += async () =>  { 
			if (!isTeamChosen)
			{
				Team.Text = "Choose a team!";
				Team.Modulate = Colors.White; 
				return;
			}
			
			if (UIManager.Instance != null) 
			{ 
				await UIManager.Instance.ChangeSceneWithTransition("res://game.tscn"); 
			} 
		};
	}
	
public void _on_hobbit_btn_pressed()
	{
		isTeamChosen = true;
		Team.Text = "The Fellowship";
		Team.Modulate = Color.FromHtml(greenHex);

		// SYNCHRONIZE BOTH ARROW BUTTONS
		if (_hobbitArrow != null) _hobbitArrow.SetPressedNoSignal(true);
		if (_sauronArrow != null) _sauronArrow.SetPressedNoSignal(false); // Force Sauron arrow to un-press

		if (Globals.gameType == Globals.GameType.AI)
		{
			Constants.HeroPlayer = new LocalPlayer();
			Constants.EnemyPlayer = new AIPlayer();
		}
	}
	
	public void _on_sauron_btn_pressed()
	{
		isTeamChosen = true;
		Team.Text = "Forces of Mordor";
		Team.Modulate = Color.FromHtml(redHex);

		// SYNCHRONIZE BOTH ARROW BUTTONS
		if (_sauronArrow != null) _sauronArrow.SetPressedNoSignal(true);
		if (_hobbitArrow != null) _hobbitArrow.SetPressedNoSignal(false); // Force Hobbit arrow to un-press

		if (Globals.gameType == Globals.GameType.AI)
		{
			Constants.HeroPlayer = new AIPlayer();
			Constants.EnemyPlayer = new LocalPlayer();
		}
	}
}
