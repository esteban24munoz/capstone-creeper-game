using Godot;
using System;
using System.Threading.Tasks;
using System.Text;

public partial class MultiplayerTest : Node2D
{
	private string username = "";
	
	public override void _Ready()
	{
		
	}
	
	private void _on_username_text_changed(string text)
	{
		Label usernameLabel = GetNode<Label>("Control/UserLabel");
		usernameLabel.Text = text;
		username = text;
	}
	
	private void _on_back_btn_pressed()
	{
		GetTree().ChangeSceneToFile("res://game.tscn");
	}
	
	private void _on_ai_btn_pressed()
	{
		GD.Print("AI Tournament");
		GetTree().ChangeSceneToFile("res://Networking/AIvAI_test.tscn");
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
}
