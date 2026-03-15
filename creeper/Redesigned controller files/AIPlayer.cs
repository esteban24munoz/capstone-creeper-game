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
	MTCS_Pure2.MonteCarloStrategy currentNode = new MTCS_Pure2.MonteCarloStrategy();

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

		// Fire-and-forget background computation
		_ = ComputeAndEmitMoveAsync();
	}

	private async Task ComputeAndEmitMoveAsync()
	{
		try
		{
			// Clone model to avoid accidental shared-state mutation during heavy compute
			var modelClone = new Model(_model);

			// Run the MCTS selection on a thread-pool thread
			var bestMove = await Task.Run(() =>
			{
				// The MonteCarloStrategy may expect a fresh model instance
				try
				{
					return currentNode.ChooseBestMove(modelClone, _player);
				}
				catch (Exception ex)
				{
					GD.PrintErr($"[AIPlayer] ChooseBestMove error (background): {ex.Message}");
					return default(MTCS_Pure2.Move);
				}
			}).ConfigureAwait(false);

			// Check if bestMove is not the default value (since Move is a struct)
			if (!bestMove.Equals(default(MTCS_Pure2.Move)))
			{
				// Schedule the event invocation on the main thread (Godot) so subscribers can safely interact with scene nodes.
				Callable.From(() => MoveFound?.Invoke(this, (bestMove.From, bestMove.To))).CallDeferred();
			}
			else
			{
				GD.PrintErr("[AIPlayer] Compute finished but no move returned.");
			}
		}
		catch (Exception ex)
		{
			GD.PrintErr($"[AIPlayer] ComputeAndEmitMoveAsync error: {ex.Message}");
		}
	}

	// LocalPlayer-only UI callbacks - AI ignores them
	public void OnClick(Vector2I pos) { }
	public void MouseEntered(Vector2I pos) { }
	public void MouseExited(Vector2I pos) { }

	// Network player uses this to push state updates from server. 
	public void ReceiveState(string state) { }
}
