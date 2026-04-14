using Godot;
using System;

public partial class TutorialUi : Control
{
	private UIManager _ui;
	
	public struct PageData
	{
		public string Text;
		public string AnimationName; 
		public string VideoPath;  
		public bool IsFullscreenExample;

		public static PageData WithAnim(string text, string animName, bool isFullscreen = false)
			=> new PageData { Text = text, AnimationName = animName, VideoPath = null, IsFullscreenExample = isFullscreen };

		public static PageData WithVideo(string text, string videoPath, bool isFullscreen = false)
			=> new PageData { Text = text, AnimationName = null, VideoPath = videoPath, IsFullscreenExample = isFullscreen };
			
		public static PageData TextOnly(string text, bool isFullscreen = false)
			=> new PageData { Text = text, AnimationName = null, VideoPath = null, IsFullscreenExample = isFullscreen };
	}

	private RichTextLabel _tutorialText;
	private Control _nextSlot;
	private Button _nextButton;
	private Button _backButton;
	private Button _backToMenuButton; 
	
	// Toggle UI Elements
	private TextureRect _gradientBackground;
	private MarginContainer _tutorialContainer;
	private Button _backToTutorialButton; 
	private Label _explanationLabel; 

	private AnimatedSprite2D _spritePlayer;
	private VideoStreamPlayer _videoPlayer;
	
	private int _pageIndex = 0;
	private Tween _textTween; //Variable to hold our typing animation

	private PageData[] _pages =
	{
		PageData.WithAnim(
			"[b]Ohhh hello, master…[/b] welcome to the board, yes…\n\n",
			"default_board" 
		),
		PageData.WithAnim(
			"[b]The Game Goal:[/b]\n\n" + 
			"Your goal is to create a [b]continuous path of tiles linking your home bases[/b]" +
			" before your opponent does [See Example Next Page].\n\n",
			"default_board" 
		),
		PageData.WithAnim(
			"",
			"win_path",
			true // Triggers the fullscreen UI toggle
		),
		PageData.WithVideo(
				"[b]Capturing:[/b]\n" +
			"You can [b]conquer an opponent’s tile[/b] by crossing diagonally over it\n\n",
            "res://UI_assets/Tutorial/menu_assets/videos/crossing_tile.ogv" 
		),
		PageData.WithVideo(
			"[b]Attacking:[/b]\n" +
			"If you walk through an opponent's character,[b] horizontally or vertically, [/b] you will remove them from the board.\n\n",
            "res://UI_assets/Tutorial/menu_assets/videos/character_attack.ogv" 
		),
		PageData.WithVideo(
			"[b]Home bases:[/b]\n" +
			"Characters may cross diagonally over [b]opponent's home bases[/b], but no tiles are placed there.\n\n",
			"res://UI_assets/Tutorial/menu_assets/videos/home_bases.ogv" 
		),
		PageData.WithVideo(
			"[b]Draws:[/b]\n" +
			"The game is a [b]draw[/b] if:\n" +
			"1. You remove all the opponent's characters from the board\n" +
			"2. Players are stuck in a loop, repeating the same moves 3 times\n",
			"res://UI_assets/Tutorial/menu_assets/videos/draw_attack.ogv"
		),
		PageData.WithAnim(
			 "[b]Winning the game:[/b]\n\n" +
			"Remember precious...You [b]win[/b] by forming a continuous path of tiles linking your home bases.\n" +
			"So...use all of your team's characters to conquer tiles quickly!",
			"win_path"
		),
		PageData.WithAnim("[b]Wish you luck in the game, my precious....[/b]\n\n", "win_path"),
		
	};
	
	public override void _Ready()
	{
		// Safely Initialize Nodes
		_tutorialText = GetNodeOrNull<RichTextLabel>("%TutorialText");
		_nextButton = GetNodeOrNull<Button>("%Next");
		_nextSlot =  GetNodeOrNull<Control>("%NextSlot");
		_backButton = GetNodeOrNull<Button>("%Back");
		
		_spritePlayer = GetNodeOrNull<AnimatedSprite2D>("%TutorialSprite");
		_videoPlayer = GetNodeOrNull<VideoStreamPlayer>("%TutorialVideo");

		// Initialize Toggle Nodes safely
		_gradientBackground = GetNodeOrNull<TextureRect>("%GradientBackground");
		_tutorialContainer = GetNodeOrNull<MarginContainer>("%TutorialContainer");
		_backToTutorialButton = GetNodeOrNull<Button>("%BackToTutorial"); 
		_backToMenuButton = GetNodeOrNull<Button>("%BackToMenu");
		
		_explanationLabel = GetNodeOrNull<Label>("%explanationLabel"); 

		// Connections
		if (_nextButton != null) _nextButton.Pressed += OnNextPressed;
		if (_backButton != null) _backButton.Pressed += OnBackPressed;
		
		// Setup the BackToTutorial Button
		if (_backToTutorialButton != null)
		{
			_backToTutorialButton.Pressed += OnNextPressed; 
		}

		// Setup the BackToMenu Button
		if (_backToMenuButton != null)
		{
			_backToMenuButton.Pressed += OnBackToMenuPressed;
		}

		UpdatePage();
	}

	private void OnExitButtonPressed()
	{
		Globals.isHelpClosed = true;
		Visible = false;
		_pageIndex = 0;
		UpdatePage();
	}

	private void OnBackToMenuPressed()
	{
		// Grab the UIManager directly from the Singleton
		UIManager currentUI = UIManager.Instance;
					
		if (currentUI != null)
		{
			// Tell the UI Manager to use its built-in back stack!
			currentUI.GoBack();
		}
		else
		{
			GD.PrintErr("CRITICAL ERROR: UIManager.Instance is completely missing!");
		}
	}
	
	private void OnNextPressed()
	{
		_pageIndex = Math.Min(_pageIndex + 1, _pages.Length - 1);
		UpdatePage();
	}
	
	private void OnBackPressed()
	{
		// Step back by one
		_pageIndex = Math.Max(_pageIndex - 1, 0);

		// Skip any fullscreen examples when going backward
		while (_pageIndex > 0 && _pages[_pageIndex].IsFullscreenExample)
		{
			_pageIndex--;
		}

		UpdatePage();
	}

	private void UpdatePage()
	{
		PageData currentPage = _pages[_pageIndex];
		
		if (_tutorialText != null)
		{
			_tutorialText.Text = currentPage.Text;

			// --- NEW: Typing Effect Logic ---
			// 1. Kill the previous animation if it's still running
			if (_textTween != null && _textTween.IsValid())
			{
				_textTween.Kill();
			}

			// 2. Hide all the text instantly
			_tutorialText.VisibleRatio = 0f;

			// 3. Create a new Tween for the current page
			_textTween = CreateTween();

			// 4. Calculate animation duration based on text length (e.g., 0.02 seconds per character)
			int textLength = string.IsNullOrEmpty(currentPage.Text) ? 0 : currentPage.Text.Length;
			float typingSpeed = 0.02f; // Make this smaller for faster typing, larger for slower
			float duration = textLength * typingSpeed;

			// 5. Animate the 'visible_ratio' property from 0.0 to 1.0
			_textTween.TweenProperty(_tutorialText, "visible_ratio", 1.0f, duration);
		}

		// Handle Visuals
		UpdateVisuals(currentPage);

		// --- TOGGLE FULLSCREEN UI LOGIC ---
		bool isFullscreen = currentPage.IsFullscreenExample;

		if (_gradientBackground != null) _gradientBackground.Visible = !isFullscreen;
		if (_tutorialContainer != null) _tutorialContainer.Visible = !isFullscreen;
		
		// Elements that ONLY show during fullscreen
		if (_backToTutorialButton != null) _backToTutorialButton.Visible = isFullscreen;
		
		// --- NEW: Toggle the Explanation Label ---
		if (_explanationLabel != null) _explanationLabel.Visible = isFullscreen;

		// --- BUTTON VISIBILITY LOGIC ---
		bool isFirstPage = _pageIndex == 0;
		bool isLastPage = _pageIndex == _pages.Length - 1;

		if (_backButton != null) _backButton.Visible = !isFirstPage && !isFullscreen;
		if (_nextSlot != null) _nextSlot.Visible = !isLastPage && !isFullscreen;

		// --- NEW: Back to Menu Button Visibility ---
		// Only show this button on the very last page
		if (_backToMenuButton != null) _backToMenuButton.Visible = isLastPage && !isFullscreen;
	}

	private void UpdateVisuals(PageData data)
	{
		// Reset all
		if (_spritePlayer != null) _spritePlayer.Visible = false;
		if (_videoPlayer != null)
		{
			_videoPlayer.Visible = false;
			_videoPlayer.Stop();
		}

		if (!string.IsNullOrEmpty(data.AnimationName))
		{
			if (_spritePlayer != null)
			{
				_spritePlayer.Visible = true;
				_spritePlayer.Play(data.AnimationName);
			}
		}
		else if (!string.IsNullOrEmpty(data.VideoPath))
		{
			if (_videoPlayer != null)
			{
				_videoPlayer.Visible = true;
				_videoPlayer.Stream = GD.Load<VideoStream>(data.VideoPath);
				_videoPlayer.Play();
			}
		}
	}
}
