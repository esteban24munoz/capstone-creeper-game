using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class NetworkPlayer : IPlayer
{
	private Model ModelInstance;
	private Grid ViewInstance;
	private Constants.Player Player;
	private string? LastState;// = ".oo.xx.o.....xo.....x.......x.....ox.....o.xx.oo.o....x........................x....o";
	
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

//	while (!Globals.cts.Token.IsCancellationRequested)
//	{
//		try
//		{
//			var state = await Globals.hostClient.GetGameStateAsync(_created.GameId, Globals.cts.Token);
//			GD.Print($"[Host] Game status: {state.Status}, turn: {state.Turn}, lastActive: {state.LastActive}");
//			// Example: make a sample move when it's host's turn.
//			//if (state.Status == "in_progress" && state.Turn == "host")
//			//{
//				// Replace with your real state string
//				//var exampleState = ".oo.xx...";
//				//await _client.MakeMoveAsync(_created.GameId, _created.HostToken, exampleState, Globals.cts.Token);
//				//GD.Print("[Host] Submitted a move.");
//			//}
//		}
//		catch (Exception ex)
//		{
//			GD.PrintErr($"[Host] Poll error: {ex.Message}");
//		}
//		await Task.Delay(TimeSpan.FromSeconds(2), Globals.cts.Token);
//	}

	// Called by networking code (Host/Guest) when a new game state string arrives.
	// Compares the last seen state to the incoming one and emits MoveFound when a
	// single pin move for this player can be determined.
	public void ReceiveState(string state)
	{
		GD.Print($"Last State: {LastState} \t Incoming state: {state}");
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
			GD.Print($"Models old and new have been updated in state");
		}
		catch (Exception ex)
		{
			// Malformed state string; ignore
			GD.Print($"Problem updating Models: {ex}");
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
			GD.Print($"RecieveState func: Move Found: {from} to {to}");
			if (this == Constants.HeroPlayer)
			{
				GD.Print("Host is network");
			}
			else if (this == Constants.EnemyPlayer)
			{
				GD.Print("Guest is network");
			}
			MoveFound?.Invoke(this, (from.Value, to.Value));
		}

		// Always update last known state
		LastState = state;
	}
}
