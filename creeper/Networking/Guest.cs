using Godot;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

// Written with the help of GitHub Copilot
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
			try
			{
				resp.EnsureSuccessStatusCode();
			}
			catch (HttpRequestException ex)
			{
				if (resp.StatusCode == System.Net.HttpStatusCode.NotFound)
					return null;
				GD.PrintErr($"Join game failed: {ex}");
			}
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
		 public string HostName { get; set; }
		
		[JsonPropertyName("guest_display_name")]
		 public string GuestName { get; set; }

		 [JsonPropertyName("status")]
		 public string Status { get; set; } = default!;

		 [JsonPropertyName("turn")]
		 public string Turn { get; set; }

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
		 public string HostDisplayName { get; set; }

		 [JsonPropertyName("status")]
		 public string Status { get; set; } = default!;

		 [JsonPropertyName("created_at")]
		 public DateTime CreatedAt { get; set; }

		 [JsonPropertyName("last_active")]
		 public DateTime LastActive { get; set; }
	}
	
	public partial class Guest : Control
	{
		private JoinResponse _joinInfo;
		private UIManager _ui;
		private Label errorMessage;
		
		private void _on_game_id_text_changed(string gameId)
		{
			Globals.gameId = gameId;
			errorMessage.Visible = false;
		}

		private void _on_game_id_text_submitted(string text)
		{
			_on_join_btn_pressed();
		}

		public override void _Ready()
		{
			_ui = UIManager.Instance;

			if (_ui == null)
			{
				GD.PrintErr("GameMode: UIManager Instance is null! Is MainUI.tscn loaded?");
				return;
			}
			Constants.HeroPlayer = new NetworkPlayer();
			Constants.EnemyPlayer = new LocalPlayer();
			Globals.cts = new CancellationTokenSource();
			errorMessage = GetNode<Label>("%ErrorMessage");
		}
		
		private async void _on_join_btn_pressed()
		{
			if (string.IsNullOrWhiteSpace(Globals.gameId)) {
				errorMessage.Text = "Room code must be entered!";
				errorMessage.Visible = true;
				return;
			}
			await JoinGame(Globals.gameId, Globals.username, Globals.cts.Token);
			if (errorMessage.Visible)
				return;
			
			// start heartbeat and polling loops
			_ = HeartbeatLoopAsync(Globals.cts.Token);
			_ = PollStateLoopAsync(Globals.gameId, Globals.cts.Token);
			await UIManager.Instance.ChangeSceneWithTransition("res://game.tscn");
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
			}
			catch (Exception ex)
			{
				GD.PrintErr($"[Guest] SubmitMove error: {ex.Message}");
			}
		}

		private async Task JoinGame(string gameId, string username, CancellationToken ct)
		{
			try
			{
				_joinInfo = await Globals.guestClient.JoinGameAsync(gameId, username, ct);
				if (_joinInfo == null)
				{
					errorMessage.Text = "Wrong room code entered";
					errorMessage.Visible = true;
				}
				GD.Print($"[Guest]\tJoined game: {_joinInfo.GameId}\ttoken: {_joinInfo.GuestToken}\tstatus: {_joinInfo.Status}");
				Globals.token = _joinInfo.GuestToken;
				Globals.status = _joinInfo.Status;
				
				// Note: heartbeat + polling started by caller after this returns
			}
			catch (Exception ex)
			{
				GD.PrintErr($"[Guest] Join error: {ex.Message}");
			}
		}
		
		private async Task HeartbeatLoopAsync(CancellationToken ct)
		{
			var interval = TimeSpan.FromSeconds(10); // keep < server PLAYER_TIMEOUT
			while (!ct.IsCancellationRequested)
			{
				try
				{
					await Globals.guestClient.HeartbeatAsync(Globals.gameId, Globals.token, ct).ConfigureAwait(false);
					GD.Print($"[Guest] Heartbeat");
				}
				catch (Exception ex)
				{
					GD.PrintErr($"[Guest] Heartbeat error: {ex.Message}");
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

		// Poll server for state and forward it to NetworkPlayer.ReceiveState
		private async Task PollStateLoopAsync(string gameId, CancellationToken ct)
		{
			var interval = TimeSpan.FromSeconds(1);
			bool moveFound = false;
			while (!ct.IsCancellationRequested)
			{
				try
				{
					var stateResp = await Globals.guestClient.GetGameStateAsync(gameId, ct);
					GD.Print($"[Guest Poll Loop]\tGame status: {stateResp.Status}\tturn: {stateResp.Turn}\tstate: {stateResp.State}");
					if (!string.IsNullOrEmpty(stateResp.State))
					{
						if ((stateResp.Status == "in_progress" && stateResp.Turn == "guest" && !moveFound) || (stateResp.Status == "finished" && !moveFound))
						{
							Constants.HeroPlayer.ReceiveState(stateResp.State);
							moveFound = true;
							Globals.status = stateResp.Status;
						}
						else if (stateResp.Status == "finished" && moveFound)
						{
							Globals.cts.Cancel();
						}
						else if (stateResp.Status == "disconnected")
						{
							// The guest disconnected (left the game)
							GD.Print("[Guest] The other player has left the game");
							Globals.status = stateResp.Status;
							Globals.cts.Cancel();
							//Change to main menu or something
						}
						
						if (stateResp.Turn == "host")
							moveFound = false;
					}
				}
				catch (Exception ex)
				{
					GD.PrintErr($"[Guest] Poll error: {ex.Message}");
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
	}
}
