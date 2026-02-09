using Godot;
using System;

public partial class Help : Control
{
	private RichTextLabel _tutorialText;
	private Button _nextButton;
	private Button _backButton;
	
	private int _pageIndex = 0;

	private string[] _pages =
	{
		// Page 0
		"[b]Ohhh hello, master…[/b]  welcome to the board, yes…\n\n" +
		"This is a game of [b]slow expansion[/b] and careful planning.\n\n" +
		"The sneaky little hobbitses (Green Team) are the [b]first characters[/b] to move on the board.",

		// Page 1
		"[b]The Game Goal:[/b]\n\n" + 
		"Your goal is to create a [b]continuous path of tiles (connected horizontally or vertically) linking your home bases[/b]" +
		" before your opponent does.\n\n",
		
	  // Page 2 — Character Moves
		"[b]Characters can only move on adjacent gray tiles[/b]\n\n" +
		"To build your path:\n\n" +
		"[b]1.Move diagonally using a character[/b] over a [b]blue tile[/b] to claim it\n",
	
	// Page 3 — Flipping and Capturing
		"[b]Flipping and Capturing:[/b]\n\n" +
		"1. You can [b]conquer an opponent’s tile[/b] by moving diagonally over it\n" +
		"2. You can also [b]jump over an opponent’s character[/b] to remove it from the board.\n\n",
		
		//// Page 5 — Home Bases
		//"**Home bases.**\n\n" +
		//"Characters may jump over [b]home bases[/b], but no tiles are placed there.\n" +
		//"Your goal is an [b]unbroken path[/b] connecting your two home bases.",

		// Page 4 — Draws
		"[b]Draws:[/b]\n\n" +
		"The game is a [b]draw[/b] if:\n" +
		"1. You remove all the opponent's character from the board\n" +
		"2. No moves remain\n",
		
		// Page 5 — Winning
		"[b]Winning the game:[/b]\n\n" +
		"Remember precious...You [b]win[/b] by forming a continuous path of tiles linking your home bases.\n" +
		"So...use all of your team's characters to conquer tiles quickly!",

			// Page 6 — Gollum
		"[b]Gollum...Gollum...[/b]\n\n",
		
	};
	
	public override void _Ready()
	{
		GetNode<Button>("%Exit").Pressed += OnExitButtonPressed;
		_tutorialText = GetNode<RichTextLabel>("%TutorialText");
		_nextButton = GetNode<Button>("%Next");
		 _backButton = GetNode<Button>("%Back");

		_nextButton.Pressed += OnNextPressed;
		 _backButton.Pressed += OnBackPressed;
		UpdatePage();
	}

	private void OnExitButtonPressed()
	{
		Visible = false;
	}
	
	private void OnNextPressed()
	{
		_pageIndex++;

		if (_pageIndex >= _pages.Length)
			_pageIndex = _pages.Length - 1;

		UpdatePage();
	}
	
	private void OnBackPressed()
	{
		_pageIndex--;

		if (_pageIndex < 0)
			_pageIndex = 0;

		UpdatePage();
	}

   private void UpdatePage()
	{
		_tutorialText.Text = _pages[_pageIndex];

		// Show Back button only from page 1 onwards
		_backButton.Visible = _pageIndex > 0;

		// Optionally, hide Next button on the last page
		_nextButton.Visible = _pageIndex < _pages.Length - 1;
	}
	
	
}
