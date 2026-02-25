using Godot;
using System;
using Client;
using System.Threading;

public partial class Globals : Node
{
	public static string ServerBaseUrl = "http://10.30.208.129:8000";
	public static System.Net.Http.HttpClient http = new System.Net.Http.HttpClient { BaseAddress = new Uri(ServerBaseUrl) };
	public static string username = "";
	public static string gameId;
	public static CancellationTokenSource cts = new CancellationTokenSource();
	public static HostClient hostClient = new HostClient(http);
	public static GuestClient guestClient = new GuestClient(http);
	public static string hostToken;
	public static string guestToken;
	public static string gameType; //Local, AI, Network
	public static string p1Type; //Person, AI, Network
	public static string p2Type; //Person, AI, Network
	public static string status;
	public static string winner;
}
