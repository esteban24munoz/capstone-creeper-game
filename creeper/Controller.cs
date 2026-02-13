using Godot;
using System;
using System.Collections;
using System.Collections.Generic;

public partial class Controller : Node2D
{
	Constants.Player ActivePlayer = Constants.Player.Hero;
	Constants.Player Winner = Constants.Player.None;
	Vector2I? SelectedCharacter = null;
	Grid ViewInstance;
	readonly Model ModelInstance = new();
	InGameScene GameUI;
	AudioStreamPlayer2D Music, FrodoWin, SauronWin;

	public override void _Ready()
	{
		ViewInstance = GetNode<Grid>("Grid");
		ViewInstance.CharacterClick += OnClick;
		ViewInstance.CharacterMouseEntered += MouseEntered;
		ViewInstance.CharacterMouseExited += MouseExited;
		ViewInstance.MoveFinished += CharacterMoveFinished;

		GameUI = GetNode<InGameScene>("GameUI");

		Music = GetNode<AudioStreamPlayer2D>("Music/Battle");
		FrodoWin = GetNode<AudioStreamPlayer2D>("Music/FrodoWin");
		SauronWin = GetNode<AudioStreamPlayer2D>("Music/SauronWin");
		ViewInstance.StartCharacterAnimations(ModelInstance.GetAllCharacters(ActivePlayer));
	}

	//This is the main game loop
	//This method is called every time a character is clicked
	void OnClick(Vector2I pos)
	{
		//If there is no selected character and the clicked character matches the active player,
		//make the clicked character selected and instantiate ghosts on the valid spaces
		if (SelectedCharacter == null)
		{
			if (ModelInstance.PlayerAt(pos) != ActivePlayer) return;
			SelectCharacter(pos);
		}
		else
		{
			if (ModelInstance.PlayerAt(pos) == ActivePlayer)
			{
				//unselect the selected character
				ViewInstance.RemoveGhosts();

				if (pos == SelectedCharacter)
				{
					SelectedCharacter = null;
				}
				else
				{
					SelectCharacter(pos);
				}
			}
			else if (ViewInstance.IsGhost(pos))
			{
				//move the character in both the model and the view
				ModelInstance.MoveCharacter(SelectedCharacter.Value, pos);

				ViewInstance.MoveCharacter(SelectedCharacter.Value, pos);
				Vector2I? jumped = Model.FindJumpedCharacter(SelectedCharacter.Value, pos);

				jumped = ModelInstance.FindJumpedHex(SelectedCharacter.Value, pos);
				if (jumped != null)
				{
					ViewInstance.ChangeTile(jumped.Value, ActivePlayer);
					Winner = ModelInstance.FindWinner();
				}

				ViewInstance.RemoveGhosts();
				SelectedCharacter = null;
				NewTurn();

				if (ModelInstance.IsDraw(ActivePlayer))
				{
					GD.Print("Draw");
					ActivePlayer = Constants.Player.None;
				}

				if (Winner != Constants.Player.None)
				{
					GD.Print("Winner: ", Winner);
					ActivePlayer = Constants.Player.None;
				}
			}
		}
	}

	private void SelectCharacter(Vector2I pos)
	{
		List<Vector2I> moves = ModelInstance.FindValidMoves(pos, ActivePlayer);
		ViewInstance.CreateGhosts(pos, moves);

		SelectedCharacter = pos;
	}

	void MouseEntered(Vector2I pos)
	{
		if ((ModelInstance.PlayerAt(pos) == ActivePlayer) || 
		(SelectedCharacter != null && ViewInstance.IsGhost(pos)))
		{
			ViewInstance.Hover(pos);
		}
	}

	void MouseExited(Vector2I pos)
	{
	   if ((ModelInstance.PlayerAt(pos) == ActivePlayer) || 
		(SelectedCharacter != null && ViewInstance.IsGhost(pos)))
		{
			ViewInstance.StopHover(pos);
		}
	}

	void CharacterMoveFinished()
	{
		if (ModelInstance.IsDraw(ActivePlayer))
		{
			GameUI.ShowWinScreen(Constants.Player.None);
			ActivePlayer = Constants.Player.None;
		}

		if (Winner != Constants.Player.None)
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
	}

	void NewTurn()
	{
		if (ActivePlayer == Constants.Player.Hero)
		{
			ActivePlayer = Constants.Player.Enemy;
		}
		else if (ActivePlayer == Constants.Player.Enemy)
		{
			ActivePlayer = Constants.Player.Hero;
		}
	}
}
