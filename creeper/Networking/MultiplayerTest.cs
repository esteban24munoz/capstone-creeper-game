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
		Globals.gameType = "Network";
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
	
	private void _on_back_btn_pressed()
	{
		_ui.ShowScreen("res://GameUI_scenes/gameMode.tscn");
	}
	
	private void _on_ai_btn_pressed()
	{
		GD.Print("AI Tournament");
		GetTree().ChangeSceneToFile("res://AI/AI_test.tscn");
	}
	
	private void _on_host_btn_pressed()
	{
		//Check for a username.
		if (username == "" || username == null) {
			return;
		}
		
		Globals.username = username;
		GD.Print(Globals.username);
		GetTree().ChangeSceneToFile("res://Networking/host_test.tscn");
	}
	
	private void _on_join_btn_pressed()
	{
		//Check for a username.
		if (username == "" || username == null) {
			return;
		}
		
		Globals.username = username;
		GD.Print(Globals.username);
		GetTree().ChangeSceneToFile("res://Networking/guest_test.tscn");
	}
}
