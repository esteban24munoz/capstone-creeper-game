using Godot;
using System;

public partial class MainMenu : Control
{
	private UIManager _ui;
	private AnimatedSprite2D _eyeTower;

	private string[] _eyeAnimations = { "position1", "position2", "position3", "position4" };
	private const string IdleAnimation = "idle";
	
	// Screen setup
	private const float SectionWidth = 256.0f; // 1024 / 4
	
	// Idle Logic Variables
	private Vector2 _lastMousePos;
	private double _timeSinceLastMove = 0.0;
	private const double IdleThreshold = 4.0; // Seconds to wait

	public override void _Ready()
	{
		_eyeTower = GetNode<AnimatedSprite2D>("EyeTower"); 

		_ui = UIManager.Instance;

		if (_ui == null)
		{
			GD.PrintErr("MainMenu: UIManager Instance is null! Is MainUI.tscn loaded?");
			return;
		}
		
		// Initialize position so it doesn't snap immediately on start
		_lastMousePos = GetViewport().GetMousePosition();

		GetNode<Button>("%StartButton").Pressed += () => 
			_ui.ShowScreen("res://GameUI_scenes/storyVideo.tscn");

		GetNode<Button>("%TutorialButton").Pressed += () => 
			_ui.ShowScreen("res://GameUI_scenes/tutorialScreen.tscn");
			
		GetNode<Button>("%SettingsButton").Pressed += () => 
			GetNode<Control>("SettingsMenu").Visible = true;
			
		GetNode<Button>("%CreditsButton").Pressed += () => 
			_ui.ShowScreen("res://GameUI_scenes/creditsScreen.tscn");

		GetNode<Button>("%QuitButton").Pressed += () => GetTree().Quit();
	}

	public override void _Process(double delta)
	{
		UpdateEyeTracking(delta);
	}

	private void UpdateEyeTracking(double delta)
	{
		if (_eyeTower == null) return;

		Vector2 currentMousePos = GetViewport().GetMousePosition();

		// Check if mouse has moved
		// We use a tiny threshold (0.1) just to handle floating point jitters
		if (currentMousePos.DistanceTo(_lastMousePos) > 0.1f)
		{
			// --- MOUSE MOVED ---
			_timeSinceLastMove = 0.0;       // Reset timer
			_lastMousePos = currentMousePos; // Update last known position
			
			// Calculate tracking immediately (Eye wakes up)
			PlayTrackingAnimation(currentMousePos.X);
		}
		else
		{
			// --- MOUSE STATIONARY ---
			_timeSinceLastMove += delta; // Count up

			if (_timeSinceLastMove >= IdleThreshold)
			{
				// Trigger Idle
				if (_eyeTower.Animation != IdleAnimation)
				{
					_eyeTower.Play(IdleAnimation);
				}
			}
		}
	}

	private void PlayTrackingAnimation(float mouseX)
	{
		// 1. Calculate section index (0 to 3)
		int sectionIndex = (int)(mouseX / SectionWidth);
		sectionIndex = Mathf.Clamp(sectionIndex, 0, 3);

		// 2. Play the correct directional animation
		string targetAnim = _eyeAnimations[sectionIndex];
		
		if (_eyeTower.Animation != targetAnim)
		{
			_eyeTower.Play(targetAnim);
		}
	}
}
