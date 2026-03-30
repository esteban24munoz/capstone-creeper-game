using Godot;
using System;
using System.Threading.Tasks;

public partial class DisconnectModal : Control
{
	private UIManager _ui;

	public override async void _Ready()
	{
		_ui = UIManager.Instance;
		
		if (Globals.gameType == Globals.GameType.Network)
		{
			await DisconnectCheckLoop();
			Setup();
		}
	}
	
	private async Task DisconnectCheckLoop()
	{
		while (Globals.status != "disconnected")
		{
			await Task.Delay(500);
		}
		return;
	}

	// Call this to show the modal
	private void Setup()
	{
		Visible = true; // Show the modal
	}

	private async void OnMenuPressed()
	{
		Visible = false; // Hide this menu
		await _ui.ReturnToMenu("res://GameUI_scenes/mainMenu.tscn");
	}

	private async void OnRestartPressed()
	{
		Visible = false; 
		await _ui.RestartGame();
	}
}
