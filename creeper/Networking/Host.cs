using Godot;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Client {
	public class HostClient
	{
		private readonly System.Net.Http.HttpClient _http;
		private readonly JsonSerializerOptions _jsonOptions = new()
		{
			PropertyNameCaseInsensitive = true
		};
		
		public HostClient(System.Net.Http.HttpClient http)
		{
			_http = http;
		}
		
		public async Task<GameCreatedResponse> CreateGameAsync(string displayName, CancellationToken ct = default)
		{
			var payload = new { display_name = displayName };
			var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
			var resp = await _http.PostAsync("/games", content, ct);
			resp.EnsureSuccessStatusCode();
			var body = await resp.Content.ReadAsStringAsync(ct);
			return JsonSerializer.Deserialize<GameCreatedResponse>(body, _jsonOptions)!;
		}
	}
	
	public class GameCreatedResponse
	{
		[JsonPropertyName("game_id")]
		public string GameId { get; set; } = default!;

		[JsonPropertyName("host_token")]
		public string HostToken { get; set; } = default!;

		[JsonPropertyName("status")]
		public string Status { get; set; } = default!;
	}
	
	public partial class Host : Node
	{
		[Export] public string ServerBaseUrl { get; set; } = "http://localhost:8000";
		[Export] public string HostDisplayName { get; set; } = "GodotHost";
		private HostClient _client = null!;
		private CancellationTokenSource _cts = null!;
		private GameCreatedResponse? _created;

		public override void _Ready()
		{
			// Create HttpClient with base address. Keep a single instance.
			var http = new System.Net.Http.HttpClient { BaseAddress = new Uri(ServerBaseUrl) };
			_client = new HostClient(http);
			_cts = new CancellationTokenSource();
			
			SetUpInfo();
			
			// Start background flow without blocking Godot main thread.
			//_ = StartHostFlowAsync();
		}
		
		private void SetUpInfo()
		{
			Label p1Name = GetNode<Label>("P1/P1name");
			GD.Print(p1Name.Text);
			GD.Print(Globals.username);
			p1Name.Text = Globals.username;
		}
		
		private async Task StartHostFlowAsync()
		{
			try
			{
				// 1) Create game
				_created = await _client.CreateGameAsync(HostDisplayName, _cts.Token);
				GD.Print($"[Host] Created game {_created.GameId} token={_created.HostToken} status={_created.Status}");
				Label id = GetNode<Label>("GameID/ID");
				id.Text = _created.GameId;

				//// 2) Start heartbeat loop (run concurrently)
				//_ = HeartbeatLoopAsync(_created, _cts.Token);
//
				//// Example: poll state periodically and optionally make a move.
				//while (!_cts.Token.IsCancellationRequested)
				//{
					//try
					//{
						//var state = await _client.GetGameStateAsync(_created.GameId, _cts.Token);
						//GD.Print($"[Host] Game status: {state.Status}, turn: {state.Turn}, lastActive: {state.LastActive}");
//
						//// Example: make a sample move when it's host's turn.
						//if (state.Status == "in_progress" && state.Turn == "host")
						//{
							//// Replace with your real state string
							//var exampleState = ".oo.xx...";
							//await _client.MakeMoveAsync(_created.GameId, _created.HostToken, exampleState, _cts.Token);
							//GD.Print("[Host] Submitted a move.");
						//}
					//}
					//catch (Exception ex)
					//{
						//GD.PrintErr($"[Host] Poll error: {ex.Message}");
					//}
//
					//await Task.Delay(TimeSpan.FromSeconds(5), _cts.Token);
				//}
			}
			catch (Exception ex)
			{
				GD.PrintErr($"[Host] Initialization error: {ex.Message}");
			}
		}
	}
}
