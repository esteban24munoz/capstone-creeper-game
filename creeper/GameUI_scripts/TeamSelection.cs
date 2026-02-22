using Godot;
using System;

public partial class TeamSelection : Control
{
	public override void _Ready()
	{
		// Now these connections will actually happen:
		GetNode<Button>("%StartGame").Pressed += () => 
			GetTree().ChangeSceneToFile("res://game.tscn");
	}
}
