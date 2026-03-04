using System;
using Godot;

public interface IPlayer
{
    //emit this event when a move is ready to be evaluated by the controller
    //holds a single argument which is a duple of two vector2i's
	public event EventHandler<(Vector2I, Vector2I)> MoveFound;
    //passes in the model and grid to let the object know the current board state
    void SetupTurn(Model model, Grid grid);
    //tells the object whether it is a hero or villain
    void SetPlayer(Constants.Player player);

    //these three functions should only be used by the LocalPlayer object
    void OnClick(Vector2I pos);
    void MouseEntered(Vector2I pos);
    void MouseExited(Vector2I pos);
}