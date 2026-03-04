using Godot;
using System;

public partial class InGameScene : CanvasLayer
{
	private Control _helpUI;
	private Control _menuUI;
	private Control _frodoWins;
	private Control _draw;
	private Control _sauronWins;
	
	public override void _Ready()
	{
		// GET THE HELP BUTTON
		GetNode<Button>("%Help").Pressed += OnHelpButtonPressed;
		_helpUI = GetNode<Control>("HelpUI");
		
		// GET THE MENU BUTTON
		GetNode<Button>("%Menu").Pressed += OnMenuButtonPressed;
		_menuUI = GetNode<Control>("MenuUI");

		// GET THE WIN SCREENS
		_frodoWins = GetNode<Control>("FrodoWins");
		_draw = GetNode<Control>("Draw");
		_sauronWins = GetNode<Control>("SauronWins");

	}

	private void OnHelpButtonPressed(){
		_helpUI.Visible = !_helpUI.Visible;
	}
	
	private void OnMenuButtonPressed(){
		_menuUI.Visible = !_menuUI.Visible;
	}

	public void ShowWinScreen(Constants.Player winner)
	{
		switch (winner)
		{
			case Constants.Player.Hero:
				_frodoWins.Visible = true;
				break;
			case Constants.Player.Enemy:
				_sauronWins.Visible = true;
				break;
			case Constants.Player.None:
				_draw.Visible = true;
				break;
		}
	}
}
