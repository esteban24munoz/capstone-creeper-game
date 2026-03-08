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
