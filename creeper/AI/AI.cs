using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// Automated training harness that runs head-to-head games between:
/// - NeuralNetStrategy (greedy perceptron)
/// - MTCS_Pure2.MonteCarloStrategy (MCTS)
///
/// This node does NOT start automatically. Call `StartTrainingAsync(...)` or
/// `RunTrainingGamesAsync(...)` from a headless entrypoint or a tool script.
/// </summary>
public partial class AI : Node2D
{
	// Tunable parameters (override when calling)
	public int GamesToPlay { get; set; } = 200;
	public int MctsSecondsBudget { get; set; } = 4;
	public bool AlternateSides { get; set; } = true;
	public double LearningRate { get; set; } = 0.01;
	public int SaveEveryNGames { get; set; } = 1;
	
	public async void _on_button_pressed()
	{
		_= StartTrainingAsync();
	}

	/// <summary>
	/// Convenience entry: starts training on a thread-pool thread and returns immediately.
	/// Use this from a headless launcher or an editor tool. Does not touch UI nodes.
	/// </summary>
	public Task StartTrainingAsync(int games = -1, int mctsSeconds = -1, bool alternateSides = true, double learningRate = 0.01, int saveEveryN = 1)
	{
		if (games > 0) GamesToPlay = games;
		if (mctsSeconds > 0) MctsSecondsBudget = mctsSeconds;
		AlternateSides = alternateSides;
		LearningRate = learningRate;
		SaveEveryNGames = saveEveryN;

		return RunTrainingGamesAsync(GamesToPlay, MctsSecondsBudget, AlternateSides, LearningRate, SaveEveryNGames);
	}

	/// <summary>
	/// Run multiple training games on a thread-pool thread. Does not block the caller.
	/// </summary>
	public Task RunTrainingGamesAsync(int games, int mctsSecondsBudget = 2, bool alternateSides = true, double learningRate = 0.01, int saveEveryN = 1)
	{
		return Task.Run(() =>
		{
			try
			{
				// Attempt to load existing weights first
				NeuralNetStrategy.TryLoadWeightsFromFile("neural_weights.json");

				int nnWins = 0, mctsWins = 0, draws = 0;
				bool nnPlaysHero = true;

				for (int g = 1; g <= games; g++)
				{
					GD.Print($"[AI.Train] Game {g}/{games} starting (NN as {(nnPlaysHero ? "Hero" : "Enemy")})");

					var (winner, trace) = PlaySingleGame(mctsSecondsBudget, nnPlaysHero, g);

					// Train if we have a trace
					if (trace != null && trace.Count > 0)
					{
						NeuralNetStrategy.TrainFromGame(trace, winner, learningRate);
						GD.Print("[AI.Train] Trained from game trace.");
					}
					else
					{
						GD.Print("[AI.Train] No training trace for this game.");
					}

					// Save periodically
					if (saveEveryN > 0 && g % saveEveryN == 0)
					{
						NeuralNetStrategy.SaveWeightsToFile("neural_weights.json");
						GD.Print($"[AI.Train] Saved weights after game {g}.");
					}

					// Update stats
					if (winner == Constants.Player.Hero)
					{
						if (nnPlaysHero) nnWins++; else mctsWins++;
					}
					else if (winner == Constants.Player.Enemy)
					{
						if (!nnPlaysHero) nnWins++; else mctsWins++;
					}
					else if (winner == Constants.Player.Draw)
					{
						draws++;
					}

					GD.Print($"[AI.Train] Result: {winner} | Totals -> NN: {nnWins}, MCTS: {mctsWins}, Draws: {draws}");

					if (alternateSides) nnPlaysHero = !nnPlaysHero;
				}

				// Final save
				NeuralNetStrategy.SaveWeightsToFile("neural_weights.json");
				GD.Print("[AI.Train] Training complete. Final weights saved.");
				GD.Print($"[AI.Train] Final totals -> NN: {nnWins}, MCTS: {mctsWins}, Draws: {draws}");
			}
			catch (Exception ex)
			{
				GD.PrintErr($"[AI.Train] Unhandled exception: {ex}");
			}
		});
	}

	/// <summary>
	/// Play a single automated game between NeuralNet (greedy) and MTCS_Pure2.
	/// Returns the winner and the trace suitable for NeuralNetStrategy.TrainFromGame.
	/// </summary>
	private (Constants.Player Winner, List<(Model ModelAfterMove, Constants.Player PlayerWhoMoved)> Trace) PlaySingleGame(int mctsSecondsBudget, bool nnPlaysHero, int gameIndex)
	{
		var model = new Model();
		var trace = new List<(Model, Constants.Player)>();

		Constants.Player current = Constants.Player.Hero;
		bool gameOver = false;
		Constants.Player winner = Constants.Player.None;
		int plyLimit = 200; // safety cap

		int plies = 0;

		GD.Print($"[AI.Train][Game {gameIndex}] Starting play loop. NNPlaysHero={nnPlaysHero}");

		while (!gameOver && plies < plyLimit)
		{
			plies++;

			bool nnTurn = (current == Constants.Player.Hero && nnPlaysHero) || (current == Constants.Player.Enemy && !nnPlaysHero);
			string actor = nnTurn ? "NN" : "MCTS";

			// Choose and apply move
			if (nnTurn)
			{
				try
				{
					var mv = NeuralNetStrategy.ChooseBestMove(model, current);
					model.MoveCharacter(mv.From, mv.To);
					//GD.Print($"[AI.Train][Game {gameIndex}][Ply {plies}] {actor} ({current}) moved {mv}");
				}
				catch (InvalidOperationException)
				{
					GD.Print($"[AI.Train][Game {gameIndex}][Ply {plies}] {actor} ({current}) has no legal moves.");
				}
				catch (Exception ex)
				{
					GD.PrintErr($"[AI.Train][Game {gameIndex}] Neural choose error: {ex.Message}");
				}
			}
			else
			{
				try
				{
					var mcts = new MTCS_Pure2.MonteCarloStrategy(mctsSecondsBudget);
					var mv = mcts.ChooseBestMove(model, current);
					model.MoveCharacter(mv.From, mv.To);
					//GD.Print($"[AI.Train][Game {gameIndex}][Ply {plies}] {actor} ({current}) moved {mv}");
				}
				catch (InvalidOperationException)
				{
					GD.Print($"[AI.Train][Game {gameIndex}][Ply {plies}] {actor} ({current}) has no legal moves.");
				}
				catch (Exception ex)
				{
					GD.PrintErr($"[AI.Train][Game {gameIndex}] MCTS choose error: {ex.Message}");
				}
			}

			// Record snapshot after move
			trace.Add((new Model(model), current));

			// Check for winner
			var maybeWinner = model.FindWinner();
			if (maybeWinner != Constants.Player.None)
			{
				winner = maybeWinner;
				gameOver = true;
				GD.Print($"[AI.Train][Game {gameIndex}] Winner detected: {winner} at ply {plies}");
				break;
			}

			// Switch player
			current = current == Constants.Player.Hero ? Constants.Player.Enemy : Constants.Player.Hero;

			// Check draw for the new active player
			if (model.IsDraw(current))
			{
				winner = Constants.Player.Draw;
				gameOver = true;
				GD.Print($"[AI.Train][Game {gameIndex}] Draw detected at ply {plies}");
				break;
			}
		}

		if (!gameOver)
		{
			// Ply limit reached -> treat as draw
			winner = Constants.Player.Draw;
			GD.PrintErr($"[AI.Train][Game {gameIndex}] Ply limit ({plyLimit}) reached; treating as draw.");
		}
		else
		{
			GD.Print($"[AI.Train][Game {gameIndex}] Game finished after {plies} plies. Winner: {winner}");
		}

		return (winner, trace);
	}
}
