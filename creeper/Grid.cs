using Godot;
using System;
using System.Collections.Generic;
using System.Reflection;

public partial class Grid : Node2D
{
	[Signal] public delegate void CharacterClickEventHandler(Vector2I pos);
	[Signal] public delegate void CharacterMouseEnteredEventHandler(Vector2I pos);
	[Signal] public delegate void CharacterMouseExitedEventHandler(Vector2I pos);
	[Signal] public delegate void MoveFinishedEventHandler();

    private const int GRID_X_DISTANCE = 96;
    private const int GRID_Y_DISTANCE = 60;
    private const int GRID_Y_OFFSET = 50;
	private bool moving = false;
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
		if (!moving) EmitSignal(SignalName.CharacterClick, ConvertPixeltoGrid((Vector2I)character.Position));
	}

	void MouseEntered(CharacterBase character)
	{
	   if (!moving) EmitSignal(SignalName.CharacterMouseEntered, ConvertPixeltoGrid((Vector2I)character.Position));
	}

	void MouseExited(CharacterBase character)
	{
		if (!moving) EmitSignal(SignalName.CharacterMouseExited, ConvertPixeltoGrid((Vector2I)character.Position));
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

	public void RemoveGhosts()
	{
		foreach(var child in TileMap.GetChildren())
		{
			if (child is CharacterBase)
			{
				if (!moving) DeleteGhosts();
				else ((CharacterBase)child).Visible = false;
			}
		}  
	}

	private void DeleteGhosts()
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

    //moves the appropriate character as well deletes jumped characters
    public void MoveCharacter(Vector2I from, Vector2I to)
    {
        CharacterBase CharacterFrom = FindCharacteratGridPos(from);
        CharacterBase CharacterTo = FindCharacteratGridPos(to);

		CharacterFrom.StopHover();
		CharacterTo.StopHover();

		//ensure the character to move is valid and that the target space is either empty or contains a ghost.
        if (CharacterFrom == null || (CharacterTo != null && !CharacterTo.IsGhost)) return;

		moving = true;

		if (Math.Abs(from.X - to.X) == 2)
		{
			KillCharacterAndMove(CharacterFrom, FindCharacteratGridPos(new(from.X + (to.X - from.X)/2, from.Y)), CharacterTo);
		}
		else if (Math.Abs(from.Y - to.Y) == 2)
		{
			KillCharacterAndMove(CharacterFrom, FindCharacteratGridPos(new(from.X, from.Y + (to.Y - from.Y)/2)), CharacterTo);
		}
		else
		{
			AnimateMove(CharacterFrom, CharacterTo, 1);
		}
	}

	private void AnimateMove(CharacterBase from, CharacterBase to, double duration)
	{
		//find the direction of movement
		Vector2 direction = (to.Position - from.Position).Normalized().Snapped(1);
		if (direction == Vector2.Left)
		{
			from.Play("walk_left");
		}
		else if (direction == Vector2.Right)
		{
			from.Play("walk_right");
		}
		else if (direction == Vector2.Up)
		{
			from.Play("walk_up");
		}
		else if (direction == Vector2.Down)
		{
			from.Play("walk_down");
		}
		else if (direction.X == 1)
		{
			from.Play("walk_right");
			duration = Math.Sqrt(2);
		}
		else if (direction.X == -1)
		{
			from.Play("walk_left");
			duration = Math.Sqrt(2);
		}

		Tween tween = CreateTween();
		tween.TweenProperty(from, "position", to.Position, duration);
		tween.Finished += () => 
		{
			from.Play("idle");
			moving = false;
			EmitSignal(SignalName.MoveFinished);
		};

		DeleteGhosts();
	}

	public void KillCharacterAndMove(CharacterBase killer, CharacterBase toKill, CharacterBase moveTo)
	{
		if (toKill == null || killer == null) return;

		//find attack direction and start attack animation
		Vector2 direction = (toKill.Position - killer.Position).Normalized().Snapped(1);
		if (direction == Vector2.Left)
		{
			killer.Play("attack_left");
		}
		else if (direction == Vector2.Right)
		{
			killer.Play("attack_right");
		}
		else if (direction == Vector2.Up)
		{
			killer.Play("attack_up");
		}
		else if (direction == Vector2.Down)
		{
			killer.Play("attack_down");
		}

        //when attack animation is finished start killer's idle animation and toKill's die animation
        void killerAnimationFinished()
        {
			killer.Play("idle");

			toKill.Play("die");

			//when toKill's die animation is finished, begin fadeout
			toKill.AnimationFinished += () =>
			{
				Tween tween = CreateTween();
				tween.TweenProperty(toKill, "modulate:a", 0.0f, 1);

				//when toKill is done fading out, move killer and delete toKill
				tween.Finished += () =>
				{
					AnimateMove(killer, moveTo, 2);
					toKill.GetParent().RemoveChild(toKill);
					toKill.QueueFree();
					characters.Remove(toKill);
				};		
			};
        }

		//ensure the function connect to animation finished is one shot to avoid conflicts
		//the code within the connect method comes from gemini
        killer.Connect(
			AnimationMixer.SignalName.AnimationFinished, 
			Callable.From(killerAnimationFinished), 
			(uint)ConnectFlags.OneShot);
    }
    public void ChangeTile(Vector2I pos, Constants.Player player)
    {
		//converts from standard grid coordinates to isometric
        TileMap.SetCell(new(pos.Y + pos.X, pos.Y - pos.X), TileType[player], new(0,0));
    }

	private static Vector2I ConvertGridtoPixel(Vector2I pos)
	{
		return new Vector2I(pos.X * GRID_X_DISTANCE, (pos.Y * GRID_Y_DISTANCE) - GRID_Y_OFFSET);
	}

	private static Vector2I ConvertPixeltoGrid(Vector2I pos)
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
