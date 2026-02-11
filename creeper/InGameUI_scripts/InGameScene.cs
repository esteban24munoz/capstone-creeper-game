using Godot;
using System;

public partial class InGameScene : CanvasLayer
{
	private Control _helpUI;
	private Control _menuUI;
	
	public override void _Ready(){
		
		//GET THE HELP BUTTON
		GetNode<Button>("%Help").Pressed += OnHelpButtonPressed;
		_helpUI = GetNode<Control>("HelpUI");
		
		//GET THE MENU BUTTON
		GetNode<Button>("%Menu").Pressed += OnMenuButtonPressed;
		_menuUI = GetNode<Control>("MenuUI");
	}
	private void OnHelpButtonPressed(){
		_helpUI.Visible = !_helpUI.Visible;
	}
	
	private void OnMenuButtonPressed(){
		_menuUI.Visible = !_menuUI.Visible;
	}
}
