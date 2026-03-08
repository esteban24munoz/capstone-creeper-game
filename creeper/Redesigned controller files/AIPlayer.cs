using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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
	// This implementation computes a move immediately (synchronously) and emits MoveFound.
	public void SetupTurn(Model model, Grid grid)
	{
		_model = model;
		_grid = grid;
		
		//currentNode = new MonteCarloStrategy();
		MTCS_Pure2.Move bestMove = currentNode.ChooseBestMove(_model, _player);
		GD.Print(bestMove);
		MoveFound?.Invoke(this, (bestMove.From, bestMove.To));
	}

	// LocalPlayer-only UI callbacks - AI ignores them
	public void OnClick(Vector2I pos) { }
	public void MouseEntered(Vector2I pos) { }
	public void MouseExited(Vector2I pos) { }

	// Network player uses this to push state updates from server. - AI ignores them
	public void ReceiveState(string state) { }
}
