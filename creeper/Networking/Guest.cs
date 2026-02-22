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
	public class GuestClient
	{
		private readonly System.Net.Http.HttpClient _http;
		private readonly JsonSerializerOptions _jsonOptions = new()
		{
			PropertyNameCaseInsensitive = true
		};

		public GuestClient(System.Net.Http.HttpClient http)
		{
			_http = http;
		}

		// Join an existing game. displayName is sent as a query parameter.
		public async Task<JoinResponse> JoinGameAsync(string gameId, string displayName = null, CancellationToken ct = default)
		{
			var url = $"/games/{Uri.EscapeDataString(gameId)}/join";
			if (!string.IsNullOrEmpty(displayName))
				url += $"?display_name={Uri.EscapeDataString(displayName)}";

			var content = new StringContent("{}", Encoding.UTF8, "application/json");
			var resp = await _http.PostAsync(url, content, ct).ConfigureAwait(false);
			resp.EnsureSuccessStatusCode();
			var body = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
			return JsonSerializer.Deserialize<JoinResponse>(body, _jsonOptions)!;
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
	
	public class JoinResponse
	{
		[JsonPropertyName("game_id")]
		public string GameId { get; set; } = default!;

		[JsonPropertyName("guest_token")]
		public string GuestToken { get; set; } = default!;

		[JsonPropertyName("status")]
		public string Status { get; set; } = default!;
	}
	
	public class GameStateResponse
	{
		 [JsonPropertyName("game_id")]
		 public string GameId { get; set; } = default!;

		 [JsonPropertyName("host_display_name")]
		 public string? HostName { get; set; }
		
		[JsonPropertyName("guest_display_name")]
		 public string? GuestName { get; set; }

		 [JsonPropertyName("status")]
		 public string Status { get; set; } = default!;

		 [JsonPropertyName("turn")]
		 public string? Turn { get; set; }

		 [JsonPropertyName("state")]
		 public string State { get; set; } = default!;

		 [JsonPropertyName("moves")]
		 public List<Dictionary<string, object>> Moves { get; set; } = new();

		 [JsonPropertyName("created_at")]
		 public DateTime CreatedAt { get; set; }

		 [JsonPropertyName("last_active")]
		 public DateTime LastActive { get; set; }
	}

	public class GameListItem
	{
		 [JsonPropertyName("game_id")]
		 public string GameId { get; set; } = default!;

		 [JsonPropertyName("host_display_name")]
		 public string? HostDisplayName { get; set; }

		 [JsonPropertyName("status")]
		 public string Status { get; set; } = default!;

		 [JsonPropertyName("created_at")]
		 public DateTime CreatedAt { get; set; }

		 [JsonPropertyName("last_active")]
		 public DateTime LastActive { get; set; }
	}
	
	public partial class Guest : Control
	{
		private JoinResponse? _joinInfo;

		// Events other nodes can subscribe to
		public event Action<JoinResponse>? OnJoined;
		public event Action<GameStateResponse>? OnStateUpdated;
		public event Action<string>? OnError;
		
		private void _on_game_id_text_changed(string gameId)
		{
			Globals.gameId = gameId;
		}
		
		private void _on_back_btn_pressed()
		{
			GetTree().ChangeSceneToFile("res://Networking/multiplayer_test.tscn");
		}

		public override void _Ready()
		{
			Globals.p1Type = "Network";
			Globals.p2Type = "Person";
		}
		
		private async void _on_join_btn_pressed()
		{
			await StartGuestFlowAsync(Globals.gameId, Globals.username, Globals.cts.Token);
			GetTree().ChangeSceneToFile("res://game.tscn");
		}
		
		public override void _ExitTree()
		{
			Globals.cts?.Cancel();
			Globals.cts?.Dispose();
		}
		
		// Submit a move from UI/game logic
		public async Task SubmitMoveAsync(string state)
		{
			if (_joinInfo == null)
			{
				GD.PrintErr("[Guest] Not joined yet.");
				return;
			}

			try
			{
				await Globals.guestClient.MakeMoveAsync(_joinInfo.GameId, _joinInfo.GuestToken, state, Globals.cts.Token).ConfigureAwait(false);
				GD.Print("[Guest] Move submitted.");
			}
			catch (Exception ex)
			{
				GD.PrintErr($"[Guest] SubmitMove error: {ex.Message}");
				OnError?.Invoke(ex.Message);
			}
		}

		private async Task StartGuestFlowAsync(string gameId, string username, CancellationToken ct)
		{
			try
			{
				_joinInfo = await Globals.guestClient.JoinGameAsync(gameId, username, ct).ConfigureAwait(false);
				GD.Print($"[Guest] Joined game {_joinInfo.GameId} token={_joinInfo.GuestToken} status={_joinInfo.Status}");
				OnJoined?.Invoke(_joinInfo);
				Globals.guestToken = _joinInfo.GuestToken;
				Globals.status = _joinInfo.Status;
				
				//GetTree().ChangeSceneToFile("res://game.tscn");
				// start heartbeat and polling
				_ = HeartbeatLoopAsync(_joinInfo, ct);
				//_ = PollStateLoopAsync(_joinInfo.GameId, ct);
			}
			catch (Exception ex)
			{
				GD.PrintErr($"[Guest] Join error: {ex.Message}");
				OnError?.Invoke(ex.Message);
			}
		}
		
		private async Task HeartbeatLoopAsync(JoinResponse join, CancellationToken ct)
		{
			var interval = TimeSpan.FromSeconds(20); // keep < server PLAYER_TIMEOUT
			while (!ct.IsCancellationRequested)
			{
				try
				{
					await Globals.guestClient.HeartbeatAsync(join.GameId, join.GuestToken, ct).ConfigureAwait(false);
				}
				catch (Exception ex)
				{
					GD.PrintErr($"[Guest] Heartbeat error: {ex.Message}");
					OnError?.Invoke(ex.Message);
				}

				try
				{
					await Task.Delay(interval, ct).ConfigureAwait(false);
				}
				catch (TaskCanceledException)
				{
					break;
				}
			}
		}

		private async Task PollStateLoopAsync(string gameId, CancellationToken ct)
		{
			var interval = TimeSpan.FromSeconds(2);
			while (!ct.IsCancellationRequested)
			{
				try
				{
					var state = await Globals.guestClient.GetGameStateAsync(gameId, ct).ConfigureAwait(false);
					// Use CallDeferred to safely interact with Godot main thread if needed
					//CallDeferred(nameof(EmitStateUpdated), state);
				}
				catch (Exception ex)
				{
					GD.PrintErr($"[Guest] Poll error: {ex.Message}");
					OnError?.Invoke(ex.Message);
				}

				try
				{
					await Task.Delay(interval, ct).ConfigureAwait(false);
				}
				catch (TaskCanceledException)
				{
					break;
				}
			}
		}

		// Invoked on the main thread via CallDeferred
		private void EmitStateUpdated(GameStateResponse state)
		{
			GD.Print($"[Guest] State update: status={state.Status}, turn={state.Turn}, lastActive={state.LastActive}");
			//OnStateUpdated?.Invoke(state);
		}
	}
}
