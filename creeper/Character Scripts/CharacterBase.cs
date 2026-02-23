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
			Color GhostColor = Modulate;
			GhostColor.A = 1;
			SelfModulate = GhostColor;
			_isGhost = value;
			}
		}
	}

	Area2D area;
	public AudioStreamPlayer2D Hit, Fade;
	private int HoverOffset = 10;
	public bool Hovering = false;
	private bool MouseOver = false;
	
	public override void _Ready()
	{
		area = GetNode<Area2D>("Area2D");
		area.MouseEntered += () => {EmitSignal(SignalName.MouseEntered, this); MouseOver = true;};
		area.MouseExited += () => {EmitSignal(SignalName.MouseExited, this); MouseOver = false;};

		Hit = GetNode<AudioStreamPlayer2D>("Hit");
		Fade = GetNode<AudioStreamPlayer2D>("Fade");
	}

	public void Hover()
	{
		//move the entire sprite up then move the collision shape back down
		if (Hovering) return;
		Position = new Vector2(Position.X, Position.Y - HoverOffset);
		area.Position = new Vector2(area.Position.X, area.Position.Y + HoverOffset);
		Hovering = true;
	}

	public void StopHover()
	{
		//move the entire sprite down then move the collision shape back up
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
