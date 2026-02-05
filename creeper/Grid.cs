using Godot;
using System;
using System.Collections.Generic;

public partial class Grid : Node2D
{
	[Signal] public delegate void CharacterClickEventHandler(Vector2I pos);
	[Signal] public delegate void CharacterMouseEnteredEventHandler(Vector2I pos);
	[Signal] public delegate void CharacterMouseExitedEventHandler(Vector2I pos);

	private const int GRID_X_DISTANCE = 96;
	private const int GRID_Y_DISTANCE = 60;
	private const int GRID_Y_OFFSET = 50;
	private readonly Dictionary<Constants.Player, int> TileType = new()
	{
		{Constants.Player.None, 4},
		{Constants.Player.Hero, 6},
		{Constants.Player.Enemy, 7}
	};


	TileMapLayer TileMap;
	Godot.Collections.Array<Node> characters;
	public override void _Ready()
	{
		TileMap = GetNode<TileMapLayer>("TileMapLayer");
		characters = TileMap.GetChild(0).GetChildren();
		foreach(CharacterBase c in characters)
		{
			c.OnClick += OnClick;
			c.MouseEntered += MouseEntered;
			c.MouseExited += MouseExited;
		}
	}

	void OnClick(CharacterBase character)
	{
		EmitSignal(SignalName.CharacterClick, ConvertPixeltoGrid((Vector2I)character.Position));
	}

	void MouseEntered(CharacterBase character)
	{
	   EmitSignal(SignalName.CharacterMouseEntered, ConvertPixeltoGrid((Vector2I)character.Position));
	}

	void MouseExited(CharacterBase character)
	{
		EmitSignal(SignalName.CharacterMouseExited, ConvertPixeltoGrid((Vector2I)character.Position));
	}

	public void Hover(Vector2I pos)
	{
		FindCharacteratGridPos(pos).Hover();
	}

	public void StopHover(Vector2I pos)
	{
		FindCharacteratGridPos(pos).StopHover();
	}


	public void CreateGhosts(Vector2I characterPos, List<Vector2I> positions)
	{
		CharacterBase character = FindCharacteratGridPos(characterPos);

		if (character == null) return;

		foreach (var pos in positions)
		{
			CharacterBase Ghost = (CharacterBase)character.Duplicate();
			TileMap.AddChild(Ghost);

			var NewPos = ConvertGridtoPixel(pos);
			NewPos.Y += 10;
			Ghost.Position = NewPos;

			Ghost.OnClick += OnClick;
			Ghost.MouseEntered += MouseEntered;
			Ghost.MouseExited += MouseExited;
			Ghost.IsGhost = true;

			characters.Add(Ghost);
		}
	}

	public void DeleteGhosts()
	{
		foreach(var child in TileMap.GetChildren())
		{
			if (child is CharacterBase)
			{
				characters.Remove(child);
				TileMap.RemoveChild(child);
				child.QueueFree();
			}
		}  
	}

	//moves the appropriate character as well deletes jumped characters and updates jumped hexes
	public void MoveCharacter(Vector2I from, Vector2I to)
	{
		CharacterBase CharacterFrom = FindCharacteratGridPos(from);
		CharacterBase CharacterTo = FindCharacteratGridPos(to);

		CharacterFrom.StopHover();
		CharacterTo.StopHover();

		//ensure the character to move is valid and that the target space is either empty or contains a ghost.
		if (CharacterFrom == null || (CharacterTo != null && !CharacterTo.IsGhost)) return;

		CharacterFrom.Position = CharacterTo.Position;

		if (Math.Abs(from.X - to.X) == 2)
		{
			DeleteCharacter(new(from.X + (to.X - from.X)/2, from.Y));
		}

		if (Math.Abs(from.Y - to.Y) == 2)
		{
			DeleteCharacter(new(from.X, from.Y + (to.Y - from.Y)/2));
		}
	}
	public void DeleteCharacter(Vector2I pos)
	{
		CharacterBase ToDelete = FindCharacteratGridPos(pos);

		if (ToDelete == null) return;

		ToDelete.GetParent().RemoveChild(ToDelete);
		ToDelete.QueueFree();
		characters.Remove(ToDelete);
	}
	public void ChangeTile(Vector2I pos, Constants.Player player)
	{
		//converts from standard grid coordinates to isometric
		TileMap.SetCell(new(pos.Y + pos.X, pos.Y - pos.X), TileType[player], new(0,0));
	}

	private Vector2I ConvertGridtoPixel(Vector2I pos)
	{
		return new Vector2I(pos.X * GRID_X_DISTANCE, (pos.Y * GRID_Y_DISTANCE) - GRID_Y_OFFSET);
	}

	private Vector2I ConvertPixeltoGrid(Vector2I pos)
	{
		return new Vector2I(pos.X / GRID_X_DISTANCE, (pos.Y + GRID_Y_OFFSET) / GRID_Y_DISTANCE);
	}

	private CharacterBase FindCharacteratGridPos(Vector2I pos)
	{
		foreach(CharacterBase c in characters)
		{
			if (ConvertPixeltoGrid((Vector2I)c.Position) == pos) return c;
		}
		return null;
	}

	public bool IsGhost(Vector2I pos)
	{
		return FindCharacteratGridPos(pos).IsGhost;
	} 
}
