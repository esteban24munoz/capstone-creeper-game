using Godot;
using System;
using System.Threading.Tasks;
using System.Text;

public partial class Multiplayer : Control
{
	private string username = "";
	private UIManager _ui;
	private Label errorMessage;
	
	public override void _Ready()
	{
		Globals.gameType = Globals.GameType.Network;
		errorMessage = GetNode<Label>("%ErrorMessage");
		_ui = UIManager.Instance;

		if (_ui == null)
		{
			GD.PrintErr("GameMode: UIManager Instance is null! Is MainUI.tscn loaded?");
			return;
		}
	}
	
	private void _on_username_text_changed(string text)
	{
		username = text;
		errorMessage.Visible = false;
	}
	
	private void _on_ai_btn_pressed()
	{
		GD.Print("AI Tournament");
		_ui.ShowScreen("res://Networking/AIvAI.tscn");
	}
	
	private void _on_host_btn_pressed()
	{
		//Check for a username.
		if (string.IsNullOrWhiteSpace(username)) {
			errorMessage.Visible = true;
			return;
		}
		
		Globals.username = username;
		_ui.ShowScreen("res://Networking/host.tscn");
	}
	
	private void _on_join_btn_pressed()
	{
		//Check for a username.
		if (string.IsNullOrWhiteSpace(username)) {
			errorMessage.Visible = true;
			return;
		}
		
		Globals.username = username;
		_ui.ShowScreen("res://Networking/guest.tscn");
	}
}
