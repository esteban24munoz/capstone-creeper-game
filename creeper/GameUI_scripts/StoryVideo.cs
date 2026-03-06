using Godot;
using System;

public partial class StoryVideo : Control
{
	private const string GameModePath = "res://GameUI_scenes/gameMode.tscn";
	private VideoStreamPlayer _videoPlayer;

	private Button _skipButton;

	private UIManager _ui;



	public override void _Ready()
	{

	_ui = UIManager.Instance;
	
	if (_ui != null)
	{
		_ui.StopMusic();
	}
	_videoPlayer = GetNode<VideoStreamPlayer>("%TrailerVideo");
	_skipButton = GetNode<Button>("%SkipButton");

	// 1. When video ends naturally
	_videoPlayer.Finished += OnVideoFinished;

	// 2. When user presses skip
	_skipButton.Pressed += OnSkipPressed;

	_videoPlayer.Play();
	}

	private void OnVideoFinished()
	{
	GoToGame();
	}

	private void OnSkipPressed()
	{
	// Stop the video so audio doesn't keep playing during transition
	_videoPlayer.Stop();
	GoToGame();
	}

	private void GoToGame()
	{
		if (_ui != null)
		{
			_ui.PlayMusic();
			//replaces StoryVideo with GameMode in the history
			_ui.SwitchScreen(GameModePath); 
		}
		else
		{
			GetTree().ChangeSceneToFile(GameModePath);
		}
	}

}
