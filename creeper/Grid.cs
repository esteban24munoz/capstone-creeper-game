using Godot;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
	AudioStreamPlayer2D Fire, Grass;
	Godot.Collections.Array<Node> characters;
	Godot.Collections.Array<Node> ghosts;
	public override void _Ready()
	{
		TileMap = GetNode<TileMapLayer>("TileMapLayer");
		characters = TileMap.GetNode<Node2D>("Characters").GetChildren();
		foreach(CharacterBase c in characters)
		{
			c.OnClick += OnClick;
			c.MouseEntered += MouseEntered;
			c.MouseExited += MouseExited;
		}

		ghosts = TileMap.GetNode<Node2D>("Ghosts").GetChildren();
		foreach(AnimatedSprite2D ghost in ghosts)
		{
			var modulate = ghost.Modulate;
			modulate.A = 0.0f;
			ghost.Modulate = modulate;
		}
		
		Fire = GetNode<AudioStreamPlayer2D>("Fire");
		Grass = GetNode<AudioStreamPlayer2D>("Grass");
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

		//at each position, duplicate the character and make the duplicate a ghost
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

			Ghost.Stop();
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

	private List<CharacterBase> highlighted = [];

	public void HighlightKillable(List<Vector2I> positions)
	{
		highlighted.Clear();
		foreach (var pos in positions)
		{
			var character = FindCharacteratGridPos(pos);
			if (character != null)
			{
				character.SelfModulate = new(1f, 0, 0);
				highlighted.Add(character);
			}
		}
	}

	public void UnHighLightKillable()
	{
		foreach (var c in highlighted)
		{
			c.SelfModulate = new(1, 1, 1);
		}
		highlighted.Clear();
	}

	//moves the appropriate character as well deletes jumped characters
	public void MoveCharacter(Vector2I from, Vector2I to)
	{
		CharacterBase CharacterFrom = FindCharacteratGridPos(from);
		CharacterBase CharacterTo = FindCharacteratGridPos(to);

		CharacterFrom.StopHover();
		if (CharacterTo != null)
			CharacterTo.StopHover();
		
		if (CharacterTo == null)
		{
			CreateGhosts(from, new List<Vector2I>([to]));
			CharacterTo = FindCharacteratGridPos(to);
			CharacterTo.Visible = false;
		}

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

			ToggleCharacterAnimations();
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
		killer.Hit.Play();

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
				toKill.Fade.Play();

				//when toKill is done fading out: move killer, delete toKill, and make appropriate ghost visible
				tween.Finished += () =>
				{
					AnimateMove(killer, moveTo, 2);
					toKill.GetParent().RemoveChild(toKill);
					toKill.QueueFree();
					characters.Remove(toKill);

					foreach(AnimatedSprite2D ghost in ghosts)
					{
						if (ghost.Name == toKill.Name + "Ghost")
						{
							var targetPos = ghost.GlobalPosition;
							ghost.Position = toKill.Position;
							Tween tween = CreateTween();
							tween.TweenProperty(ghost, "modulate:a", 1, 0.5);
							tween.TweenProperty(ghost, "global_position", targetPos, 1).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.InOut);
							break;
						}
					}
				};		
			};
		}

		//ensure the function connect to animation finished is one shot to avoid conflicts
		//the code within the connect method comes from gemini
		killer.Connect(
			AnimationMixer.SignalName.AnimationFinished, 
			Callable.From(killerAnimationFinished), 
			(uint)ConnectFlags.OneShot
		);
	}

	public void StopCharacterAnimations(List<Vector2I> positions)
	{
		foreach (Vector2I character in positions)
		{
			FindCharacteratGridPos(character)?.Stop();
		} 
	}

	public void StartCharacterAnimations(List<Vector2I> positions)
	{
		foreach (Vector2I character in positions)
		{
			FindCharacteratGridPos(character)?.Play("idle");
		} 
	}

	public void ToggleCharacterAnimations()
	{
		foreach (CharacterBase character in characters.Cast<CharacterBase>())
		{
			if (character.IsPlaying()) character.Stop();
			else character.Play();
		}
	}
	
	public void StopAllCharacterAnimations()
	{
		foreach (CharacterBase character in characters.Cast<CharacterBase>())
		{
			if (character.IsPlaying()) character.Stop();
		}
	}

	public void ChangeTile(Vector2I pos, Constants.Player player)
	{
		//converts from standard grid coordinates to isometric
		TileMap.SetCell(new(pos.Y + pos.X, pos.Y - pos.X), TileType[player], new(0,0));

		switch (player)
		{
			case Constants.Player.Enemy:
				Fire.Play();
				break;
			case Constants.Player.Hero:
				Grass.Play();
				break;
		}
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
