using Godot;
using System;

public partial class TeamSelection : Control
{
	Label Team;
	bool isTeamChosen = false;
	
	public override void _Ready() { 
		Team = GetNode<Label>("%ChosenTeam");
		GetNode<Button>("%StartGame").Pressed += async () =>  { 
			if (!isTeamChosen)
			{
				Team.Text = "Choose a team!";
				return;
			}
			
			if (UIManager.Instance != null) 
			{ 
				//transition manager for a smooth handoff 
				await UIManager.Instance.ChangeSceneWithTransition("res://game.tscn"); 
			} 
		};
	}
	
	public void _on_hobbit_btn_pressed()
	{
		isTeamChosen = true;
		Team.Text = "The Fellowship";
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
		if (Globals.gameType == Globals.GameType.AI)
		{
			Constants.HeroPlayer = new AIPlayer();
			Constants.EnemyPlayer = new LocalPlayer();
		}
	}
}
