using Godot;
using System;

public partial class MainMenuOptions : Control
{
	
	
	private ConfirmationModalInGame _confirmationModal;

	public override void _Ready()
	{


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


		GetNode<Button>("%SettingsButton").Pressed += () =>
		{
			GetNode<Control>("%SettingsMenu").Visible = true;
		};

		GetNode<Button>("%SettingsButton").Pressed += () =>
		{
			GetNode<Control>("%SettingsMenu").Visible = true;
		};

// --- MAIN MENU ---
		GetNode<Button>("%MainMenu").Pressed += () =>
		{
			_confirmationModal.Setup(
				"Quit to Main Menu?", 
				async () => 
				{
					Visible = false; 
					
					// Grab the UIManager right now, directly from the Singleton!
					UIManager currentUI = UIManager.Instance;
					
					if (currentUI == null)
					{
						// If this happens, your UIManager is completely missing from the scene!
						GD.PrintErr("CRITICAL ERROR: UIManager.Instance is completely missing!");
						return;
					}

					GD.Print("0. Modal Yes clicked! Calling UIManager...");
					await currentUI.ReturnToMenu("res://GameUI_scenes/mainMenu.tscn");
				}
			);
		};
	}
}
