using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Linq;

public partial class MTCS_Pure : Node
{	
	public override void _Ready()
	{
		MCTSNode currentNode = new MCTSNode(currentGame);
		Move bestMove = currentNode.GetBestMove(currentGame);
		GD.Print($"Move {bestMove} chosen");
	}
	public string boardState = ".oo.x..o....xxo.....x.......x.....ox.....o.xx.oo.o....x........................x....o"; 
	public Model currentGame;
	private const double c = 1.41421356;
	public Constants.Player currentPlayer = Constants.Player.Enemy;
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
		public Constants.Player currentPlayer = Constants.Player.Enemy;

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
			Action = action;
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
