using Godot;
using System;

public partial class TutorialScreen : Control
{
	// 1. Structure now supports Text, Animation, AND Video
	public struct PageData
	{
		public string Text;
		public string AnimationName; 
		public string VideoPath;  
		public bool UsePathSprite; // Flag to select the sprite

		// Constructor for Animation pages
		public static PageData WithAnim(string text, string animName, bool usePathSprite = false)
			{
				return new PageData 
				{ 
					Text = text, 
					AnimationName = animName, 
					VideoPath = null,
					UsePathSprite = usePathSprite 
				};
			}

		// Constructor for Video pages (Added back)
		public static PageData WithVideo(string text, string videoPath)
			{
				return new PageData { Text = text, AnimationName = null, VideoPath = videoPath, UsePathSprite = false };
			}
			
			public static PageData TextOnly(string text)
			{
				return new PageData { Text = text, AnimationName = null, VideoPath = null, UsePathSprite = false };
			}
	}

	private RichTextLabel _tutorialText;
	private Button _nextButton;
	private Button _backButton;
	private Button _closeButton;
	
	// Visual Nodes
	private AnimatedSprite2D _spritePlayer;
	private AnimatedSprite2D _pathSpritePlayer;
	private VideoStreamPlayer _videoPlayer; // Added back

	private int _pageIndex = 0;

	// 2. Your Pages Configuration (Preserved your current text/anims)
	private PageData[] _pages =
	{
		// Page 0 - Intro
		PageData.TextOnly(
			"[b]Ohhh hello, master…[/b]  welcome to the board, yes…\n\n" +
			"This is a game of [b]slow expansion[/b] and careful planning.\n\n" +
			"The sneaky little hobbitses (Green Team) are the [b]first characters[/b] to move on the board."
		),
		
		// Page 1 - Movement
		PageData.WithAnim(
			"[b]Characters can only move on adjacent gray tiles[/b]\n\n" +
			"During your turn, [b]cross diagonally over a tile[/b] using one of your characters to claim it\n",
			"move_example" 
		),
	
		// Page 2 - Flipping/Capturing
		// (Currently set to Animation, but you can change to .WithVideo(...) if you have a file)
		PageData.WithVideo(
			"[b]Attacking:[/b]\n" +
			"You can [b]jump horizontally or vertically over an opponent’s character[/b] to remove it from the board.\n\n",
            "res://UI_assets/Tutorial/capture_video.ogv" 
		),
		
		PageData.WithVideo(
			"[b]Capturing:[/b]\n" +
			"You can [b]conquer an opponent’s tile[/b] by crossing diagonally over it\n\n",
            "res://UI_assets/Tutorial/claim_tile_video.ogv" 
		),

		// Page 3 - Home Bases
		PageData.WithVideo(
			"[b]Home bases:[/b]\n" +
			"Characters may cross diagonally over [b]opponent's home bases[/b], but no tiles are placed there.\n\n",
			"res://UI_assets/Tutorial/home_bases.ogv" 
		),
		
		// Page 4 - Game Goal
		PageData.TextOnly(
			"[b]The Game Goal:[/b]\n\n" + 
			"Your goal is to create a [b]continuous path of tiles linking your home bases[/b]" +
			" before your opponent does.\n\n"
		),
		
		PageData.WithAnim(
			"",
			"path_anim", 
			true // This tells it to use the new node
		),

		// Page 5 - Draws
		PageData.TextOnly(
			"[b]Draws:[/b]\n" +
			"The game is a [b]draw[/b] if:\n" +
			"1. You remove all the opponent's characters from the board\n" +
			"2. Players are stuck in a loop, repeating the same moves 3 times\n"
		),
		
		// Page 6 - Winning
		PageData.TextOnly(
			 "[b]Winning the game:[/b]\n\n" +
			"Remember precious...You [b]win[/b] by forming a continuous path of tiles linking your home bases.\n" +
			"So...use all of your team's characters to conquer tiles quickly!"
		),

		// Page 7 - Gollum
		PageData.TextOnly(
			"[b]Gollum...Gollum...[/b]\n\n"
		),
	};
	
	public override void _Ready()
	{
		GetNode<Button>("%Exit").Pressed += OnExitButtonPressed;
		_tutorialText = GetNode<RichTextLabel>("%TutorialText");
		_nextButton = GetNode<Button>("%Next");
		_backButton = GetNode<Button>("%Back");
		_closeButton = GetNode<Button>("%CloseButton");
		_closeButton.Pressed += OnExitButtonPressed;

		// Get both visual nodes
		_spritePlayer = GetNode<AnimatedSprite2D>("%TutorialSprite");
		_pathSpritePlayer = GetNode<AnimatedSprite2D>("%AnimatedPathSprite");
		_videoPlayer = GetNode<VideoStreamPlayer>("%TutorialVideo"); // Added back

		_nextButton.Pressed += OnNextPressed;
		_backButton.Pressed += OnBackPressed;
		
		UpdatePage();
	}

	private void OnExitButtonPressed()
	{
		// 1. Hide the menu immediately
		Visible = false;
		
		// 2. Reset the page counter to the start
		_pageIndex = 0;
		
		// 3. Update the visuals so it is ready for the next time it opens
		// (Since Page 0 is TextOnly, this is safe and won't start playing hidden videos)
		UpdatePage();
	}
	
	private void OnNextPressed()
	{
		_pageIndex++;
		if (_pageIndex >= _pages.Length) _pageIndex = _pages.Length - 1;
		UpdatePage();
	}
	
	private void OnBackPressed()
	{
		_pageIndex--;
		if (_pageIndex < 0) _pageIndex = 0;
		UpdatePage();
	}

	private void UpdatePage()
	{
		PageData currentPage = _pages[_pageIndex];

		_tutorialText.Text = currentPage.Text;

		if (!string.IsNullOrEmpty(currentPage.AnimationName))
		{
			// Hide Video
			_videoPlayer.Visible = false;
			_videoPlayer.Stop();

			// NEW LOGIC: Check which sprite to use
			if (currentPage.UsePathSprite)
			{
				// Activate Path Sprite
				_spritePlayer.Visible = false; // Hide main
				_spritePlayer.Stop();
				
				_pathSpritePlayer.Visible = true; // Show path
				_pathSpritePlayer.Play(currentPage.AnimationName);
			}
			else
			{
				// Activate Main Sprite
				_pathSpritePlayer.Visible = false; // Hide path
				_pathSpritePlayer.Stop();

				_spritePlayer.Visible = true; // Show main
				_spritePlayer.Play(currentPage.AnimationName);
			}
		}
		else if (!string.IsNullOrEmpty(currentPage.VideoPath))
		{
			// Hide BOTH sprites when showing video
			_spritePlayer.Visible = false;
			_pathSpritePlayer.Visible = false;
			
			_videoPlayer.Visible = true;
			_videoPlayer.Stream = GD.Load<VideoStream>(currentPage.VideoPath);
			_videoPlayer.Play();
		}
		else
		{
			// Hide ALL visuals
			_spritePlayer.Visible = false;
			_pathSpritePlayer.Visible = false;
			_videoPlayer.Visible = false;
		}

	// --- BUTTON VISIBILITY LOGIC ---
		
		// Check if this is the very last page
		bool isLastPage = _pageIndex == _pages.Length - 1;

		// 1. Back Button: Visible on all pages except the first
		_backButton.Visible = _pageIndex > 0;

		// 2. Next Button: Visible on all pages EXCEPT the last one
		_nextButton.Visible = !isLastPage;

		// 3. Close Button: Visible ONLY on the last page
		_closeButton.Visible = isLastPage;
	}
}
