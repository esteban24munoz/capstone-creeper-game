using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;

public partial class MTCS_Pure2 : Node
{
	private static string SOFTSERVE_URL = "https://softserve.harding.edu";
	private const string PLAYER_NAME = "Team10Test";
	//private const string PLAYER_TOKEN = "yV73X6Wtg_4rAnHEI2UP1Iw53F9XkuQ4Gr-tbfWo1-M";
	private const string PLAYER_EMAIL = "hconner@harding.edu";
	private const string EVENT = "mirror"; //midterm
	private static System.Net.Http.HttpClient client = new System.Net.Http.HttpClient();
	int winCount = 0;
	int drawCount = 0;
	
	public async void PlayAiVsAi()
	{
		string PLAYER_TOKEN;
		
		try
		{
			PLAYER_TOKEN = await File.ReadAllTextAsync($"{PLAYER_NAME}_token.txt");
		}
		catch (FileNotFoundException)
		{
			var createPlayerPayload = new
			{
				name = PLAYER_NAME,
				email = PLAYER_EMAIL
			};
			
			var createResponse = await client.PostAsJsonAsync($"{SOFTSERVE_URL}/player/create", createPlayerPayload);
			createResponse.EnsureSuccessStatusCode();
			var createJson = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
			PLAYER_TOKEN = createJson.GetProperty("token").GetString();
			
			await File.WriteAllTextAsync($"{PLAYER_NAME}_token.txt", PLAYER_TOKEN);
		}
		
		string playStateUrl = $"{SOFTSERVE_URL}/aivai/play-state";
		Model currentGame = new();
		while (true)
		//for (int i = 0; i < 1; i++)
		{
			var playStateObj = new
			{
				@event = EVENT,
				player = PLAYER_NAME,
				token = PLAYER_TOKEN
			};
			
			GD.Print("Getting the state");
			var playStateResponse = await client.PostAsJsonAsync(playStateUrl, playStateObj);
			//playStateResponse.EnsureSuccessStatusCode();

			if (playStateResponse.StatusCode == System.Net.HttpStatusCode.NoContent || playStateResponse.StatusCode == System.Net.HttpStatusCode.InternalServerError)
			{
				// No games waiting for move, wait and continue
				GD.Print($"{playStateResponse.StatusCode}: Sleep 2 seconds and try again");
				await Task.Delay(2000);
				continue;
			}

			var playStateJson = await playStateResponse.Content.ReadFromJsonAsync<JsonElement>();
			string state = playStateJson.GetProperty("state").GetString();
			string game_id = playStateJson.GetProperty("game_id").ToString();
			string action_id = playStateJson.GetProperty("action_id").ToString();
			GD.Print($"game_id: {game_id}\t action_id: {action_id}");
			GD.Print($"state: {state}");
			string parsedAction;

			/***************************************************************************************
			Starting here, AI should take over. The state of the game is still just a string.
			***************************************************************************************/
			if (state == ".oo.xx.o.....xo.....x.......x.....ox.....o.xx.oo.o....x........................x....ox"){
				GD.Print("New game started");
				//return;
			}
			
			//Get State and update the Model accordingly
			Constants.Player activePlayer = currentGame.UpdateState(state);
			MonteCarloStrategy currentNode = new MonteCarloStrategy();
			Move bestMove = currentNode.ChooseBestMove(currentGame, activePlayer);
			GD.Print(bestMove);
			parsedAction = ParseAction(bestMove.From, bestMove.To);
			
			/***************************************************************************************
			End of AI
			***************************************************************************************/
			
			var submitActionObj = new
			{
				action = parsedAction,
				action_id = action_id,
				player = PLAYER_NAME,
				token = PLAYER_TOKEN
			};

			var submitResponse = await client.PostAsJsonAsync($"{SOFTSERVE_URL}/aivai/submit-action", submitActionObj);
			//submitResponse.EnsureSuccessStatusCode();
			if (submitResponse.StatusCode == System.Net.HttpStatusCode.OK)
			{
				var submitJson = await submitResponse.Content.ReadFromJsonAsync<JsonElement>();
				string winner = submitJson.GetProperty("winner").GetString();
				GD.Print($"Winner: {winner}");
				if (winner == "draw")
				{
					drawCount++;
					GD.Print($"Draws: {drawCount}");
				}
				if (winner == "x" || winner == "o")
				{
					winCount++;
					GD.Print($"Wins: {winCount}");
				}
			}
			else // (submitResponse.StatusCode == System.Net.HttpStatusCode.InternalServerError)
			{
				GD.Print("{submitResponse.StatusCode}: Sleep half a second and try again");
				await Task.Delay(500);
				continue;
			}
		}
	}
	
	public string ParseAction(Vector2I from, Vector2I to)
	{
		//Convert the X to letters. Format should look like f7e6
		char fromCol = (char)('a' + from.Y);
		char toCol = (char)('a' + to.Y);
		
		string action = $"{fromCol}{from.X + 1}{toCol}{to.X + 1}";
		GD.Print(action);
		return action;
	}
	
	public void _on_button_pressed()
	{
		try
		{
			PlayAiVsAi();
		}
		catch (HttpRequestException ex)
		{
			GD.Print($"Request to softserve failed: {ex.Message}");
		}
	}
	
	public override void _Ready()
	{
		//string boardState = ".o..x..o.....xoo....x....x..x.....oxx..o....x.oo.o...xx.o.x.....x............oox....ox";
		//Model currentGame = new();
		//Constants.Player activePlayer = currentGame.UpdateState(boardState);
		//MonteCarloStrategy currentNode = new MonteCarloStrategy();
		//Move bestMove = currentNode.ChooseBestMove(currentGame, activePlayer);
		//GD.Print(bestMove);
		//string parsedAction = ParseAction(bestMove.From, bestMove.To);
		//try
		//{
			//PlayAiVsAi();
		//}
		//catch (HttpRequestException ex)
		//{
			//GD.Print($"Request to softserve failed: {ex.Message}");
		//}
	}
	
	public readonly struct Move
	{
		public Vector2I From { get; }
		public Vector2I To { get; }

		public Move(Vector2I from, Vector2I to)
		{
			From = from;
			To = to;
		}

		public override string ToString() => $"Move(From={From}, To={To})";
	}
	
	public class MonteCarloStrategy
	{
		private readonly TimeSpan _timeBudget;
		private readonly int _maxPlayoutLength;
		private readonly double _winScore;
		private readonly double _drawScore;
		private readonly Random _rng;
		//public Model state

		/// <summary>
		/// Monte Carlo constructor.
		/// Default behavior weights wins higher than draws (winScore=1.0, drawScore=0.4).
		/// Time budget is specified in seconds (default 4).
		/// </summary>
		public MonteCarloStrategy(int timeBudgetSeconds = 4, int maxPlayoutLength = 200, double winScore = 1.0, double drawScore = 0.4, int? seed = null)
		{
			_timeBudget = TimeSpan.FromSeconds(Math.Max(0.1, timeBudgetSeconds));
			_maxPlayoutLength = Math.Max(1, maxPlayoutLength);
			_winScore = winScore;
			_drawScore = drawScore;
			_rng = seed.HasValue ? new Random(seed.Value) : new Random();
		}

		/// <summary>
		/// Choose the best move for the given player by running randomized playouts
		/// until the configured time budget elapses. Returns the selected Move (from/to).
		/// Throws InvalidOperationException if no moves available.
		/// </summary>
		public Move ChooseBestMove(Model rootModel, Constants.Player myPlayer)
		{
			// Enumerate legal candidate moves from the root state
			var candidates = new List<Move>();
			foreach (var pos in rootModel.GetAllCharacters(myPlayer))
			{
				var valid = rootModel.FindValidMoves(pos, myPlayer);
				if (valid != null)
				{
					foreach (var to in valid)
					{
						candidates.Add(new Move(pos, to));
					}
				}
			}

			if (candidates.Count == 0)
				throw new InvalidOperationException("No legal moves available for player.");

			// stats per candidate: accumulated weighted score and simulation count
			var accScore = new double[candidates.Count];
			var sims = new int[candidates.Count];

			var sw = Stopwatch.StartNew();

			// Ensure at least one simulation per candidate if time allows
			for (int i = 0; i < candidates.Count && sw.Elapsed < _timeBudget; i++)
			{
				RunSingleSimulation(rootModel, myPlayer, candidates[i], ref accScore[i], ref sims[i]);
			}

			// Round-robin continue until time budget expires
			while (sw.Elapsed < _timeBudget)
			{
				for (int i = 0; i < candidates.Count; i++)
				{
					RunSingleSimulation(rootModel, myPlayer, candidates[i], ref accScore[i], ref sims[i]);
					if (sw.Elapsed >= _timeBudget) break;
				}
			}

			// Pick best by average weighted score (accScore / sims)
			double bestScore = double.NegativeInfinity;
			Move bestMove = candidates[0];

			for (int i = 0; i < candidates.Count; i++)
			{
				double avg = sims[i] > 0 ? (accScore[i] / sims[i]) : 0.0;
				avg += (_rng.NextDouble() - 0.5) * 1e-6; // tiny noise to break ties
				if (avg > bestScore)
				{
					bestScore = avg;
					bestMove = candidates[i];
				}
			}

			return bestMove;
		}

		private void RunSingleSimulation(Model rootModel, Constants.Player myPlayer, Move candidate, ref double accScoreOut, ref int simsOut)
		{
			// Clone and play
			var simModel = new Model(rootModel);
			simModel.MoveCharacter(candidate.From, candidate.To);

			var current = Opponent(myPlayer);
			int steps = 0;

			while (steps < _maxPlayoutLength)
			{
				var winner = simModel.FindWinner();
				if (winner != Constants.Player.None) break;

				var legalMoves = GatherAllMoves(simModel, current);
				if (legalMoves.Count == 0) break;

				var chosen = legalMoves[_rng.Next(legalMoves.Count)];
				simModel.MoveCharacter(chosen.From, chosen.To);

				current = Opponent(current);
				steps++;
			}

			var finalWinner = simModel.FindWinner();
			if (finalWinner == myPlayer)
			{
				accScoreOut += _winScore;
			}
			else if (finalWinner == Constants.Player.None && simModel.IsDraw(current))
			{
				accScoreOut += _drawScore;
			}
			// losses give 0

			simsOut += 1;
		}

		private static Constants.Player Opponent(Constants.Player p)
		{
			return p == Constants.Player.Hero ? Constants.Player.Enemy : Constants.Player.Hero;
		}

		// Gather all legal moves for a player as Move records
		private List<Move> GatherAllMoves(Model model, Constants.Player player)
		{
			var moves = new List<Move>();
			foreach (var pos in model.GetAllCharacters(player))
			{
				var valid = model.FindValidMoves(pos, player);
				if (valid != null)
				{
					foreach (var to in valid)
					{
						moves.Add(new Move(pos, to));
					}
				}
			}
			return moves;
		}
	}
}
