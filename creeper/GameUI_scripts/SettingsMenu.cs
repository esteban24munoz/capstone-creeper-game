using Godot;
using System;

public partial class SettingsMenu : Control
{
	public override void _Ready(){ 
		GetNode<Button>("%CloseButton").Pressed += () => 
		Visible = false;
	}

}
