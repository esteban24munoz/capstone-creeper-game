using Godot;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Security;
using System.Threading.Tasks;

public partial class NewController : Node2D
{
	[Export] public AudioStream[] MusicArray = [];
	private Stack<AudioStream> MusicStack = [];
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
		Music.Finished += MusicFinished;
		Music.Stream = RandomizeMusic();
		Music.Play();
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
			GameUI.EnableBoardToggleButton();
			ActivePlayer = Constants.Player.None;

			Music.Stop();
			DrawMusic.Play();
		
		}
		else if (Winner != Constants.Player.None)
		{
			ActivePlayer = Constants.Player.None;

			Music.Stop();
			
			// -- AI & NETWORK LOGIC (Show Defeat Screens) --
			if (Globals.gameType == Globals.GameType.AI || Globals.gameType == Globals.GameType.Network)
			{
				// Local player is Fellowship, but Enemy won -> Frodo Loses
				if (Constants.HeroPlayer is LocalPlayer && Winner == Constants.Player.Enemy)
				{
					GameUI.ShowFrodoLosesScreen();
					SauronWin.Play(); 
				}
				else if (Constants.EnemyPlayer is LocalPlayer && Winner == Constants.Player.Hero)
				{
					GameUI.ShowSauronLosesScreen();
					FrodoWin.Play(); 
				}
		
				// Local player won against the AI/Network Opponent -> Show normal Win Screen
				else
				{
					GameUI.ShowWinScreen(Winner);
					if (Winner == Constants.Player.Hero) FrodoWin.Play();
					else SauronWin.Play();
				}
			}
			// -- LOCAL MULTIPLAYER LOGIC (Hotseat 1v1) --
			else if (Globals.gameType == Globals.GameType.Local)
			{
				// Two humans are playing locally, so just announce the winner
				GameUI.ShowWinScreen(Winner);
				if (Winner == Constants.Player.Hero) FrodoWin.Play();
				else SauronWin.Play();
			}

			// Show the CheckButton so the player can toggle the board
			GameUI.EnableBoardToggleButton();
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


	private AudioStream RandomizeMusic(AudioStream current = null)
	{
		if (!MusicStack.TryPop(out AudioStream track))
		{
			do 
			{
				Random.Shared.Shuffle(MusicArray);
				MusicStack = new(MusicArray);
				track = MusicStack.Pop();
			} while (track == current);
		}
		
		return track;
	}

	void MusicFinished()
	{
		Music.Play();
		Tween tween = CreateTween();
		tween.TweenProperty(Music, "volume_linear", 0, 5).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
		tween.Finished += () =>
		{
			Music.Stream = RandomizeMusic(Music.Stream);
			Music.VolumeLinear = VolumeManager.MusicVolume;
			Music.Play();
		};
	}
}
