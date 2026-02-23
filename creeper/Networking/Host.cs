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
		
		public async Task HeartbeatAsync(string gameId, string playerToken, CancellationToken ct = default)
		{
			var payload = new { player_token = playerToken };
			var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
			var resp = await _http.PostAsync($"/games/{gameId}/heartbeat", content, ct).ConfigureAwait(false);
			resp.EnsureSuccessStatusCode();
		}

		public async Task MakeMoveAsync(string gameId, string playerToken, string state, CancellationToken ct = default)
		{
			var payload = new { player_token = playerToken, state = state };
			var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
			var resp = await _http.PostAsync($"/games/{gameId}/move", content, ct).ConfigureAwait(false);
			resp.EnsureSuccessStatusCode();
		}

		public async Task<GameStateResponse> GetGameStateAsync(string gameId, CancellationToken ct = default)
		{
			var resp = await _http.GetAsync($"/games/{gameId}", ct).ConfigureAwait(false);
			resp.EnsureSuccessStatusCode();
			var body = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
			return JsonSerializer.Deserialize<GameStateResponse>(body, _jsonOptions)!;
		}

		public async Task<List<GameListItem>> ListGamesAsync(CancellationToken ct = default)
		{
			var resp = await _http.GetAsync("/games", ct).ConfigureAwait(false);
			resp.EnsureSuccessStatusCode();
			var body = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
			return JsonSerializer.Deserialize<List<GameListItem>>(body, _jsonOptions)!;
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
	
	public partial class Host : Control
	{
		private GameCreatedResponse? _created;

		public override void _Ready()
		{
			SetUpInfo();
			
			 //Start background flow without blocking Godot main thread.
			_ = StartHostFlowAsync();
		}
		
		private void SetUpInfo()
		{
			Label p1Name = GetNode<Label>("%P1name");
			GD.Print(Globals.username);
			p1Name.Text = Globals.username;
			Globals.p1Type = "Person";
			Globals.p2Type = "Network";
		}
		
		private async Task StartHostFlowAsync()
		{
			try
			{
				// 1) Create game
				_created = await Globals.hostClient.CreateGameAsync(Globals.username, Globals.cts.Token);
				GD.Print($"[Host] Created game {_created.GameId} token={_created.HostToken} status={_created.Status}");
				Label id = GetNode<Label>("%ID");
				id.Text = _created.GameId;
				Globals.gameId = _created.GameId;
				Globals.hostToken = _created.HostToken;
				Globals.status = _created.Status;

				// 2) Start heartbeat loop (run concurrently)
				_ = HeartbeatLoopAsync(Globals.cts.Token);

				// Example: poll state periodically and optionally make a move.
				while (!Globals.cts.Token.IsCancellationRequested)
				{
					try
					{
						var state = await Globals.hostClient.GetGameStateAsync(_created.GameId, Globals.cts.Token);
						GD.Print($"[Host] Game status: {state.Status}, turn: {state.Turn}, lastActive: {state.LastActive}");
						
						//Update p2 name and start game
						if (state.Status == "in_progress")
						{
							Label p2Name = GetNode<Label>("%P2name");
							p2Name.Text = state.GuestName;
							GD.Print($"[Host]: {state.GuestName} joined game");
							GetTree().ChangeSceneToFile("res://game.tscn");
						}
						
						// Example: make a sample move when it's host's turn.
						//if (state.Status == "in_progress" && state.Turn == "host")
						//{
							// Replace with your real state string
							//var exampleState = ".oo.xx...";
							//await _client.MakeMoveAsync(_created.GameId, _created.HostToken, exampleState, Globals.cts.Token);
							//GD.Print("[Host] Submitted a move.");
						//}
					}
					catch (Exception ex)
					{
						GD.PrintErr($"[Host] Poll error: {ex.Message}");
					}

					await Task.Delay(TimeSpan.FromSeconds(2), Globals.cts.Token);
				}
			}
			catch (Exception ex)
			{
				GD.PrintErr($"[Host] Initialization error: {ex.Message}");
			}
		}
		
		private async Task HeartbeatLoopAsync(CancellationToken ct)
		{
			// Heartbeat interval should be well under server PLAYER_TIMEOUT (server default 120s).
			var interval = TimeSpan.FromSeconds(20);

			while (!ct.IsCancellationRequested)
			{
				try
				{
					await Globals.hostClient.HeartbeatAsync(Globals.gameId, Globals.hostToken, ct);
				}
				catch (Exception ex)
				{
					GD.PrintErr($"[Host] Heartbeat error: {ex.Message}");
				}

				try
				{
					await Task.Delay(interval, ct);
				}
				catch (TaskCanceledException)
				{
					break;
				}
			}
		}
		
		private void _on_back_btn_pressed()
		{
			GetTree().ChangeSceneToFile("res://Networking/multiplayer_test.tscn");
		}
	}
}
