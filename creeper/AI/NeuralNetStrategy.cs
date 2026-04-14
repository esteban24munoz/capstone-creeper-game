using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

/// <summary>
/// Lightweight perceptron evaluator with simple training (online / batch SGD) and weight persistence.
/// Training uses Monte-Carlo returns: each recorded state is paired with the final game outcome
/// from the perspective of the player who moved at that state (Win=+1, Draw=0, Loss=-1).
/// Written with the help of GitHub Copilot
/// </summary>
public static class NeuralNetStrategy
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

	private static readonly double[] _weights = new double[]
	{
		0.890462548697793,
		-0.8319791953797963,
		1.2724287807275947,
		-1.0115392813531137,
		1.149401636235937,
		-1.2043864492911385,
		0.08762919306676697,
		-0.24933425728563646,
		-1.35542413666725975,
		-0.05814343142690983
	};
	private static double _bias = -0.11821682047247309;

	// Normalization constants
	private const double MAX_PIECES = 12.0;
	private const double MAX_TILES = 36.0;
	private const double MAX_MOBILITY = 100.0;
	private const double MAX_JUMP_OPS = 30.0;
	private const double MAX_VULN = 12.0;

	private static readonly Random _rng = new Random();
	private static readonly object _weightsLock = new object();

	// Simple persistent file for weights
	private const string WEIGHTS_FILENAME = "neural_weights.json";

	/// <summary>
	/// Choose best move by greedy NN evaluation (same as before).
	/// </summary>
	public static Move ChooseBestMove(Model rootModel, Constants.Player myPlayer)
	{
	  var candidates = new List<Move>();
		foreach (var pos in rootModel.GetAllCharacters(myPlayer))
		{
			var valid = rootModel.FindValidMoves(pos, myPlayer);
			if (valid != null)
			{
				foreach (var to in valid)
					candidates.Add(new Move(pos, to));
			}
		}

		if (candidates.Count == 0)
			throw new InvalidOperationException("No legal moves available for player.");

		// Precompute which moves would leave the opponent with zero pieces (i.e., take the last character)
		var opp = Opponent(myPlayer);
		var capturesLast = new Dictionary<Move, bool>();
		bool existsNonCapturing = false;

		foreach (var candidate in candidates)
		{
			var sim = new Model(rootModel);
			sim.MoveCharacter(candidate.From, candidate.To);
			int oppPiecesAfter = sim.GetAllCharacters(opp).Count;
			bool takesLast = (oppPiecesAfter == 0);
			capturesLast[candidate] = takesLast;
			if (!takesLast) existsNonCapturing = true;
		}

		// If there is at least one move that does NOT capture the opponent's last piece,
		// prefer those moves and ignore capturing-last moves. Otherwise allow capturing-last moves.
		var allowedCandidates = existsNonCapturing
			? candidates.Where(c => !capturesLast[c]).ToList()
			: new List<Move>(candidates);

		// Avoid moves that immediately result in a draw (repetition or stalemate).
		// If at least one allowed candidate does NOT produce a draw, prefer those.
		var causesDraw = new Dictionary<Move, bool>();
		bool existsNonDrawing = false;
		foreach (var candidate in allowedCandidates)
		{
			var sim = new Model(rootModel);
			sim.MoveCharacter(candidate.From, candidate.To);
			bool isDraw = sim.IsDraw(Opponent(myPlayer));
			causesDraw[candidate] = isDraw;
			if (!isDraw) existsNonDrawing = true;
		}

		if (existsNonDrawing)
		{
			allowedCandidates = allowedCandidates.Where(c => !causesDraw[c]).ToList();
		}

		double bestScore = double.NegativeInfinity;
		var bestMoves = new List<Move>();

		foreach (var candidate in allowedCandidates)
		{
			var sim = new Model(rootModel);
			sim.MoveCharacter(candidate.From, candidate.To);

			// Immediate win still takes precedence if it's not the disallowed "take last" move
			if (sim.FindWinner() == myPlayer)
				return candidate;

			double score = EvaluateState(sim, myPlayer);

			if (score > bestScore + 1e-9)
			{
				bestScore = score;
				bestMoves.Clear();
				bestMoves.Add(candidate);
			}
			else if (Math.Abs(score - bestScore) <= 1e-9)
			{
				bestMoves.Add(candidate);
			}
		}

		return bestMoves[_rng.Next(bestMoves.Count)];
	}

	/// <summary>
	/// Build normalized feature vector (same transformation used by EvaluateState).
	/// </summary>
	public static double[] BuildFeatureVector(Model model, Constants.Player myPlayer)
	{
		var opp = Opponent(myPlayer);

		double myPieces = model.GetAllCharacters(myPlayer).Count;
		double oppPieces = model.GetAllCharacters(opp).Count;

		double myTiles = CountTiles(model, myPlayer);
		double oppTiles = CountTiles(model, opp);

		double myMobility = TotalMobility(model, myPlayer);
		double oppMobility = TotalMobility(model, opp);

		double myJumpOps = TotalJumpOpportunities(model, myPlayer);
		double oppJumpOps = TotalJumpOpportunities(model, opp);

		double myVuln = CountVulnerablePieces(model, myPlayer);
		double oppVuln = CountVulnerablePieces(model, opp);

		var feats = new double[10];
		feats[0] = myPieces / MAX_PIECES;
		feats[1] = oppPieces / MAX_PIECES;
		feats[2] = myTiles / MAX_TILES;
		feats[3] = oppTiles / MAX_TILES;
		feats[4] = Math.Min(1.0, myMobility / MAX_MOBILITY);
		feats[5] = Math.Min(1.0, oppMobility / MAX_MOBILITY);
		feats[6] = Math.Min(1.0, myJumpOps / MAX_JUMP_OPS);
		feats[7] = Math.Min(1.0, oppJumpOps / MAX_JUMP_OPS);
		feats[8] = Math.Min(1.0, myVuln / MAX_VULN);
		feats[9] = Math.Min(1.0, oppVuln / MAX_VULN);

		return feats;
	}

	/// <summary>
	/// Predict scalar value for a feature vector (higher = better for the feature's player).
	/// </summary>
	public static double Predict(double[] feats)
	{
		double s = 0.0;
		lock (_weightsLock)
		{
			for (int i = 0; i < _weights.Length; i++)
				s += _weights[i] * feats[i];
			s += _bias;
		}
		return s;
	}

	public static double EvaluateState(Model model, Constants.Player myPlayer) => Predict(BuildFeatureVector(model, myPlayer));

	// ----- Training API -----

	public record TrainingExample(double[] Features, double Reward);

	/// <summary>
	/// Train with a batch of training examples using simple SGD on MSE.
	/// Reward should be in range [-1, +1]. Typical mapping: win=+1, draw=0, loss=-1.
	/// </summary>
	public static void TrainBatch(IReadOnlyList<TrainingExample> examples, double learningRate = 0.01, int epochs = 1)
	{
		if (examples == null || examples.Count == 0) return;

		lock (_weightsLock)
		{
			for (int e = 0; e < epochs; e++)
			{
				// Optionally shuffle for stochasticity
				var idxs = Enumerable.Range(0, examples.Count).ToArray();
				Shuffle(idxs);

				foreach (var idx in idxs)
				{
					var ex = examples[idx];
					double pred = 0.0;
					for (int i = 0; i < _weights.Length; i++) pred += _weights[i] * ex.Features[i];
					pred += _bias;

					double error = ex.Reward - pred; // target - pred
					// MSE gradient for weights is -2 * (target - pred) * x; we absorb factors into lr
					double delta = learningRate * error;
					for (int i = 0; i < _weights.Length; i++)
						_weights[i] += delta * ex.Features[i];
					_bias += delta;
				}
			}
		}
	}

	/// <summary>
	/// Convenience: Train from a finished game trace.
	/// trace: list of (modelAfterMove, playerWhoMoved). rewardForWinner: +1 for winner, 0 draw, -1 for loser.
	/// </summary>
	public static void TrainFromGame(IReadOnlyList<(Model ModelAfterMove, Constants.Player PlayerWhoMoved)> trace, Constants.Player winner, double learningRate = 0.01)
	{
		if (trace == null || trace.Count == 0) return;

		var examples = new List<TrainingExample>(trace.Count);
		foreach (var (modelAfter, mover) in trace)
		{
			double reward;
			if (winner == Constants.Player.Draw) reward = 0.0;
			else reward = (winner == mover) ? 1.0 : -1.0;

			var feats = BuildFeatureVector(modelAfter, mover);
			examples.Add(new TrainingExample(feats, reward));
		}

		TrainBatch(examples, learningRate, epochs: 1);
	}

	// ----- Persistence -----
	public static void SaveWeightsToFile(string path = WEIGHTS_FILENAME)
	{
		GD.Print("Writing neural weights to file...");

		// Resolve Godot style paths (user://, res://) to real filesystem paths
		string resolved = ResolveGodotPath(path);

		lock (_weightsLock)
		{
			var payload = new { weights = _weights, bias = _bias };
			var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });

			try
			{
				// Ensure directory exists
				string dir = Path.GetDirectoryName(resolved);
				if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
					Directory.CreateDirectory(dir);

				File.WriteAllText(resolved, json);
				GD.Print($"Neural weights written to: {resolved}");
			}
			catch (Exception ex)
			{
				GD.PrintErr($"Failed to write neural weights to '{resolved}': {ex.Message}");
				throw;
			}
		}
	}

	public static bool TryLoadWeightsFromFile(string path = WEIGHTS_FILENAME)
	{
		// Resolve Godot-style path to real filesystem path
		string resolved = ResolveGodotPath(path);

		if (!File.Exists(resolved))
		{
			GD.Print($"Neural weights file not found at: {resolved}");
			return false;
		}

		try
		{
			var json = File.ReadAllText(resolved);
			using var doc = JsonDocument.Parse(json);
			var root = doc.RootElement;

			if (!root.TryGetProperty("weights", out var wElem) || !root.TryGetProperty("bias", out var bElem))
			{
				GD.PrintErr("Neural weights file missing required properties.");
				return false;
			}

			var w = wElem.EnumerateArray().Select(e => e.GetDouble()).ToArray();
			var b = bElem.GetDouble();

			if (w.Length != _weights.Length)
			{
				GD.PrintErr($"Neural weights length mismatch: file has {w.Length}, expected {_weights.Length}");
				return false;
			}

			lock (_weightsLock)
			{
				for (int i = 0; i < _weights.Length; i++) _weights[i] = w[i];
				_bias = b;
			}

			GD.Print($"Neural weights loaded from: {resolved}");
			return true;
		}
		catch (Exception ex)
		{
			GD.PrintErr($"Failed to load neural weights from '{resolved}': {ex.Message}");
			return false;
		}
	}

	// Helper resolves Godot virtual paths (user://, res://) to real filesystem paths
	private static string ResolveGodotPath(string path)
	{
		if (string.IsNullOrEmpty(path)) return path;
		try
		{
			// ProjectSettings.GlobalizePath converts 'user://' and 'res://' into OS paths
			if (path.StartsWith("user://", StringComparison.OrdinalIgnoreCase) || path.StartsWith("res://", StringComparison.OrdinalIgnoreCase))
			{
				return ProjectSettings.GlobalizePath(path);
			}
		}
		catch
		{
			// Fallback to raw path if ProjectSettings not available for some reason
		}
		return path;
	}

	// ----- Helpers & features reused from previous implementation -----

	private static Constants.Player Opponent(Constants.Player p) => p == Constants.Player.Hero ? Constants.Player.Enemy : Constants.Player.Hero;

	private static int CountTiles(Model m, Constants.Player p)
	{
		int count = 0;
		var s = m.StringifyState();
		for (int i = 49; i < 49 + 36 && i < s.Length; i++)
			if ((p == Constants.Player.Hero && s[i] == 'x') || (p == Constants.Player.Enemy && s[i] == 'o'))
				count++;
		return count;
	}

	private static int TotalMobility(Model m, Constants.Player p)
	{
		int total = 0;
		foreach (var pos in m.GetAllCharacters(p))
			total += m.FindValidMoves(pos, p).Count;
		return total;
	}

	private static int TotalJumpOpportunities(Model m, Constants.Player p)
	{
		int total = 0;
		foreach (var pos in m.GetAllCharacters(p))
		{
			foreach (var move in m.FindValidMoves(pos, p))
			{
				if (Math.Abs(pos.X - move.X) == 2 || Math.Abs(pos.Y - move.Y) == 2)
					total++;
			}
		}
		return total;
	}

	private static int CountVulnerablePieces(Model m, Constants.Player p)
	{
		var opponent = Opponent(p);
		int vulnerable = 0;
		foreach (var pos in m.GetAllCharacters(p))
		{
			if (IsPositionVulnerableToOpponentJump(m, pos, opponent)) vulnerable++;
		}
		return vulnerable;
	}

	private static bool IsPositionVulnerableToOpponentJump(Model simModel, Vector2I pos, Constants.Player opponent)
	{
		foreach (var from in simModel.GetAllCharacters(opponent))
		{
			bool fromIsOrthAdjacent =
				(Math.Abs(from.X - pos.X) == 1 && from.Y == pos.Y) ||
				(Math.Abs(from.Y - pos.Y) == 1 && from.X == pos.X);
			if (!fromIsOrthAdjacent) continue;

			var landing = new Vector2I(pos.X * 2 - from.X, pos.Y * 2 - from.Y);
			if (landing.X < 0 || landing.X > 6 || landing.Y < 0 || landing.Y > 6) continue;
			if (simModel.PlayerAt(landing) != Constants.Player.None) continue;

			var jumped = Model.FindJumpedCharacter(from, landing);
			if (jumped != null && jumped.Value == pos) return true;
		}
		return false;
	}

	private static void Shuffle(int[] a)
	{
		for (int i = a.Length - 1; i > 0; i--)
		{
			int j = _rng.Next(i + 1);
			int tempj = a[j];
			int tempi = a[i];
			a[j] = tempi;
			a[i] = tempj;
		}
	}
}
