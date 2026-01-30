using Godot;
using System;
using System.Collections.Generic;


public class Model
{
    Constants.Player[,] Grid {get;} = {
        {Constants.Player.None, Constants.Player.Hero, Constants.Player.Hero, Constants.Player.None, Constants.Player.Enemy, Constants.Player.Enemy, Constants.Player.None},
        {Constants.Player.Hero, Constants.Player.None, Constants.Player.None, Constants.Player.None, Constants.Player.None, Constants.Player.None, Constants.Player.Enemy},
        {Constants.Player.Hero, Constants.Player.None, Constants.Player.None, Constants.Player.None, Constants.Player.None, Constants.Player.None, Constants.Player.Enemy},
        {Constants.Player.None, Constants.Player.None, Constants.Player.None, Constants.Player.None, Constants.Player.None, Constants.Player.None, Constants.Player.None},
        {Constants.Player.Enemy, Constants.Player.None, Constants.Player.None, Constants.Player.None, Constants.Player.None, Constants.Player.None, Constants.Player.Hero},
        {Constants.Player.Enemy, Constants.Player.None, Constants.Player.None, Constants.Player.None, Constants.Player.None, Constants.Player.None, Constants.Player.Hero},
        {Constants.Player.None, Constants.Player.Enemy, Constants.Player.Enemy, Constants.Player.None, Constants.Player.Hero, Constants.Player.Hero, Constants.Player.None},
    };

    public void MoveCharacter(Vector2I from, Vector2I to)
    {
        Constants.Player player = Grid[from.X, from.Y];
        Grid[from.X, from.Y] = Constants.Player.None;
        Grid[to.X, to.Y] = player;

        if (Math.Abs(from.X - to.X) == 2)
        {
            Grid[from.X + (to.X - from.X)/2, from.Y] = Constants.Player.None;
        }

        if (Math.Abs(from.Y - to.Y) == 2)
        {
            Grid[from.X, from.Y + (to.Y - from.Y)/2] = Constants.Player.None;
        }
    }

    public Constants.Player PlayerAt(Vector2I pos)
    {
        if (pos.X < 0 || pos.X >= Grid.Length || pos.Y < 0 || pos.Y >= Grid.Length)
            return Constants.Player.None;
        else
            return Grid[pos.X, pos.Y];
    }

    public List<Vector2I> FindValidMoves(Vector2I pos, Constants.Player activePlayer)
    {
        if(Grid[pos.X, pos.Y] != activePlayer)
        {
            return [];
        }
        List<Vector2I> ValidMoves = [];

        Constants.Player p;

        //Orthagonal movement
        if (pos.X > 0)
        {
            p = Grid[pos.X - 1, pos.Y];

            if (p == Constants.Player.None)
            {
                ValidMoves.Add(new Vector2I(pos.X - 1, pos.Y));
            }
            else if (p != activePlayer && pos.X > 1 && Grid[pos.X - 2, pos.Y] == Constants.Player.None)  
            {
                ValidMoves.Add(new Vector2I(pos.X - 2, pos.Y));
            }
        }

        if (pos.X < Grid.GetLength(0) - 1)
        {
            p = Grid[pos.X + 1, pos.Y];

            if (p == Constants.Player.None)
            {
                ValidMoves.Add(new Vector2I(pos.X + 1, pos.Y));
            }
            else if (p != activePlayer && pos.X < Grid.GetLength(0) - 2 && Grid[pos.X + 2, pos.Y] == Constants.Player.None)  
            {
                ValidMoves.Add(new Vector2I(pos.X + 2, pos.Y));
            }
        }

        if (pos.Y > 0)
        {
            p = Grid[pos.X, pos.Y - 1];

            if (p == Constants.Player.None)
            {
                ValidMoves.Add(new Vector2I(pos.X, pos.Y - 1));
            }
            else if (p != activePlayer && pos.Y > 1 && Grid[pos.X, pos.Y - 2] == Constants.Player.None)  
            {
                ValidMoves.Add(new Vector2I(pos.X, pos.Y - 2));
            }
        }

        if (pos.Y < Grid.GetLength(0) - 1)
        {
            p = Grid[pos.X, pos.Y + 1];

            if (p == Constants.Player.None)
            {
                ValidMoves.Add(new Vector2I(pos.X, pos.Y + 1));
            }
            else if (p != activePlayer && pos.Y < Grid.GetLength(0) - 2 && Grid[pos.X, pos.Y + 2] == Constants.Player.None)  
            {
                ValidMoves.Add(new Vector2I(pos.X, pos.Y + 2));
            }
        }

        //Diagonal movement
        if (pos.X > 0 && pos.Y > 0 && Grid[pos.X - 1, pos.Y - 1] == Constants.Player.None)
        {
            ValidMoves.Add(new Vector2I(pos.X - 1, pos.Y - 1));
        }

        if (pos.X > 0 && pos.Y < Grid.GetLength(0) - 1 && Grid[pos.X - 1, pos.Y + 1] == Constants.Player.None)
        {
            ValidMoves.Add(new Vector2I(pos.X - 1, pos.Y + 1));
        }

        if (pos.X < Grid.GetLength(0) - 1 && pos.Y > 0 && Grid[pos.X + 1, pos.Y - 1] == Constants.Player.None)
        {
            ValidMoves.Add(new Vector2I(pos.X + 1, pos.Y - 1));
        }

        if (pos.X < Grid.GetLength(0) - 1 && pos.Y < Grid.GetLength(0) - 1 && Grid[pos.X + 1, pos.Y + 1] == Constants.Player.None)
        {
            ValidMoves.Add(new Vector2I(pos.X + 1, pos.Y + 1));
        }

        ValidMoves.RemoveAll(x => x == new Vector2I(0, 0) || x == new Vector2I(0, Grid.GetLength(0) - 1) || x == new Vector2I(Grid.GetLength(0) - 1, Grid.GetLength(0) - 1) || x == new Vector2I(Grid.GetLength(0) - 1, 0));
        return ValidMoves;
    }

}