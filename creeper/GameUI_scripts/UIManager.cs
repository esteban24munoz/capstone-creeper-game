using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class UIManager : Control
{
	public static UIManager Instance { get; private set; }
	
	private Stack<Control> _screenStack = new();
	private Control _container;
	private Button _backButton;
	private Button _menuButton;
	private ColorRect _overlay;
	private bool _isTransitioning = false;
	private Node _currentGameInstance;

	public override void _Ready()
	{
		Instance = this; //global reference
		
		_container = GetNode<Control>("%ScreenContainer");
		_backButton = 	GetNode<Button>("%BackButton");

		_menuButton = GetNode<Button>("%MenuButton");
		_overlay = GetNode<ColorRect>("TransitionLayer/ColorRect");
		_overlay.Visible = false;
		_overlay.Modulate = new Color(1, 1, 1, 0);

		_backButton.Pressed += GoBack;
		_menuButton.Pressed += ShowSettingsMenu;
		
		CallDeferred(MethodName.ShowScreen, "res://GameUI_scenes/mainMenu.tscn", true);
	}

	private void ShowInitialScreen()
	{
		ShowScreen("res://GameUI_scenes/mainMenu.tscn", true);
	}

	public async void ShowScreen(string path, bool clearStack = false)
	{
		if (_isTransitioning) return;
		_isTransitioning = true;

		// Start Transition
		await FadeOut();

		if (clearStack)
		{
			// Clear existing children from the container manually
			foreach (Node child in _container.GetChildren())
			{
				child.QueueFree();
			}
			_screenStack.Clear();
		}
		else if (_screenStack.Count > 0)
		{
			// Just hide the top screen if we aren't clearing
			_screenStack.Peek().Visible = false;
		}

		// Load and Instance
		var scene = GD.Load<PackedScene>(path);
		if (scene == null)
		{
			GD.PrintErr($"Failed to load scene at: {path}");
			_isTransitioning = false;
			return;
		}

		var screenInstance = scene.Instantiate<Control>();
		_container.AddChild(screenInstance);
		_screenStack.Push(screenInstance);

		UpdateBackButton();
		
		// End Transition
		await FadeIn();
		_isTransitioning = false;
	}
	
	public async Task ChangeSceneWithTransition(string path) 
	{ 
		if (_isTransitioning) return; 
		_isTransitioning = true; 
		
		await FadeOut(); 

		// 1. Clean up the existing game instance if we are restarting
		if (_currentGameInstance != null && GodotObject.IsInstanceValid(_currentGameInstance))
		{
			_currentGameInstance.QueueFree();
			_currentGameInstance = null;
		}

		// 2. Load and Instantiate the Game Scene manually
		var scene = GD.Load<PackedScene>(path);
		if (scene != null)
		{
			_currentGameInstance = scene.Instantiate();
			
			// Add the game to the root of the SceneTree
			GetTree().Root.AddChild(_currentGameInstance);
			
			// Hide the UI screens so the game is visible underneath
			_container.Visible = false; 
			_backButton.Visible = false;
			_menuButton.Visible = false;
		}
		else 
		{
			GD.PrintErr($"Failed to load game scene at: {path}");
		}

		await FadeIn(); 
		_isTransitioning = false;
	}
	
	public async Task RestartGame()
{
	if (_isTransitioning) return;
	_isTransitioning = true;

	await FadeOut();

	// 1. Destroy the running game
	if (_currentGameInstance != null && GodotObject.IsInstanceValid(_currentGameInstance))
	{
		_currentGameInstance.QueueFree();
		_currentGameInstance = null;
	}

	// 2. Unhide the UI container. 
	// Since we never cleared the stack when starting the game, 
	// the previous screen (like Team Selection) will instantly be visible again!
	_container.Visible = true;
	
	// 3. Restore the correct state for the Top Bar buttons
	UpdateBackButton(); 

	await FadeIn();
	_isTransitioning = false;
	}
	
	// We add a path parameter so it knows exactly which menu to load
	public async Task ReturnToMenu(string mainMenuPath)
	{
		if (_isTransitioning) return;
		_isTransitioning = true;

		await FadeOut();

		// 1. Destroy the running game
		if (_currentGameInstance != null && GodotObject.IsInstanceValid(_currentGameInstance))
		{
			_currentGameInstance.QueueFree();
			_currentGameInstance = null;
		}

		// 2. Clear the UI stack so we don't see Team Selection anymore
		foreach (Node child in _container.GetChildren())
		{
			child.QueueFree();
		}
		_screenStack.Clear();

		// 3. Load and instance the Main Menu
		var scene = GD.Load<PackedScene>(mainMenuPath);
		if (scene != null)
		{
			var screenInstance = scene.Instantiate<Control>();
			_container.AddChild(screenInstance);
			_screenStack.Push(screenInstance);
		}
		else
		{
			GD.PrintErr($"Failed to load main menu at: {mainMenuPath}");
		}

		// 4. Make the UI container visible again
		_container.Visible = true;
		UpdateBackButton(); 

		await FadeIn();
		_isTransitioning = false;
	}
	

	private async Task FadeOut()
	{
		_overlay.Visible = true;
		var tween = CreateTween();
		// Uses "SetTrans" to make the fade feel smoother
		tween.SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
		tween.TweenProperty(_overlay, "modulate:a", 1.0f, 0.25f);
		await ToSignal(tween, "finished");
	}

	private async Task FadeIn()
	{
		var tween = CreateTween();
		tween.SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
		tween.TweenProperty(_overlay, "modulate:a", 0.0f, 0.25f);
		await ToSignal(tween, "finished");
		_overlay.Visible = false;
	}

	public async void GoBack()
	{
		if (_screenStack.Count <= 1 || _isTransitioning) return;

		_isTransitioning = true;
		await FadeOut();

		var current = _screenStack.Pop();
		current.QueueFree();

		// Show the previous screen
		var previous = _screenStack.Peek();
		previous.Visible = true;

		UpdateBackButton();
		await FadeIn();
		_isTransitioning = false;
	}
	
	private void ShowSettingsMenu(){
		GD.Print("Settings menu has been opened!");
		GetNode<Control>("%SettingsMenu").Visible = true;
	}

	private void UpdateBackButton()
	{
		_backButton.Visible = _screenStack.Count > 1;
		_menuButton.Visible = _screenStack.Count > 1;
	}
}
