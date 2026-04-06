using Godot;
using System; 

public partial class EndOptions : Control
{
	[Signal] public delegate void EndMenuBackButtonPressedEventHandler();

	private UIManager _ui;
	
	private ConfirmationModalInGame _confirmationModal;

	public override void _Ready()
	{
		_ui = UIManager.Instance;

		// Get the reference to the modal in your scene tree
		_confirmationModal = GetNode<ConfirmationModalInGame>("%ConfirmationModal");

		GetNode<Button>("%BackButton").Pressed += () =>
		{
			Visible = false;
            EmitSignal(SignalName.EndMenuBackButtonPressed);
		};
		
		GetNode<Button>("%ExitButton").Pressed += () =>
		{
			Visible = false;
		};

		// --- FRODO WIN ---
		GetNode<Button>("%FrodoButton").Pressed += () =>
		{
			// Open the modal and pass the specific Frodo win logic
			_confirmationModal.Setup(
				"Are you sure you want to end the game?", 
			    () => 
				{
					Visible = false; // Hide this options menu
					_ui.EndGame(Constants.Player.Hero);
				}
			);
		};

        // --- SAURON WIN ---
		GetNode<Button>("%SauronButton").Pressed += () =>
		{
			// Open the modal and pass the specific Sauron win logic
			_confirmationModal.Setup(
				"Are you sure you want to end the game?", 
			    () => 
				{
					Visible = false; // Hide this options menu
					_ui.EndGame(Constants.Player.Enemy);
				}
			);
		};

        // --- DRAW ---
		GetNode<Button>("%DrawButton").Pressed += () =>
		{
			// Open the modal and pass the specific draw logic
			_confirmationModal.Setup(
				"Are you sure you want to end the game?", 
			    () => 
				{
					Visible = false; // Hide this options menu
					_ui.EndGame(Constants.Player.Draw);
				}
			);
		};
	}
}
