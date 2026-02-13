using Godot;
using System;
using System.Collections.Generic;


public class Model
{
    Constants.Player[,] Grid {get;} = {
        {Constants.Player.None, Constants.Player.Enemy, Constants.Player.Enemy, Constants.Player.None, Constants.Player.Hero, Constants.Player.Hero, Constants.Player.None},
        {Constants.Player.Enemy, Constants.Player.None, Constants.Player.None, Constants.Player.None, Constants.Player.None, Constants.Player.None, Constants.Player.Hero},
        {Constants.Player.Enemy, Constants.Player.None, Constants.Player.None, Constants.Player.None, Constants.Player.None, Constants.Player.None, Constants.Player.Hero},
        {Constants.Player.None, Constants.Player.None, Constants.Player.None, Constants.Player.None, Constants.Player.None, Constants.Player.None, Constants.Player.None},
        {Constants.Player.Hero, Constants.Player.None, Constants.Player.None, Constants.Player.None, Constants.Player.None, Constants.Player.None, Constants.Player.Enemy},
        {Constants.Player.Hero, Constants.Player.None, Constants.Player.None, Constants.Player.None, Constants.Player.None, Constants.Player.None, Constants.Player.Enemy},
        {Constants.Player.None, Constants.Player.Hero, Constants.Player.Hero, Constants.Player.None, Constants.Player.Enemy, Constants.Player.Enemy, Constants.Player.None},
    };

    Constants.Player[,] Tiles {get;} =
    {
        {Constants.Player.Enemy, Constants.Player.None, Constants.Player.None, Constants.Player.None, Constants.Player.None, Constants.Player.Hero},
        {Constants.Player.None, Constants.Player.None, Constants.Player.None, Constants.Player.None, Constants.Player.None, Constants.Player.None},
        {Constants.Player.None, Constants.Player.None, Constants.Player.None, Constants.Player.None, Constants.Player.None, Constants.Player.None},
        {Constants.Player.None, Constants.Player.None, Constants.Player.None, Constants.Player.None, Constants.Player.None, Constants.Player.None},
        {Constants.Player.None, Constants.Player.None, Constants.Player.None, Constants.Player.None, Constants.Player.None, Constants.Player.None},
        {Constants.Player.Hero, Constants.Player.None, Constants.Player.None, Constants.Player.None, Constants.Player.None, Constants.Player.Enemy},
    };

    //moves the appropriate character as well deletes jumped characters and updates jumped hexes
    public void MoveCharacter(Vector2I from, Vector2I to)
    {
        Constants.Player player = Grid[from.X, from.Y];
        Grid[from.X, from.Y] = Constants.Player.None;
        Grid[to.X, to.Y] = player;

        Vector2I? jumped = FindJumpedCharacter(from, to);
        if (jumped != null)
        {
            Grid[jumped.Value.X, jumped.Value.Y] = Constants.Player.None;
        }

        jumped = FindJumpedHex(from, to);
        if (jumped != null)
        {
            Tiles[jumped.Value.X, jumped.Value.Y] = player;
        }
    }

    public Constants.Player PlayerAt(Vector2I pos)
    {
        if (pos.X < 0 || pos.X >= Grid.Length || pos.Y < 0 || pos.Y >= Grid.Length)
            return Constants.Player.None;
        else
            return Grid[pos.X, pos.Y];
    }

    //find the position of every character of the given player
    public List<Vector2I> GetAllCharacters(Constants.Player player)
    {
        List<Vector2I> players = [];
        for (int i  = 0; i < Grid.GetLength(0); i++)
        {
            for (int j = 0; j < Grid.GetLength(0); j++)
            {
                if (Grid[i,j] == player) players.Add(new(i,j));
            }
        }
        return players;
    }

    public static Vector2I? FindJumpedCharacter(Vector2I from, Vector2I to)
    {
        if (Math.Abs(from.X - to.X) == 2)
        {
            return new(from.X + (to.X - from.X)/2, from.Y);
        }

        if (Math.Abs(from.Y - to.Y) == 2)
        {
            return new(from.X, from.Y + (to.Y - from.Y)/2);
        }

        return null;
    }

    public Vector2I? FindJumpedHex(Vector2I from, Vector2I to)
    {
        if (from.X == to.X || from.Y == to.Y) return null;

        Vector2I higher = from.Y < to.Y ? from : to;
        Vector2I lower = higher == from ? to : from;

        Vector2I hex;
        if (higher.X < lower.X)
        {
            hex = higher;
        }
        else
        {
            hex = new(higher.X - 1, higher.Y);
        }


        //return null if jumped hex is one of the four corners
        if (hex == new Vector2I(0,0) || hex == new Vector2I(0,Tiles.GetLength(0) - 1) || hex == new Vector2I(Tiles.GetLength(0) - 1,0) || hex == new Vector2I(Tiles.GetLength(0) - 1, Tiles.GetLength(0) - 1))
        {
            return null;
        }
        else return hex;
    }

    public bool IsDraw(Constants.Player activePlayer)
    {
        for (int i = 0; i < Grid.GetLength(0); i++)
        {
            for (int j = 0; j < Grid.GetLength(0); j++)
            {
                if (Grid[i,j] == activePlayer && FindValidMoves(new(i,j), activePlayer).Count != 0)
                    return false;          
            }
        }
        return true;
    }

    //idea to use hash set instead of list from Gemini
    private readonly HashSet<Vector2I> visited = [];
    public Constants.Player FindWinner()
    {
        visited.Clear();
        if(FindWinner(new(0,0), Constants.Player.Enemy)) return Constants.Player.Enemy;
        visited.Clear();
        if(FindWinner(new(0,Tiles.GetLength(0) - 1), Constants.Player.Hero)) return Constants.Player.Hero;
        return Constants.Player.None;
    }

    //Depth First Search to find if a line has been completed
    private bool FindWinner(Vector2I tile, Constants.Player player)
    {
        //if we found the target tile return true
        if (player == Constants.Player.Enemy && tile == new Vector2I(Tiles.GetLength(0) - 1, Tiles.GetLength(0) - 1) ||
        player == Constants.Player.Hero && tile == new Vector2I(Tiles.GetLength(0) - 1, 0))
        {
            return true;
        }

        if (Tiles[tile.X, tile.Y] != player) return false;

        if (visited.Contains(tile)) return false;

        visited.Add(tile);

        bool done = false;

        //search all 4 directions. If no direction finds the target, return false
        if (tile.X < Tiles.GetLength(0) - 1)
            done = FindWinner(new(tile.X + 1, tile.Y), player);
        if (done)
        {
            return done;
        }

        if (tile.Y < Tiles.GetLength(0) -1)
            done = FindWinner(new(tile.X, tile.Y + 1), player);
        if (done)
        {
            return done;
        }

        if (tile.X > 0)
            done = FindWinner(new(tile.X - 1, tile.Y), player);
        if (done)
        {
            return done;
        }

        if (tile.Y > 0)
            done = FindWinner(new(tile.X, tile.Y - 1), player);
        if (done)
        {
            return done;
        }

        return false;
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

        //Delete the 4 corners since they are never valid moves
        ValidMoves.RemoveAll(x => x == new Vector2I(0, 0) || x == new Vector2I(0, Grid.GetLength(0) - 1) || x == new Vector2I(Grid.GetLength(0) - 1, Grid.GetLength(0) - 1) || x == new Vector2I(Grid.GetLength(0) - 1, 0));
        return ValidMoves;
    }

}