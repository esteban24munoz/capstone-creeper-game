using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

public class AIPlayer : IPlayer
{
	// IPlayer event - raised when AI has selected a move
	public event EventHandler<(Vector2I, Vector2I)> MoveFound;

	Constants.Player _player = Constants.Player.None;
	Model _model;
	Grid _grid;

	// time budget (ms) used for MCTS; tune as needed
	private readonly int _mctsTimeMs = 4000;

	public void SetPlayer(Constants.Player player)
	{
		_player = player;
	}

	// Called by NewController to give the AI the current model & view.
	// This implementation computes a move asynchronously and emits MoveFound on the main thread.
	public void SetupTurn(Model model, Grid grid)
	{
		_model = model;
		_grid = grid;

		// Quick guard: if no pieces or no valid moves, do nothing
		var pieces = _model.GetAllCharacters(_player);
		bool hasMove = false;
		foreach (var p in pieces)
		{
			if (_model.FindValidMoves(p, _player).Count > 0) { hasMove = true; break; }
		}
		if (!hasMove) return;
		
		////Wait until LocalPlayer has closed Help Menu
		//if (Globals.gameType == Globals.GameType.AI && Constants.HeroPlayer is AIPlayer)
		//{
			//while (!Globals.isHelpClosed)
			//{
				//await Task.Delay(500);
			//}
		//}

		// Fire-and-forget background computation
		_ = ComputeAndEmitMoveAsync();
	}

	private async Task ComputeAndEmitMoveAsync()
	{
		//Wait until LocalPlayer has closed Help Menu
		if (Constants.HeroPlayer is AIPlayer)
		{
			while (!Globals.isHelpClosed)
			{
				await Task.Delay(500);
			}
		}
		
		try
		{
			// Clone model to avoid accidental shared-state mutation during heavy compute
			var modelClone = new Model(_model);

			// Convert ms budget to seconds for the strategy constructors
			int secondsBudget = Math.Max(1, (int)Math.Ceiling(_mctsTimeMs / 1000.0));

			// Run the MCTS selection on a thread-pool thread and return a simple (from,to) tuple
			var bestMove = await Task.Run(() =>
			{
				try
				{
					// Choose strategy based on Globals.difficulty
					switch (Globals.difficulty)
					{
						case Globals.AIDifficulty.Easy:
						{
							// Easy: use MTCS_Pure (lighter strategy)
							var strat = new MTCS_Pure.MonteCarloStrategy(secondsBudget);
							var mv = strat.ChooseBestMove(modelClone, _player);
							GD.Print($"[AI] Easy strategy selected {mv}");
							return (mv.From, mv.To);
						}
						case Globals.AIDifficulty.Medium:
						{
							// Medium: use MTCS_Pure2 (more advanced)
							var strat = new MTCS_Pure2.MonteCarloStrategy(secondsBudget);
							var mv = strat.ChooseBestMove(modelClone, _player);
							GD.Print($"[AI] Medium strategy selected {mv}");
							return (mv.From, mv.To);
						}
						case Globals.AIDifficulty.Hard:
						default:
						{
							if (_player == Constants.Player.Hero)
							{
								var strat = new MTCS_Pure.MonteCarloStrategy(secondsBudget);
								var mv = strat.ChooseBestMove(modelClone, _player);
								GD.Print($"[AI] Easy strategy selected {mv}");
								return (mv.From, mv.To);
							}
							else {
								// Hard/default: use MTCS_Pure2 with same budget (could be tuned larger)
								var strat = new MTCS_Pure2.MonteCarloStrategy(secondsBudget);
								var mv = strat.ChooseBestMove(modelClone, _player);
								GD.Print($"[AI] Hard strategy selected {mv}");
								return (mv.From, mv.To);
							}
						}
					}
				}
				catch (Exception ex)
				{
					GD.PrintErr($"[AI] ChooseBestMove error (background): {ex.Message}");
					return (new Vector2I(-1, -1), new Vector2I(-1, -1));
				}
			}).ConfigureAwait(false);

			// Validate move (use sentinel -1,-1 to indicate failure)
			if (bestMove.Item1.X >= 0 && bestMove.Item1.Y >= 0)
			{
				// Schedule the event invocation on the main thread (Godot) so subscribers can safely interact with scene nodes.
				Callable.From(() => MoveFound?.Invoke(this, (bestMove.Item1, bestMove.Item2))).CallDeferred();
			}
			else
			{
				GD.PrintErr("[AI] Compute finished but no move returned.");
			}
		}
		catch (Exception ex)
		{
			GD.PrintErr($"[AI] ComputeAndEmitMoveAsync error: {ex.Message}");
		}
	}

	// LocalPlayer-only UI callbacks - AI ignores them
	public void OnClick(Vector2I pos) { }
	public void MouseEntered(Vector2I pos) { }
	public void MouseExited(Vector2I pos) { }

	// Network player uses this to push state updates from server. 
	public void ReceiveState(string state) { }
}
