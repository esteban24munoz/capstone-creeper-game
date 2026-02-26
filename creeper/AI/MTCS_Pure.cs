using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Linq;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Text.Json;

public partial class MTCS_Pure : Node
{	
	private static string SOFTSERVE_URL = "https://softserve.harding.edu";
	private const string PLAYER_NAME = "LOTRAI";
	private const string PLAYER_TOKEN = "yV73X6Wtg_4rAnHEI2UP1Iw53F9XkuQ4Gr-tbfWo1-M";
	private const string PLAYER_EMAIL = "hconner@harding.edu";
	private const string EVENT = "mirror";
	private static System.Net.Http.HttpClient client = new System.Net.Http.HttpClient();
	
	public async void PlayAiVsAi()
	{
		string playStateUrl = $"{SOFTSERVE_URL}/aivai/play-state";
		Model currentGame = new();
		bool gameOver = false;
		while (!gameOver)
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
			//GD.Print(playStateResponse.StatusCode);
			playStateResponse.EnsureSuccessStatusCode();

			if (playStateResponse.StatusCode == System.Net.HttpStatusCode.NoContent)
			{
				// No games waiting for move, wait and continue
				GD.Print("204: Sleep 5 seconds and try again");
				await Task.Delay(5000);
				continue;
			}

			var playStateJson = await playStateResponse.Content.ReadFromJsonAsync<JsonElement>();
			//GD.Print(playStateJson);
			string state = playStateJson.GetProperty("state").GetString();
			string action_id = playStateJson.GetProperty("action_id").ToString();
			string parsedAction;

			GD.Print($"state:\t{state}");

			/***************************************************************************************
			Starting here, AI should take over. The state of the game is still just a string.
			***************************************************************************************/
			
			Constants.Player activePlayer = currentGame.UpdateState(state);
			var winner = currentGame.FindWinner();
			GD.Print($"Winner: {winner}");
			if (winner == Constants.Player.None)
			{
				GD.Print("Game continues");
				MCTSNode currentNode = new MCTSNode(currentGame);
				currentNode.currentPlayer = activePlayer;
				Move bestMove = currentNode.GetBestMove(currentGame);
				GD.Print($"Move {bestMove._from} {bestMove.to} chosen");
				parsedAction = ParseAction(bestMove._from, bestMove.to);
				
				var submitActionObj = new
				{
					action = parsedAction,
					action_id = action_id,
					player = PLAYER_NAME,
					token = PLAYER_TOKEN
				};

				var submitResponse = await client.PostAsJsonAsync($"{SOFTSERVE_URL}/aivai/submit-action", submitActionObj);
				submitResponse.EnsureSuccessStatusCode();
			}
			else if (winner == Constants.Player.Draw)
			{
				GD.Print("Game Over! It was a draw!");
				gameOver = true;
			}
			else
			{
				GD.Print($"Game Over! {winner} won!");
				gameOver = true;
			}
			
			//GD.Print("Testing Action Parsing");
			//string parsedAction = ParseAction(bestMove._from, bestMove.to);
			
			/***************************************************************************************
			End of AI
			***************************************************************************************/
			
			//var submitActionObj = new
			//{
				//action = parsedAction,
				//action_id = action_id,
				//player = PLAYER_NAME,
				//token = PLAYER_TOKEN
			//};
//
			//var submitResponse = await client.PostAsJsonAsync($"{SOFTSERVE_URL}/aivai/submit-action", submitActionObj);
			//submitResponse.EnsureSuccessStatusCode();
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
	
	public void TestState()
	{
		Model currentGame = new();
		string state = ".oo.xx.o.....xo.....x.......x.....ox.....o.xx.oo.o....x........................x....ox";
		GD.Print($"state:\t{state}");
		Constants.Player activePlayer = currentGame.UpdateState(state);
		var winner = currentGame.FindWinner();
		GD.Print($"Winner: {winner}");
		if (winner == Constants.Player.None)
		{
			GD.Print("Game continues");
			MCTSNode currentNode = new MCTSNode(currentGame);
			currentNode.currentPlayer = activePlayer;
			Move bestMove = currentNode.GetBestMove(currentGame);
			GD.Print($"Move {bestMove._from} {bestMove.to} chosen");
		}
		else if (winner == Constants.Player.Draw)
		{
			GD.Print("Game Over! It was a draw!");
		}
		else
		{
			GD.Print($"Game Over! {winner} won!");
		}
	}
	
	public override void _Ready()
	{
		//string boardState = ".o..x..o.....xoo....x....x..x.....oxx..o....x.oo.o...xx.o.x.....x............oox....ox";
		//Model currentGame = new();
		//currentGame.UpdateState(boardState);
		//MCTSNode currentNode = new MCTSNode(currentGame);
		//Move bestMove = currentNode.GetBestMove(currentGame);
		//GD.Print($"Move {bestMove._from} {bestMove.to} chosen");
		PlayAiVsAi();
		//TestState();
	}
	public string boardState = ".oo.xx.o.....xo.....x.......x.....ox.....o.xx.oo.o....x........................x....ox"; 
	public Model currentGame;
	//public Constants.Player currentPlayer = Constants.Player.Hero;
	//currentPlayer = currentGame.UpdateState(boardState);
	
	public class Move
	{
		public Vector2I _from;
		public Vector2I to;
		
		//Copy Constructor
		public Move(Move other)
		{
			this._from = other._from;
			this.to = other.to;
		}
		public Move()
		{
			this._from = new();
			this.to = new();
		}
	}
	
	//usage of a node came from gemini prompt "Generate a c# Monte Carlo Strategy"
	public class MCTSNode 
	{
		public Model State;
		public MCTSNode Parent;
		public List<MCTSNode> Children = new List<MCTSNode>();
		public Move Action; // Move taken to reach this state
		public double Wins = 0;
		public int Visits = 0;
		public Move LastMove;
		public Constants.Player currentPlayer;

		//Copy Constructor
		public MCTSNode(MCTSNode other)
		{
			//Primitive and value types
			this.Wins = other.Wins;
			this.Visits = other.Visits;
			this.currentPlayer = other.currentPlayer;
			
			// Immutable or simple reference types
			this.Action = other.Action != null ? new Move(other.Action) : null;
			this.LastMove = other.LastMove != null ? new Move(other.LastMove) : null;
			this.State = other.State != null ? new Model(other.State) : null;
			this.Parent = other.Parent;
			
			if (other.Children != null)
			{
				this.Children = other.Children
					.Select(child => new MCTSNode(child) {Parent = this }).ToList();
			}
			else
			{
				this.Children = new List<MCTSNode>();
			}
		}
		
		public MCTSNode(Model state, MCTSNode parent = null, Move action = null) 
		{
			State = state;
			Parent = parent;
			LastMove = action;
		}
		public double GetUCB1(double explorationConstant = 1.41)
		{
			if (Visits == 0) return double.MaxValue;
			return (Wins / Visits) + explorationConstant * Math.Sqrt(Math.Log(Parent.Visits) / Visits);
		}
		
		public Move GetBestMove(Model rootState, int maxTimeMs = 4000)
		{
			MCTSNode root = new MCTSNode(this);
			Stopwatch sw = Stopwatch.StartNew();
			while(sw.ElapsedMilliseconds < maxTimeMs)
			{
				MCTSNode selected = Select(root);
				if (!selected.State.IsDraw(currentPlayer) && selected.State.FindWinner() == Constants.Player.None) //Possible issue with draw returning
				{
					selected = Expand(selected);
				}
				Model clone = new Model(root.State);
				float result = Simulate(clone);
				Backpropagate(selected, result);
			}
			//GD.Print(root.Children.Count);
			//GD.Print("Move "+root.Children.OrderByDescending(c => c.Visits).FirstOrDefault());
			return root.Children.OrderByDescending(c => c.Visits).FirstOrDefault()?.LastMove;
		}
		private MCTSNode Select(MCTSNode node)
		{
			List<Move> moves = new();
			List<Vector2I> pieces = node.State.GetAllCharacters(currentPlayer);
			for (int i = 0; i < pieces.Count(); i++) {
				List<Vector2I> possibleMoves = node.State.FindValidMoves(pieces[i], currentPlayer);
				for(int x = 0; x < possibleMoves.Count(); x++){
					Move newMove = new();
					newMove._from = pieces[i];
					newMove.to = possibleMoves[x];
					moves.Add(newMove);
				}
			}
			while (node.Children.Count > 0 && moves.Count() == node.Children.Count)
			{
				node = node.Children.OrderByDescending(c => c.GetUCB1()).First();
			}
			return node;
		}
		private MCTSNode Expand(MCTSNode node)
		{
			List<Move> moves = new();
			List<Vector2I> pieces = node.State.GetAllCharacters(currentPlayer);
			for (int i = 0; i < pieces.Count(); i++) {
				List<Vector2I> possibleMoves = node.State.FindValidMoves(pieces[i], currentPlayer);
				for(int x = 0; x < possibleMoves.Count(); x++){
					Move newMove = new();
					newMove._from = pieces[i];
					newMove.to = possibleMoves[x];
					moves.Add(newMove);
				}
			}
			List<Move> triedMoves = node.Children.Select(c => c.LastMove).ToList();
			List<Move> untriedMoves = moves.Where(m => !triedMoves.Contains(m)).ToList();

			if (untriedMoves.Count == 0) return node;

			Move move = untriedMoves[new Random().Next(untriedMoves.Count)];
			Model newState = new Model(node.State);
			newState.MoveCharacter(move._from,move.to);
			
			MCTSNode newNode = new MCTSNode(newState, node, move);
			node.Children.Add(newNode);
			return newNode;
		}
		private float Simulate(Model state)
		{
			Random rand = new Random();
			while (!state.IsDraw(currentPlayer) && state.FindWinner() == Constants.Player.None)
			{
				//var moves = state.GetPossibleMoves();
				//state.ApplyMove(moves[rand.Next(moves.Count)]);
				List<Move> moves = new();
				List<Vector2I> pieces = state.GetAllCharacters(currentPlayer);
				for(int i = 0; i < pieces.Count(); i++){
					List<Vector2I> possibleMoves = state.FindValidMoves(pieces[i], currentPlayer);
					for(int x = 0; x < possibleMoves.Count(); x++){
						Move newMove = new();
						newMove._from = pieces[i];
						newMove.to = possibleMoves[x];
						moves.Add(newMove);
					}
				}
				state.MoveCharacter(moves[rand.Next(moves.Count)]._from,moves[rand.Next(moves.Count)].to);
			}
			if(state.FindWinner() == currentPlayer)
			{
				return 1; // 1 for win, 0 for loss, 0.5 for draw
			}
			else if(state.IsDraw(currentPlayer))
			{
				return 0.5f; //struggled to get float had to get gemini to help promp of "in c# how to make a float = to .5"
			}
			else
			{
				return 0;
			}
		}
		private void Backpropagate(MCTSNode node, float result)
		{
			while (node != null)
			{
				node.Visits++;
				node.Wins += result;
				node = node.Parent;
			}
		}
	}
}
