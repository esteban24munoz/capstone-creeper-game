using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;

public partial class AI : Node2D
{
	Model AIGame = new Model();
	Constants.Player currentPlayer = Constants.Player.Hero;
	
	public override void _Ready()
	{
		GD.Print("Starting test");
		
		for (int i = 0; i < 5; i++)
		{
			//Vector2I selectedPiece = SelectRandomPiece();
			//GD.Print($"selected piece is {selectedPiece.X}, {selectedPiece.Y}");
			//MakeRandomMove(selectedPiece);
			//GD.Print("Move made, run again");
			GD.Print("Game #" + i);
			bool isGameDone = PlayGame();
			GD.Print("Game is over: "+isGameDone);
			//Need to reset the game to a fresh game and save old game data for stragey
			AIGame = new Model();
			currentPlayer = Constants.Player.Hero; 
		}
	}
	
	Vector2I SelectRandomPiece()
	{
		List<Vector2I> playersPieces = new();
		//int playersPiecesIterator = 0;
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
					//playersPieces[playersPiecesIterator].X = r;
					//playersPieces[playersPiecesIterator].Y = c;
					playersPieces.Add(pos);
					//playersPiecesIterator++;
				}
			}
		}
		//GD.Print("Number of pins: "+playersPieces.Count);
		Random randomPiece = new Random();
		//int piecesArrayPoint = randomPiece(playersPiecesIterator);
		//Vector2I pieceToMove = playersPieces[piecesArrayPoint];
		Vector2I pieceToMove = playersPieces[randomPiece.Next(playersPieces.Count)];
		GD.Print("Pin selected: " + pieceToMove);
		return pieceToMove;
	}
	
	void MakeRandomMove(Vector2I pieceToMove)
	{
		List<Vector2I> validMoves = AIGame.FindValidMoves(pieceToMove, currentPlayer);
		Random randomMove = new Random();
		Vector2I moveToMake = validMoves[randomMove.Next(validMoves.Count)];
		GD.Print("Move picked: " + moveToMake);
		AIGame.MoveCharacter(pieceToMove, moveToMake);
		//GD.Print("Move made");
	}
	
	bool PlayGame()
	{
		bool isGameOver = false;
		
		while (!isGameOver)
		{
			Vector2I piece = SelectRandomPiece();
			MakeRandomMove(piece);
			Constants.Player checkWin = AIGame.FindWinner();
			
			if (currentPlayer == checkWin)
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
		}
		return isGameOver;
	}
}
