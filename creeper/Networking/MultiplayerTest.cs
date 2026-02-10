using Godot;
using System;
using System.Threading.Tasks;
using System.Text;

public partial class MultiplayerTest : Node2D
{
	[Export]
	private int port = 7000;
	[Export]
	private string address = "0.0.0.0"; //Need to figure out a perminate address to use
	private ENetMultiplayerPeer peer;
	private bool isHost;
	private int UDPport = 9999;
	private PacketPeerUdp udp = new PacketPeerUdp();
	private string broadcastAddress = "255.255.255.255";
	private double timer = 0;
	
	public override void _Ready()
	{
		Multiplayer.PeerConnected += PeerConnected;
		Multiplayer.PeerDisconnected += PeerDisconnected;
		Multiplayer.ConnectedToServer += ConnectedToServer;
		Multiplayer.ConnectionFailed += ConnectionFailed;
		Multiplayer.ServerDisconnected += ServerDisconnected;
		
		GD.Print("Multiplayer Menu");
		var addresses = IP.GetLocalAddresses();
		foreach (var address in addresses)
		{
			GD.Print(address);
		}
		int lastaddr = addresses.Length;
		address = addresses[lastaddr-1].ToString();
		GD.Print("Host IP should be " + address);
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
	
	//Host using UDP broadcasting
	private void _on_host_btn_pressed()
	{
		GD.Print("Hosting Game");
		isHost = true;
		
		udp.Bind(UDPport);
		//udp.SetBroadcastEnabled(true);
		udp.SetDestAddress(broadcastAddress, UDPport);
		
		HostGame();
	}
	
	//Join using UDP broadcasting
	private void _on_join_btn_pressed()
	{
		GD.Print("Joining Game");
		isHost = false;
		//bind to all interfaces to listen for the broadcast
		udp.Bind(UDPport);
	}
	
	//Needed for UDP broadcasting functionality
	public override void _Process(double delta)
	{
		if (isHost)
		{
			HostProcess(delta);
		}
		else
		{
			ClientProcess(delta);
		}
	}
	
	public void HostProcess(double delta)
	{
		timer += delta;
		if (timer > 3.0) //Broadcast every 3 second
		{
			timer = 0;
			string message = $"Server_Discover_v1: {address}";
			byte[] packet = Encoding.UTF8.GetBytes(message);
			udp.PutPacket(packet);
		}
	}
	
	public void ClientProcess(double delta)
	{
		if (udp.GetAvailablePacketCount() > 0)
		{
			string hostIp = udp.GetPacketIP();
			byte[] packetData = udp.GetPacket();
			string message = Encoding.UTF8.GetString(packetData);
			
			if (message.Contains("Server_Discover_v1: "))
			{
				hostIp = message.Substring(20);
				GD.Print($"Found host at: {hostIp}");
				//Can now use hostIP to connect via ENetMultiplayerPeer
				StopDiscover();
				JoinGame(hostIp);
			}
		}
	}
	
	private void StopDiscover()
	{
		udp.Close();
		SetProcess(false);
	}
	
	//Host using high level Multiplayer
	private void HostGame()
	{
		GD.Print("Hosting game Function");
		GD.Print(peer);
		if (peer != null)
		{
			peer.Close();
			peer = null;
		}
		Multiplayer.MultiplayerPeer = null;
		udp = null;
		
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
		
		peer.Host.Compress(ENetConnection.CompressionMode.RangeCoder);
		
		Multiplayer.MultiplayerPeer = peer;
		GD.Print("Waiting for players");
	}
	
	//Join using high level Multiplayer
	private void JoinGame(string hostIP)
	{
		GD.Print("Joining game Function");
		peer = new ENetMultiplayerPeer();
		var error = peer.CreateClient(hostIP, port, 0, 0, 2);
		
		if (error != Error.Ok)
		{
			GD.Print($"Error connecting to server: {error}");
			peer = null;
			return;
		}
		else
		{
			GD.Print("Connected to server");
		}
		
		peer.Host.Compress(ENetConnection.CompressionMode.RangeCoder);
		
		Multiplayer.MultiplayerPeer = peer;
	}
}
