using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class NetworkPlayer : IPlayer
{
	private Model ModelInstance;
	private Grid ViewInstance;
	private Constants.Player Player;
	private string LastState;
	
	// Emit this when a network move is detected
	public event EventHandler<(Vector2I, Vector2I)> MoveFound;

	// Called by the controller to give the player the live model and view
	public void SetupTurn(Model model, Grid grid)
	{
		ModelInstance = model;
		ViewInstance = grid;
		// initialize last known state from the controller model if available
		try
		{
			LastState = ModelInstance.StringifyState();
		}
		catch
		{
			LastState = null;
		}
	}

	public void SetPlayer(Constants.Player player)
	{
		Player = player;
	}

	// Network player does not handle local click/hover input; no-ops per IPlayer contract
	public void OnClick(Vector2I pos) { /* no-op for network-controlled player */ }
	public void MouseEntered(Vector2I pos) { /* no-op for network-controlled player */ }
	public void MouseExited(Vector2I pos) { /* no-op for network-controlled player */ }

	// Called by networking code (Host/Guest) when a new game state string arrives.
	// Compares the last seen state to the incoming one and emits MoveFound when a
	// single pin move for this player can be determined.
	public void ReceiveState(string state)
	{
		if (string.IsNullOrEmpty(state)) return;
		if (state == LastState) return;

		// If we have no previous state, just set it and return (can't diff)
		if (LastState == null)
		{
			LastState = state;
			return;
		}

		// Build two transient models to diff player positions
		var oldModel = new Model();
		var newModel = new Model();

		try
		{
			oldModel.UpdateState(LastState);
			newModel.UpdateState(state);
		}
		catch (Exception ex)
		{
			// Malformed state string; ignore
			GD.PrintErr($"Problem updating Models: {ex}");
			LastState = state;
			return;
		}

		Vector2I? from = null;
		Vector2I? to = null;

		for (int x = 0; x < 7; x++)
		{
			for (int y = 0; y < 7; y++)
			{
				var pos = new Vector2I(x, y);
				var oldP = oldModel.PlayerAt(pos);
				var newP = newModel.PlayerAt(pos);

				// Detect the moved pin for this player: former location -> empty
				if (oldP == Player && newP != Player)
				{
					// consider first such as the from
					if (from == null) from = pos;
				}

				// Detect the new location for this player's pin: former empty -> now player's
				if (oldP != Player && newP == Player)
				{
					if (to == null) to = pos;
				}
			}
		}

		// If we found a clear from->to, emit a MoveFound so the controller will apply it
		if (from != null && to != null)
		{
			Callable.From(() => MoveFound?.Invoke(this, (from.Value, to.Value))).CallDeferred();
		}

		// Always update last known state
		LastState = state;
	}

    public void Stop() {/* forced stop cannot happen in a network game */}

}
