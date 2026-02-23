using Godot;

public partial class MenuOptions : Control
{
	private UIManager _ui;

	public override void _Ready()
	{
		_ui = UIManager.Instance;

		GetNode<Button>("%ResumeGame").Pressed += () =>
		{
			Visible = false;
		};

		GetNode<Button>("%RestartGame").Pressed += () =>
		{
			_ui.ShowScreen("res://GameUI_scenes/gameMode.tscn");
		};

		GetNode<Button>("%SettingsMenu").Pressed += () =>
		{
			_ui.ShowScreen("res://GameUI_scenes/tutorialScreen.tscn");
		};

		GetNode<Button>("%MainMenu").Pressed += () =>
		{
			_ui.ShowScreen("res://GameUI_scenes/mainMenu.tscn");
		};
	}
}
