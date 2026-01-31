using Godot;
using System;
using System.Collections.Generic;

public partial class Controller : Node2D
{
    Constants.Player ActivePlayer = Constants.Player.Hero;
    Vector2I? SelectedCharacter = null;
    Grid ViewInstance;
    readonly Model ModelInstance = new();

    public override void _Ready()
    {
        ViewInstance = GetNode<Grid>("Grid");
        ViewInstance.CharacterClick += OnClick;
        ViewInstance.CharacterMouseEntered += MouseEntered;
        ViewInstance.CharacterMouseExited += MouseExited;
    }

    void OnClick(Vector2I pos)
    {
        if (SelectedCharacter == null)
        {
            if (ModelInstance.PlayerAt(pos) != ActivePlayer) return;

            List<Vector2I> moves = ModelInstance.FindValidMoves(pos, ActivePlayer);
            ViewInstance.CreateGhosts(pos, moves);

            SelectedCharacter = pos;
        }
        else
        {
            if (pos == SelectedCharacter)
            {
                ViewInstance.DeleteGhosts();
                SelectedCharacter = null;
            }
            else if (ViewInstance.IsGhost(pos))
            {
                ModelInstance.MoveCharacter((Vector2I)SelectedCharacter, pos);
                ViewInstance.MoveCharacter((Vector2I)SelectedCharacter, pos);
                ViewInstance.DeleteGhosts();
                SelectedCharacter = null;
                NewTurn();
            }
        }
    }

    void MouseEntered(Vector2I pos)
    {
        if ((SelectedCharacter == null && ModelInstance.PlayerAt(pos) == ActivePlayer) || 
        (SelectedCharacter != null && ViewInstance.IsGhost(pos)))
        {
            ViewInstance.Hover(pos);
        }
    }

    void MouseExited(Vector2I pos)
    {
       if ((SelectedCharacter == null && ModelInstance.PlayerAt(pos) == ActivePlayer) || 
        (SelectedCharacter != null && ViewInstance.IsGhost(pos)))
        {
            ViewInstance.StopHover(pos);
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