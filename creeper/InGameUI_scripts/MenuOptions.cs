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
		
		GetNode<Button>("%ExitButton").Pressed += () =>
		{
			Visible = false;
		};

		GetNode<Button>("%RestartGame").Pressed += async () =>
		{
			Visible = false; // Hide this options menu
			
			// Destroys the game and unhides the UIManager, 
			// automatically showing whatever screen was there before!
			await _ui.RestartGame();
		};

		GetNode<Button>("%SettingsButton").Pressed += () =>
		{
			GetNode<Control>("%SettingsMenu").Visible = true;
		};

		GetNode<Button>("%MainMenu").Pressed += async () =>
		{
			Visible = false; // Hide this menu
			// Pass the path to your main menu scene here
			await _ui.ReturnToMenu("res://GameUI_scenes/mainMenu.tscn");
		};
	}
}
