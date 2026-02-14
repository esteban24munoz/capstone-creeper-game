using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;

public partial class AI : Node2D
{
	public Model AIGame = new Model();
	public Constants.Player currentPlayer = Constants.Player.Hero;
	string gameState = "";
	int gameNumber = 1;

	public override void _Ready()
	{
		GD.Print("Starting test");
		for (gameNumber = 0; gameNumber < 10; gameNumber++)
		{
			//GD.Print($"Game: {i+1}");
			bool isGameDone = PlayGame();
			//GD.Print($"Game: {i+1} is done");
			//Need to reset the game to a fresh game and save old game data for stragey
			AIGame = new Model();
			currentPlayer = Constants.Player.Hero; 
		}
		GD.Print($"Game {gameNumber+1} done");
	}
	
	Vector2I SelectRandomPiece()
	{
		List<Vector2I> playersPieces = new();
		
		//gemini prompt "c # grid number of cells"
		for (int r = 0; r < 7; r++) 
		{
			for (int c = 0; c < 7; c++) 
			{
				Vector2I pos = new Vector2I(); //check to see if I can just pass in int
				pos.X = r;
				pos.Y = c;
				
				if (AIGame.PlayerAt(pos) == currentPlayer)
				{
					playersPieces.Add(pos);
				}
			}
		}
		//GD.Print("Number of pins: "+playersPieces.Count);
		Random randomPiece = new Random();
		int rPiece = randomPiece.Next(playersPieces.Count);
		if (playersPieces.Count < rPiece || rPiece == 0)
		{
			//GD.Print($"Count: {playersPieces.Count} index: {rPiece}");
		}
		Vector2I pieceToMove = playersPieces[rPiece];
		return pieceToMove;
	}
	
	bool MakeRandomMove(Vector2I pieceToMove)
	{
		List<Vector2I> validMoves = AIGame.FindValidMoves(pieceToMove, currentPlayer);
		Random randomMove = new Random();
		if (validMoves.Count == 0)
		{
			//GD.Print("No Valid Moves for piece: ", pieceToMove);
			return false;
		}
		else
		{
			Vector2I moveToMake = validMoves[randomMove.Next(validMoves.Count)];
			AIGame.MoveCharacter(pieceToMove, moveToMake);
			return true;
		}
		//GD.Print("Move picked: " + moveToMake);
		//GD.Print("Move made");
	}
	
	bool PlayGame()
	{
		bool isGameOver = false;
		
		while (!isGameOver)
		{
			Vector2I piece = SelectRandomPiece();
			bool moveMade = MakeRandomMove(piece);
			Constants.Player checkWin = AIGame.FindWinner();
			gameState = "asdfghf";
			var gdNode = GetNode("Node");
			gdNode.Call("save_game_state", gameState);
			
			if (currentPlayer == checkWin || !moveMade)
			{
				isGameOver = true;
			}
			else {
				if (currentPlayer == Constants.Player.Hero)
				{
					currentPlayer = Constants.Player.Enemy;
				}
				else
				{
					currentPlayer = Constants.Player.Hero;
				}
			}
			if (AIGame.IsDraw(currentPlayer))
			{
				isGameOver = true;
			}
		}
		return isGameOver;
	}
}
