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
		public async Task<JoinResponse> JoinGameAsync(string gameId, string displayName = Globals.username, CancellationToken ct = default)
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
		 public string? HostDisplayName { get; set; }

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
	
	public partial class Guest : Node
	{
		[Export] public string ServerBaseUrl { get; set; } = "http://localhost:8000";

		private GuestClient _api = null!;
		private CancellationTokenSource _cts = null!;
		private JoinResponse? _joinInfo;

		// Events other nodes can subscribe to
		public event Action<JoinResponse>? OnJoined;
		//public event Action<GameStateResponse>? OnStateUpdated;
		public event Action<string>? OnError;
		
		private void _on_game_id_text_changed(string gameId)
		{
			Globals.gameId = gameId;
		}

		public override void _Ready()
		{
			var http = new System.Net.Http.HttpClient { BaseAddress = new Uri(ServerBaseUrl) };
			_api = new GuestClient(http);
			_cts = new CancellationTokenSource();
		}
		
		private void _on_join_btn_pressed()
		{
			_ = StartGuestFlowAsync(Globals.gameId, Globals.username, _cts.Token);
		}
		
		public override void _ExitTree()
		{
			_cts?.Cancel();
			_cts?.Dispose();
		}

		// Programmatic join that UI can call (non-blocking)
		public void JoinGame(string gameId, string username)
		{
			_ = StartGuestFlowAsync(gameId, username, _cts.Token);
		}
		
		// Submit a move from UI/game logic
		public async Task SubmitMoveAsync(string state)
		{
			if (_joinInfo == null)
			{
				GD.PrintErr("[GuestClientNode] Not joined yet.");
				return;
			}

			try
			{
				await _api.MakeMoveAsync(_joinInfo.GameId, _joinInfo.GuestToken, state, _cts.Token).ConfigureAwait(false);
				GD.Print("[GuestClientNode] Move submitted.");
			}
			catch (Exception ex)
			{
				GD.PrintErr($"[GuestClientNode] SubmitMove error: {ex.Message}");
				OnError?.Invoke(ex.Message);
			}
		}
		
		public (string GameId, string GuestToken)? GetJoinInfo()
		{
			if (_joinInfo == null) return null;
			return (_joinInfo.GameId, _joinInfo.GuestToken);
		}

		private async Task StartGuestFlowAsync(string gameId, string username, CancellationToken ct)
		{
			try
			{
				_joinInfo = await _api.JoinGameAsync(gameId, username, ct).ConfigureAwait(false);
				GD.Print($"[GuestClientNode] Joined game {_joinInfo.GameId} token={_joinInfo.GuestToken} status={_joinInfo.Status}");
				OnJoined?.Invoke(_joinInfo);

				// start heartbeat and polling
				//_ = HeartbeatLoopAsync(_joinInfo, ct);
				//_ = PollStateLoopAsync(_joinInfo.GameId, ct);
			}
			catch (Exception ex)
			{
				GD.PrintErr($"[GuestClientNode] Join error: {ex.Message}");
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
					await _api.HeartbeatAsync(join.GameId, join.GuestToken, ct).ConfigureAwait(false);
				}
				catch (Exception ex)
				{
					GD.PrintErr($"[GuestClientNode] Heartbeat error: {ex.Message}");
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
					var state = await _api.GetGameStateAsync(gameId, ct).ConfigureAwait(false);
					// Use CallDeferred to safely interact with Godot main thread if needed
					CallDeferred(nameof(EmitStateUpdated), state);
				}
				catch (Exception ex)
				{
					GD.PrintErr($"[GuestClientNode] Poll error: {ex.Message}");
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
			GD.Print($"[GuestClientNode] State update: status={state.Status}, turn={state.Turn}, lastActive={state.LastActive}");
			OnStateUpdated?.Invoke(state);
		}
	}
}
