using Godot;
using System;
using System.Threading.Tasks;

public partial class MultiplayerTest : Node2D
{
	[Export]
	private int port = 9999;
	[Export]
	private string address = "127.0.0.1";
	private ENetMultiplayerPeer peer;
	
	public override void _Ready()
	{
		Multiplayer.PeerConnected += PeerConnected;
		Multiplayer.PeerDisconnected += PeerDisconnected;
		Multiplayer.ConnectedToServer += ConnectedToServer;
		Multiplayer.ConnectionFailed += ConnectionFailed;
		Multiplayer.ServerDisconnected += ServerDisconnected;
		
		GD.Print("Multiplayer Menu");
	}
	
	private void ServerDisconnected()
	{
		if (peer != null)
		{
			peer.Close();
			peer = null;
		}
		GD.Print("Server Disconnected");
		Multiplayer.MultiplayerPeer = null;
	}
	
	private void ConnectionFailed()
	{
		GD.Print("Connection Failed");
	}
	
	private void ConnectedToServer()
	{
		GD.Print("Connected to server");
	}
	
	private void PeerDisconnected(long id)
	{
		GD.Print("Player disconnected: " + id.ToString());
		Multiplayer.MultiplayerPeer = null;
	}
	
	private void PeerConnected(long id)
	{
		GD.Print("Player Connected: " + id.ToString());
		//Start the game
	}
	
	private void _on_back_btn_pressed()
	{
		if (peer != null)
		{
			peer.Close();
			peer = null;
		}
		Multiplayer.MultiplayerPeer = null;
		GetTree().ChangeSceneToFile("res://game.tscn");
	}
	
	private void _on_ai_btn_pressed()
	{
		Multiplayer.MultiplayerPeer = null;
		GD.Print("AI Tournament");
		GetTree().ChangeSceneToFile("res://Networking/AIvAI_test.tscn");
	}
	
	private void _on_host_btn_pressed()
	{
		GD.Print("Hosting game");
		GD.Print(peer);
		if (peer != null)
		{
			peer.Close();
			peer = null;
		}
		Multiplayer.MultiplayerPeer = null;
		
		peer = new ENetMultiplayerPeer();
		var error = peer.CreateServer(port, 2);
		
		if (error != Error.Ok)
		{
			GD.Print("error connot host! : " + error.ToString());
			peer = null;
			return;
		}
		else
		{
			GD.Print("Server created");
		}
		
		Multiplayer.MultiplayerPeer = peer;
		GD.Print("Waiting for players");
	}
	
	private void _on_join_btn_pressed()
	{
		GD.Print("Joining game");
	}
}
