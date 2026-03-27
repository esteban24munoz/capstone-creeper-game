using Godot;
using System;

public partial class InGameScene : CanvasLayer
{
	private Control _helpUI;
	private Control _menuUI;
	private Control _frodoWins;
	private Control _draw;
	private Control _sauronWins;
	
	private Control _frodoLoses;
	private Control _sauronLoses;

	private Control _activeEndScreen;
	
	// 1. ADD THE LABEL VARIABLE
	private Label _turnLabel;
	

	private CheckButton _toggleBoardButton;
	
	public override void _Ready()
	{
		
		// 2. INITIALIZE THE LABEL REFERENCE
		// Ensure you have a Label named "TurnLabel" inside your GameUI scene
		_turnLabel = GetNode<Label>("%TurnLabel");

		GetNode<Button>("%Help").Pressed += OnHelpButtonPressed;
		_helpUI = GetNode<Control>("HelpUI");
		

		GetNode<Button>("%Menu").Pressed += OnMenuButtonPressed;
		_menuUI = GetNode<Control>("MenuUI");


		_frodoWins = GetNode<Control>("FrodoWins");
		_draw = GetNode<Control>("Draw");
		_sauronWins = GetNode<Control>("SauronWins");
		
	
		_frodoLoses = GetNode<Control>("FrodoLoses");
		_sauronLoses = GetNode<Control>("SauronLoses");

		_toggleBoardButton = GetNode<CheckButton>("%ToggleBoardButton");
		_toggleBoardButton.Visible = false;
		_toggleBoardButton.Toggled += OnToggleBoardButtonPressed;
	}
	
	public void UpdateTurnText(string text, string hexColor)
	{
		if (_turnLabel != null)
		{
			_turnLabel.Text = text;
			// Color.FromHtml converts your "8d2817" string into a Godot Color object
			_turnLabel.Modulate = Color.FromHtml(hexColor);
		}
	}
	
	// 4. HIDE THE LABEL WHEN THE GAME ENDS
	public void HideTurnLabel()
	{
		if (_turnLabel != null) _turnLabel.Visible = false;
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
				_activeEndScreen = _frodoWins; // Remember this screen
				break;
			case Constants.Player.Enemy:
				_sauronWins.Visible = true;
				_activeEndScreen = _sauronWins; // Remember this screen
				break;
			case Constants.Player.None:
				_draw.Visible = true;
				_activeEndScreen = _draw; // Remember this screen
				break;
		}
	}

	// 4. ADD METHODS FOR YOUR NEW LOSE SCREENS
	public void ShowFrodoLosesScreen()
	{
		_frodoLoses.Visible = true;
		_activeEndScreen = _frodoLoses;
	}

	public void ShowSauronLosesScreen()
	{
		_sauronLoses.Visible = true;
		_activeEndScreen = _sauronLoses;
	}

	// 5. METHOD TO ACTIVATE THE TOGGLE BUTTON AT THE END OF THE GAME
	public void EnableBoardToggleButton()
	{
		_toggleBoardButton.Text = "See board moves";
		_toggleBoardButton.ButtonPressed = false; // Ensure it starts unchecked
		_toggleBoardButton.Visible = true;
	}

	// 6. TOGGLE LOGIC
	private void OnToggleBoardButtonPressed(bool toggledOn)
	{
		// Only hide the end screen, keep the rest of GameUI (and this button) visible!
		if (_activeEndScreen != null)
		{
			_activeEndScreen.Visible = !toggledOn;
		}

		if (toggledOn)
		{
			_toggleBoardButton.Text = "Back to screen";
		}
		else
		{
			_toggleBoardButton.Text = "See board moves";
		}
	}
}
