using Godot;
using System; 

public partial class MenuOptions : Control
{
	[Signal] public delegate void EndButtonPressedEventHandler();

	private UIManager _ui;
	
	private ConfirmationModalInGame _confirmationModal;

	public override void _Ready()
	{
		_ui = UIManager.Instance;

		// Get the reference to the modal in your scene tree
		_confirmationModal = GetNode<ConfirmationModalInGame>("%ConfirmationModal");

		if (Globals.gameType == Globals.GameType.Network)
		{
			GetNode<Button>("%EndButton").Visible = false;
		}
		else
		{
			GetNode<Button>("%EndButton").Visible = true;
		}

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

		// --- END GAME ---
		GetNode<Button>("%EndButton").Pressed += () =>
		{
			Visible = false;
			EmitSignal(SignalName.EndButtonPressed);
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
