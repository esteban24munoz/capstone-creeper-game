using System;
using System.Collections.Generic;
using Godot;

public partial class LocalPlayer : IPlayer
{
    private Vector2I? SelectedCharacter = null;
    private Model ModelInstance;
    private Grid ViewInstance;
    //ready is set to true in SetupTurn, and back to false at the end of OnClick
    //it ensures that the object can't interact with the board when it's not its turn
    private bool ready = false;
    private Constants.Player Player;

    //declare event
    public event EventHandler<(Vector2I, Vector2I)> MoveFound;

    public void SetupTurn(Model model, Grid grid)
    {
        ModelInstance = model;
        ViewInstance = grid;
        ready = true;
    }

    public void SetPlayer(Constants.Player player)
    {
        Player = player;
    }

    //This is the main input loop
	//This method is called every time a character is clicked
    public void OnClick(Vector2I pos)
    {
        if (!ready) return;

        //If there is no selected character and the clicked character matches the active player,
		//make the clicked character selected and instantiate ghosts on the valid spaces
        if (SelectedCharacter == null)
		{
			if (ModelInstance.PlayerAt(pos) != Player) return;
			SelectCharacter(pos);
		}
		else
		{
			if (ModelInstance.PlayerAt(pos) == Player)
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
                //the object now knows what move it wants to make, so it emits MoveFound to tell the controller
                MoveFound?.Invoke(this, (SelectedCharacter.Value, pos));
				ViewInstance.RemoveGhosts();
				SelectedCharacter = null;

                ready = false;
            }
        }
    }

    //creates ghosts on valid spots
    private void SelectCharacter(Vector2I pos)
	{
		List<Vector2I> moves = ModelInstance.FindValidMoves(pos, Player);
		ViewInstance.CreateGhosts(pos, moves);

		SelectedCharacter = pos;
	}

    //starts character hover
    public void MouseEntered(Vector2I pos)
	{
		if (ready && (
            ModelInstance.PlayerAt(pos) == Player ||
            (SelectedCharacter != null && ViewInstance.IsGhost(pos))
        ))
		{
			ViewInstance.Hover(pos);
		}
	}

    //stops character hover
	public void MouseExited(Vector2I pos)
	{
	   if (ready && (
            ModelInstance.PlayerAt(pos) == Player ||
            (SelectedCharacter != null && ViewInstance.IsGhost(pos))
        ))
		{
			ViewInstance.StopHover(pos);
		}
	}
}