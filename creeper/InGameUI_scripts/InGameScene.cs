using Godot;
using System;

public partial class InGameScene : CanvasLayer
{
	private Control _helpUI;
	
	public override void _Ready(){
		GetNode<Button>("%Help").Pressed += OnHelpButtonPressed;
		_helpUI = GetNode<Control>("HelpUI");
	}
	private void OnHelpButtonPressed(){
		_helpUI.Visible = !_helpUI.Visible;
	}
}
