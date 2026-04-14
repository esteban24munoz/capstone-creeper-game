using Godot;
using System;
using Client;
using System.Threading;

public partial class Globals : Node
{
	public enum GameType
	{
		Local,
		AI,
		Network
	}
	
	public enum AIDifficulty
	{
		Easy,
		Medium,
		Hard
	}
	
	public static string ServerBaseUrl = "http://10.30.208.216:8000";
	public static System.Net.Http.HttpClient http = new System.Net.Http.HttpClient { BaseAddress = new Uri(ServerBaseUrl) };
	public static string username = "";
	public static string gameId;
	public static CancellationTokenSource cts = new CancellationTokenSource();
	public static HostClient hostClient = new HostClient(http);
	public static GuestClient guestClient = new GuestClient(http);
	public static string token;
	public static GameType gameType = GameType.Local;
	public static string status;
	public static string winner;
	public static AIDifficulty difficulty = AIDifficulty.Easy;
	public static bool isHelpClosed = false;
	public static bool isRepitionDraw;
}
