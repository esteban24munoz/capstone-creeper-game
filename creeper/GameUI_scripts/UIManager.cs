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
	private ColorRect _overlay;
	private bool _isTransitioning = false;

	public override void _Ready()
	{
		Instance = this; //global reference
		_container = GetNode<Control>("ScreenContainer");
		
		_backButton = GetNode<Button>("buttonsMargin/StillButtonsContainer/BackButton");

		_overlay = GetNode<ColorRect>("TransitionLayer/ColorRect");

		_overlay.Visible = false;
		_overlay.Modulate = new Color(1, 1, 1, 0);

		_backButton.Pressed += GoBack;

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

	private async void GoBack()
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

	private void UpdateBackButton()
	{
		_backButton.Visible = _screenStack.Count > 1;
	}
}
