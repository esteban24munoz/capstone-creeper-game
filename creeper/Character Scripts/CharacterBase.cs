using Godot;
using System;

public partial class CharacterBase : AnimatedSprite2D
{

	[Signal] public delegate void OnClickEventHandler(CharacterBase character);
	[Signal] public delegate void MouseEnteredEventHandler(CharacterBase character);
	[Signal] public delegate void MouseExitedEventHandler(CharacterBase character);

	private bool _isGhost = false;
	public bool IsGhost
	{
		get {return _isGhost;}
		set
		{
			if (value)
			{
			//Code to make the character transparent comes from Gemini
			Color GhostColor = Modulate;
			GhostColor.A = 0.6f;
			SelfModulate = GhostColor;
			_isGhost = value;
			}
			else
			{
			//Code to make the character transparent comes from Gemini
			Color GhostColor = Modulate;
			GhostColor.A = 1;
			SelfModulate = GhostColor;
			_isGhost = value;
			}
		}
	}

	Area2D area;
	private int HoverOffset = 10;
	public bool Hovering = false;
	private bool MouseOver = false;
	
	public override void _Ready()
	{
		Play("idle");
		area = GetNode<Area2D>("Area2D");
		area.MouseEntered += () => {EmitSignal(SignalName.MouseEntered, this); MouseOver = true;};
		area.MouseExited += () => {EmitSignal(SignalName.MouseExited, this); MouseOver = false;};
	}

	public void Hover()
	{
		if (Hovering) return;
		Position = new Vector2(Position.X, Position.Y - HoverOffset);
		area.Position = new Vector2(area.Position.X, area.Position.Y + HoverOffset);
		Hovering = true;
	}

	public void StopHover()
	{
		if (!Hovering) return;
		Position = new Vector2(Position.X, Position.Y + HoverOffset);
		area.Position = new Vector2(area.Position.X, area.Position.Y - HoverOffset);
		Hovering = false;
	}

	public override void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed("mouse_click") && MouseOver)
		{
			EmitSignal(SignalName.OnClick, this);
		}
	}

}
