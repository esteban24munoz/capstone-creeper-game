using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
		private GameCreatedResponse _created;
		private UIManager _ui;

		public override void _Ready()
		{
			_ui = UIManager.Instance;

			if (_ui == null)
			{
				GD.PrintErr("GameMode: UIManager Instance is null! Is MainUI.tscn loaded?");
				return;
			}
			
			SetUpInfo();
			
			 //Start background flow without blocking Godot main thread.
			_ = StartHostFlowAsync();
		}
		
		private void SetUpInfo()
		{
			Label p1Name = GetNode<Label>("%P1name");
			p1Name.Text = Globals.username;
			Constants.EnemyPlayer = new NetworkPlayer();
			Constants.HeroPlayer = new LocalPlayer();
			Globals.cts = new CancellationTokenSource();
		}
		
		private async Task CreateGame()
		{
			_created = await Globals.hostClient.CreateGameAsync(Globals.username, Globals.cts.Token);
			GD.Print($"[Host]\tCreated game: {_created.GameId}\ttoken: {_created.HostToken}\tstatus: {_created.Status}");
			Label id = GetNode<Label>("%ID");
			id.Text = _created.GameId;
			Globals.gameId = _created.GameId;
			Globals.token = _created.HostToken;
			Globals.status = _created.Status;
		}
		
		private async Task StartHostFlowAsync()
		{
			try
			{
				// 1) Create game
				await CreateGame();

				// 2) Start heartbeat loop (run concurrently)
				_ = HeartbeatLoopAsync(Globals.cts.Token);

				// Example: poll state periodically to check when p2 has joined.
				while (!Globals.cts.Token.IsCancellationRequested)
				{
					try
					{
						var state = await Globals.hostClient.GetGameStateAsync(Globals.gameId, Globals.cts.Token);
						GD.Print($"[Host]\tGame status: {state.Status}\tstate: {state.State}");
						
						//Update p2 name and start game
						if (state.Status == "in_progress")
						{
							Label p2Name = GetNode<Label>("%P2name");
							p2Name.Text = state.GuestName;
							GD.Print($"[Host]\t{state.GuestName} joined game");
							_ = PollStateLoopAsync(Globals.gameId, Globals.cts.Token);
							await UIManager.Instance.ChangeSceneWithTransition("res://game.tscn");
							return;
						}
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

		private async Task PollStateLoopAsync(string gameId, CancellationToken ct)
		{
			var interval = TimeSpan.FromSeconds(1);
			bool moveFound = false;
			while (!ct.IsCancellationRequested)
			{
				try
				{
					var stateResp = await Globals.hostClient.GetGameStateAsync(gameId, ct);
					if (!string.IsNullOrEmpty(stateResp.State))
					{
						GD.Print($"[Host Poll Loop]\tGame status: {stateResp.Status}\tturn: {stateResp.Turn}\tstate: {stateResp.State}");
						if ((stateResp.Status == "in_progress" && stateResp.Turn == "host" && !moveFound) || (stateResp.Status == "finished" && !moveFound)) 
						{
							Constants.EnemyPlayer.ReceiveState(stateResp.State);
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
							GD.Print("[Host] The other player has left the game");
							Globals.status = stateResp.Status;
							Globals.cts.Cancel();
							//Change to main menu or something
						}
						if (stateResp.Turn == "guest")
							moveFound = false;
					}
				}
				catch (Exception ex)
				{
					GD.PrintErr($"[Host] Poll loop error: {ex.Message}");
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

		private async Task HeartbeatLoopAsync(CancellationToken ct)
		{
			// Heartbeat interval should be well under server PLAYER_TIMEOUT.
			var interval = TimeSpan.FromSeconds(10);

			while (!ct.IsCancellationRequested)
			{
				try
				{
					await Globals.hostClient.HeartbeatAsync(Globals.gameId, Globals.token, ct);
					GD.Print($"[Host] Heartbeat");
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
	}
}
