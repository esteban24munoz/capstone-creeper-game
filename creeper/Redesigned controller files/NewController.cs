using Godot;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class NewController : Node2D
{
	Constants.Player ActivePlayer = Constants.Player.Hero;
	IPlayer ActivePlayerObject = Constants.HeroPlayer;
	Constants.Player Winner = Constants.Player.None;
	Grid ViewInstance;
	readonly Model ModelInstance = new();
	InGameScene GameUI;
	AudioStreamPlayer2D Music, FrodoWin, SauronWin, DrawMusic;

	public override void _Ready()
	{
		//tell the objects what player they belong to and attach MakeMove to each
		Constants.HeroPlayer.SetPlayer(Constants.Player.Hero);
		Constants.EnemyPlayer.SetPlayer(Constants.Player.Enemy);
		Constants.HeroPlayer.MoveFound += MakeMove;
		Constants.EnemyPlayer.MoveFound += MakeMove;

		ViewInstance = GetNode<Grid>("Grid");
		ViewInstance.MoveFinished += CharacterMoveFinished;

		//attach three view signals to the two player objects
		ViewInstance.CharacterClick += Constants.HeroPlayer.OnClick;
		ViewInstance.CharacterClick += Constants.EnemyPlayer.OnClick;
		ViewInstance.CharacterMouseEntered += Constants.HeroPlayer.MouseEntered;
		ViewInstance.CharacterMouseEntered += Constants.EnemyPlayer.MouseEntered;
		ViewInstance.CharacterMouseExited += Constants.HeroPlayer.MouseExited;
		ViewInstance.CharacterMouseExited += Constants.EnemyPlayer.MouseExited;

		GameUI = GetNode<InGameScene>("GameUI");

		Music = GetNode<AudioStreamPlayer2D>("Music/Battle");
		FrodoWin = GetNode<AudioStreamPlayer2D>("Music/FrodoWin");
		SauronWin = GetNode<AudioStreamPlayer2D>("Music/SauronWin");
		DrawMusic = GetNode<AudioStreamPlayer2D>("Music/DrawMusic");
		ViewInstance.StartCharacterAnimations(ModelInstance.GetAllCharacters(ActivePlayer));

		//start the first turn
		ActivePlayerObject.SetupTurn(ModelInstance, ViewInstance);
	}

	//this is called whenever a player object wants to make a move
	async void MakeMove(object sender, (Vector2I from, Vector2I to) move)
	{
		//only accept moves from the active player
		if (sender != ActivePlayerObject) return;

		//update the model and view
		ModelInstance.MoveCharacter(move.from, move.to);
		ViewInstance.MoveCharacter(move.from, move.to);

		Vector2I? jumped = ModelInstance.FindJumpedHex(move.from, move.to);
		if (jumped != null)
		{
			ViewInstance.ChangeTile(jumped.Value, ActivePlayer);
			Winner = ModelInstance.FindWinner();
		}

		// If this is a networked game and the move originated locally, submit state to server
		if (Globals.gameType == Globals.GameType.Network && sender is LocalPlayer)
		{
			// Capture state after applying move (same format other networking code uses)
			string state = ModelInstance.StringifyState();
			if (ActivePlayer == Constants.Player.Hero)
				state += 'x';
			else if (ActivePlayer == Constants.Player.Enemy)
				state += 'o';
			GD.Print($"Submitting state: {state}");
			_ = SubmitMoveToServerAsync(state); // fire-and-forget; errors are logged inside helper
		}
	}

	// Send move to appropriate client (host/guest) without blocking the main thread.
	private async Task SubmitMoveToServerAsync(string state)
	{
		try
		{
			if (Constants.HeroPlayer is LocalPlayer)
			{
				await Globals.hostClient.MakeMoveAsync(Globals.gameId, Globals.token, state, Globals.cts.Token).ConfigureAwait(false);
				GD.Print("[Network] Host submitted move.");
			}
			else if (Constants.EnemyPlayer is LocalPlayer)
			{
				await Globals.guestClient.MakeMoveAsync(Globals.gameId, Globals.token, state, Globals.cts.Token).ConfigureAwait(false);
				GD.Print("[Network] Guest submitted move.");
			}
			else
			{
				GD.PrintErr("[Network] No player token available when trying to submit move.");
			}
		}
		catch (Exception ex)
		{
			GD.PrintErr($"[Network] SubmitMove error: {ex.Message}");
		}
	}

	//this is called when the view is finished all move animations
	void CharacterMoveFinished()
	{
		//check for draw or win, else start a new turn
		if (ModelInstance.IsDraw(ActivePlayer))
		{
			GameUI.ShowWinScreen(Constants.Player.None);
			ActivePlayer = Constants.Player.None;

			Music.Stop();
			DrawMusic.Play();
		}
		else if (Winner != Constants.Player.None)
		{
			GameUI.ShowWinScreen(Winner);
			ActivePlayer = Constants.Player.None;

			Music.Stop();
			switch (Winner)
			{
				case Constants.Player.Hero:
					FrodoWin.Play();
					break;
				case Constants.Player.Enemy:
					SauronWin.Play();
					break;
			}
		}
		else 
			NewTurn();
	}

	void NewTurn()
	{
		//switch to next player
		if (ActivePlayer == Constants.Player.Hero)
		{
			ActivePlayer = Constants.Player.Enemy;
			ActivePlayerObject = Constants.EnemyPlayer;
		}
		else if (ActivePlayer == Constants.Player.Enemy)
		{
			ActivePlayer = Constants.Player.Hero;
			ActivePlayerObject = Constants.HeroPlayer;
		}

		//tell new active player to setup
		ActivePlayerObject.SetupTurn(ModelInstance, ViewInstance);

		// If the new active player is a network player, you may want to show a "Waiting for opponent" UI.
		// Example (if you have such method): GameUI.SetWaiting(ActivePlayerObject is NetworkPlayer);
	}
}
