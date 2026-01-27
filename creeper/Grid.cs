using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;

public partial class Grid : Node2D
{
    Constants.Player[,] PinholeGrid = {
        {Constants.Player.None, Constants.Player.Hero, Constants.Player.Hero, Constants.Player.None, Constants.Player.Enemy, Constants.Player.Enemy, Constants.Player.None},
        {Constants.Player.Hero, Constants.Player.None, Constants.Player.None, Constants.Player.None, Constants.Player.None, Constants.Player.None, Constants.Player.Enemy},
        {Constants.Player.Hero, Constants.Player.None, Constants.Player.None, Constants.Player.None, Constants.Player.None, Constants.Player.None, Constants.Player.Enemy},
        {Constants.Player.None, Constants.Player.None, Constants.Player.None, Constants.Player.None, Constants.Player.None, Constants.Player.None, Constants.Player.None},
        {Constants.Player.Enemy, Constants.Player.None, Constants.Player.None, Constants.Player.None, Constants.Player.None, Constants.Player.None, Constants.Player.Hero},
        {Constants.Player.Enemy, Constants.Player.None, Constants.Player.None, Constants.Player.None, Constants.Player.None, Constants.Player.None, Constants.Player.Hero},
        {Constants.Player.None, Constants.Player.Enemy, Constants.Player.Enemy, Constants.Player.None, Constants.Player.Hero, Constants.Player.Hero, Constants.Player.None},
    };
    Constants.Player ActivePlayer = Constants.Player.Hero;
    
    TileMapLayer t;
    Godot.Collections.Array<Node> characters;
    public override void _Ready()
    {
        t = GetNode<TileMapLayer>("TileMapLayer");
        characters = t.GetChild(0).GetChildren();
        foreach(CharacterBase c in characters)
        {
            c.OnClick += OnClick;
            c.MouseEntered += MouseEntered;
            c.MouseExited += MouseExited;
        }
    }

    void OnClick(CharacterBase character)
    {
        Vector2I gridPos = ConvertPixeltoGrid((Vector2I)character.Position);
        List<Vector2I> moves = FindValidMoves(gridPos);
        if (moves.Count > 0)
        {
            character.Position = ConvertGridtoPixel(moves[0]);
            PinholeGrid[gridPos.X, gridPos.Y] = Constants.Player.None;
            PinholeGrid[moves[0].X, moves[0].Y] = ActivePlayer;
            character.StopHover();
            NewTurn();
        }
    }

    void MouseEntered(CharacterBase character)
    {
        Vector2I gridpos = ConvertPixeltoGrid((Vector2I)character.Position);
        if (PinholeGrid[gridpos.X, gridpos.Y] == ActivePlayer)
        {
            character.Hover();
        }
    }

    void MouseExited(CharacterBase character)
    {
        Vector2I gridpos = ConvertPixeltoGrid((Vector2I)character.Position);
        if (PinholeGrid[gridpos.X, gridpos.Y] == ActivePlayer)
        {
            character.StopHover();
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

    List<Vector2I> FindValidMoves(Vector2I pos)
    {
        if(PinholeGrid[pos.X, pos.Y] != ActivePlayer)
        {
            return [];
        }
        List<Vector2I> ValidMoves = [];

        Constants.Player p;

        //Orthagonal movement
        if (pos.X > 0)
        {
            p = PinholeGrid[pos.X - 1, pos.Y];

            if (p == Constants.Player.None)
            {
                ValidMoves.Add(new Vector2I(pos.X - 1, pos.Y));
            }
            else if (p != ActivePlayer && pos.X > 1 && PinholeGrid[pos.X - 2, pos.Y] == Constants.Player.None)  
            {
                ValidMoves.Add(new Vector2I(pos.X - 2, pos.Y));
            }
        }

        if (pos.X < PinholeGrid.GetLength(0) - 1)
        {
            p = PinholeGrid[pos.X + 1, pos.Y];

            if (p == Constants.Player.None)
            {
                ValidMoves.Add(new Vector2I(pos.X + 1, pos.Y));
            }
            else if (p != ActivePlayer && pos.X < PinholeGrid.GetLength(0) - 2 && PinholeGrid[pos.X + 2, pos.Y] == Constants.Player.None)  
            {
                ValidMoves.Add(new Vector2I(pos.X + 2, pos.Y));
            }
        }

        if (pos.Y > 0)
        {
            p = PinholeGrid[pos.X, pos.Y - 1];

            if (p == Constants.Player.None)
            {
                ValidMoves.Add(new Vector2I(pos.X, pos.Y - 1));
            }
            else if (p != ActivePlayer && pos.Y > 1 && PinholeGrid[pos.X, pos.Y - 2] == Constants.Player.None)  
            {
                ValidMoves.Add(new Vector2I(pos.X, pos.Y - 2));
            }
        }

        if (pos.Y < PinholeGrid.GetLength(0) - 1)
        {
            p = PinholeGrid[pos.X, pos.Y + 1];

            if (p == Constants.Player.None)
            {
                ValidMoves.Add(new Vector2I(pos.X, pos.Y + 1));
            }
            else if (p != ActivePlayer && pos.Y < PinholeGrid.GetLength(0) - 2 && PinholeGrid[pos.X, pos.Y + 2] == Constants.Player.None)  
            {
                ValidMoves.Add(new Vector2I(pos.X, pos.Y + 2));
            }
        }

        //Diagonal movement
        if (pos.X > 0 && pos.Y > 0 && PinholeGrid[pos.X - 1, pos.Y - 1] == Constants.Player.None)
        {
            ValidMoves.Add(new Vector2I(pos.X - 1, pos.Y - 1));
        }

        if (pos.X > 0 && pos.Y < PinholeGrid.GetLength(0) - 1 && PinholeGrid[pos.X - 1, pos.Y + 1] == Constants.Player.None)
        {
            ValidMoves.Add(new Vector2I(pos.X - 1, pos.Y + 1));
        }

        if (pos.X < PinholeGrid.GetLength(0) - 1 && pos.Y > 0 && PinholeGrid[pos.X + 1, pos.Y - 1] == Constants.Player.None)
        {
            ValidMoves.Add(new Vector2I(pos.X + 1, pos.Y - 1));
        }

        if (pos.X < PinholeGrid.GetLength(0) - 1 && pos.Y < PinholeGrid.GetLength(0) - 1 && PinholeGrid[pos.X + 1, pos.Y + 1] == Constants.Player.None)
        {
            ValidMoves.Add(new Vector2I(pos.X + 1, pos.Y + 1));
        }

        ValidMoves.RemoveAll(x => x == new Vector2I(0, 0) || x == new Vector2I(0, PinholeGrid.GetLength(0) - 1) || x == new Vector2I(PinholeGrid.GetLength(0) - 1, PinholeGrid.GetLength(0) - 1) || x == new Vector2I(PinholeGrid.GetLength(0) - 1, 0));
        return ValidMoves;
    }

    Vector2I ConvertGridtoPixel(Vector2I pos)
    {
        return new Vector2I(pos.X * Constants.GRID_X_DISTANCE, (pos.Y * Constants.GRID_Y_DISTANCE) - Constants.GRID_Y_OFFSET);
    }

    Vector2I ConvertPixeltoGrid(Vector2I pos)
    {
        return new Vector2I(pos.X / Constants.GRID_X_DISTANCE, (pos.Y + Constants.GRID_Y_OFFSET) / Constants.GRID_Y_DISTANCE);
    }

    CharacterBase FindCharacteratGridPos(Vector2I pos)
    {
        foreach(CharacterBase c in characters)
        {
            if (ConvertPixeltoGrid((Vector2I)c.Position) == pos) return c;
        }
        return null;
    }
}
