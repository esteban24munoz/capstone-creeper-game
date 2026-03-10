using Godot;
using System;

public partial class TeamSelection : Control
{
	public override void _Ready() { 
		GetNode<Button>("%StartGame").Pressed += async () =>  { 
			
			if (UIManager.Instance != null) 
			{ 
				//transition manager for a smooth handoff 
				await UIManager.Instance.ChangeSceneWithTransition("res://game.tscn"); 
			} 
		};
	}
}
