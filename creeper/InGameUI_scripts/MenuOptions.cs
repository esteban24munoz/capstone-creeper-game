using Godot;
using System; 

public partial class MenuOptions : Control
{
	private UIManager _ui;
	
	private ConfirmationModalInGame _confirmationModal;

	public override void _Ready()
	{
		_ui = UIManager.Instance;

		// Get the reference to the modal in your scene tree
		_confirmationModal = GetNode<ConfirmationModalInGame>("%ConfirmationModal");

		GetNode<Button>("%ResumeGame").Pressed += () =>
		{
			Visible = false;
		};
		
		GetNode<Button>("%ExitButton").Pressed += () =>
		{
			Visible = false;
		};

		// --- RESTART GAME ---
		GetNode<Button>("%RestartGame").Pressed += () =>
		{
			// Open the modal and pass the specific Restart logic
			_confirmationModal.Setup(
				"Are you sure you want to restart?", 
				async () => 
				{
					Visible = false; // Hide this options menu
					await _ui.RestartGame();
				}
			);
		};

		GetNode<Button>("%SettingsButton").Pressed += () =>
		{
			GetNode<Control>("%SettingsMenu").Visible = true;
		};

		// --- MAIN MENU ---
		GetNode<Button>("%MainMenu").Pressed += () =>
		{
			// Open the modal and pass the specific Main Menu logic
			_confirmationModal.Setup(
				"Quit to Main Menu?", 
				async () => 
				{
					Visible = false; // Hide this menu
					await _ui.ReturnToMenu("res://GameUI_scenes/mainMenu.tscn");
				}
			);
		};
	}
}
