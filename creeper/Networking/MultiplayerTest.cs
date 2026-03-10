using Godot;
using System;
using System.Threading.Tasks;
using System.Text;

public partial class MultiplayerTest : Control
{
	private string username = "";
	private UIManager _ui;
	
	public override void _Ready()
	{
		Globals.gameType = Globals.GameType.Network;
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
	}
	
	private void _on_ai_btn_pressed()
	{
		GD.Print("AI Tournament");
		_ui.ShowScreen("res://AI/AI_test.tscn");
	}
	
	private void _on_host_btn_pressed()
	{
		//Check for a username.
		if (username == "" || username == null) {
			return;
		}
		
		Globals.username = username;
		GD.Print(Globals.username);
		_ui.ShowScreen("res://Networking/host_test.tscn");
	}
	
	private void _on_join_btn_pressed()
	{
		//Check for a username.
		if (username == "" || username == null) {
			return;
		}
		
		Globals.username = username;
		GD.Print(Globals.username);
		_ui.ShowScreen("res://Networking/guest_test.tscn");
	}
}
