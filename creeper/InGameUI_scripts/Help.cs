using Godot;
using System;

public partial class Help : Control
{
	// --- 1. PERSISTENCE LOGIC ---
	// Static variables persist as long as the game is running, 
	// even if the scene is reloaded or changed.
	private static bool _hasShownTutorial = false;

	public struct PageData
	{
		public string Text;
		public string AnimationName; 
		public string VideoPath;  
		public bool UsePathSprite;

		public static PageData WithAnim(string text, string animName, bool usePathSprite = false)
			=> new PageData { Text = text, AnimationName = animName, VideoPath = null, UsePathSprite = usePathSprite };

		public static PageData WithVideo(string text, string videoPath)
			=> new PageData { Text = text, AnimationName = null, VideoPath = videoPath, UsePathSprite = false };
			
		public static PageData TextOnly(string text)
			=> new PageData { Text = text, AnimationName = null, VideoPath = null, UsePathSprite = false };
	}

	private RichTextLabel _tutorialText;
	private Button _nextButton;
	private Button _backButton;
	private Button _closeButton;
	private Button _skipButton; // Reference for Skip Tutorial

	private AnimatedSprite2D _spritePlayer;
	private AnimatedSprite2D _pathSpritePlayer;
	private VideoStreamPlayer _videoPlayer;

	private int _pageIndex = 0;

	private PageData[] _pages =
	{
		PageData.TextOnly(
			"[b]Ohhh hello, master…[/b]  welcome to the board, yes…\n\n" +
			"The sneaky little hobbitses (Green Team) are the [b]first characters[/b] to move on the board."
		),
		PageData.TextOnly(
			"[b]The Game Goal:[/b]\n\n" + 
			"Your goal is to create a [b]continuous path of tiles linking your home bases[/b]" +
			" before your opponent does.\n\n"
		),
		PageData.WithAnim("", "path_anim", true),
		PageData.WithAnim(
			"[b]Characters can only move on adjacent gray tiles[/b]\n\n" +
			"During your turn, [b]cross diagonally over a tile[/b] using one of your characters to claim it\n",
			"move_example" 
		),
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
		PageData.WithVideo(
			"[b]Home bases:[/b]\n" +
			"Characters may cross diagonally over [b]opponent's home bases[/b], but no tiles are placed there.\n\n",
			"res://UI_assets/Tutorial/home_bases.ogv" 
		),
		PageData.TextOnly(
			"[b]Draws:[/b]\n" +
			"The game is a [b]draw[/b] if:\n" +
			"1. You remove all the opponent's characters from the board\n" +
			"2. Players are stuck in a loop, repeating the same moves 3 times\n"
		),
		PageData.TextOnly(
			 "[b]Winning the game:[/b]\n\n" +
			"Remember precious...You [b]win[/b] by forming a continuous path of tiles linking your home bases.\n" +
			"So...use all of your team's characters to conquer tiles quickly!"
		),
		PageData.TextOnly("[b]Gollum...Gollum...[/b]\n\n"),
	};
	
	public override async void _Ready()
	{
		// Initialize Nodes
		_tutorialText = GetNode<RichTextLabel>("%TutorialText");
		_nextButton = GetNode<Button>("%Next");
		_backButton = GetNode<Button>("%Back");
		_closeButton = GetNode<Button>("%CloseButton");
		_skipButton = GetNode<Button>("%SkipButton");
		
		_spritePlayer = GetNode<AnimatedSprite2D>("%TutorialSprite");
		_pathSpritePlayer = GetNode<AnimatedSprite2D>("%AnimatedPathSprite");
		_videoPlayer = GetNode<VideoStreamPlayer>("%TutorialVideo");

		// Connections
		_nextButton.Pressed += OnNextPressed;
		_backButton.Pressed += OnBackPressed;
		_closeButton.Pressed += OnExitButtonPressed;
		_skipButton.Pressed += OnExitButtonPressed;
		GetNode<Button>("%Exit").Pressed += OnExitButtonPressed;

		UpdatePage();
		
		// 2. SHOW/HIDE LOGIC
		if (_hasShownTutorial)
		{
			// If we already saw the tutorial, just hide it and do nothing else.
			// Because we didn't "return" early, the nodes are safely loaded 
			// if the player clicks the Help button later!
			Visible = false;
		}
		else
		{
			// First time playing: hide it initially, wait 1 second, then pop it up.
			Visible = false;
			_hasShownTutorial = true;
			
			await ToSignal(GetTree().CreateTimer(1.0f), SceneTreeTimer.SignalName.Timeout);
			Visible = true;
		}
	}

	private void OnExitButtonPressed()
	{
		Visible = false;
		_pageIndex = 0;
		UpdatePage();
	}
	
	private void OnNextPressed()
	{
		_pageIndex = Math.Min(_pageIndex + 1, _pages.Length - 1);
		UpdatePage();
	}
	
	private void OnBackPressed()
	{
		_pageIndex = Math.Max(_pageIndex - 1, 0);
		UpdatePage();
	}

	private void UpdatePage()
	{
		PageData currentPage = _pages[_pageIndex];
		_tutorialText.Text = currentPage.Text;

		// Handle Visuals
		UpdateVisuals(currentPage);

		// --- 3. BUTTON VISIBILITY LOGIC ---
		bool isFirstPage = _pageIndex == 0;
		bool isLastPage = _pageIndex == _pages.Length - 1;

		_backButton.Visible = !isFirstPage;
		_nextButton.Visible = !isLastPage;
		_closeButton.Visible = isLastPage;
		
		// Skip Button only shows on the very first page
		_skipButton.Visible = isFirstPage;
	}

	private void UpdateVisuals(PageData data)
	{
		// Reset all
		_spritePlayer.Visible = false;
		_pathSpritePlayer.Visible = false;
		_videoPlayer.Visible = false;
		_videoPlayer.Stop();

		if (!string.IsNullOrEmpty(data.AnimationName))
		{
			var target = data.UsePathSprite ? _pathSpritePlayer : _spritePlayer;
			target.Visible = true;
			target.Play(data.AnimationName);
		}
		else if (!string.IsNullOrEmpty(data.VideoPath))
		{
			_videoPlayer.Visible = true;
			_videoPlayer.Stream = GD.Load<VideoStream>(data.VideoPath);
			_videoPlayer.Play();
		}
	}
}
