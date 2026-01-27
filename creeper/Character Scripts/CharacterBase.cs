using Godot;
using System;

public partial class CharacterBase : AnimatedSprite2D
{

    [Signal] public delegate void OnClickEventHandler(CharacterBase character);
    [Signal] public delegate void MouseEnteredEventHandler(CharacterBase character);
    [Signal] public delegate void MouseExitedEventHandler(CharacterBase character);

    Area2D area;
    private int HoverOffset = 10;
    private bool Hovering = false;
    
    public override void _Ready()
    {
        Play("idle");
        area = GetNode<Area2D>("Area2D");
        area.MouseEntered += () => EmitSignal(SignalName.MouseEntered, this);
        area.MouseExited += () => EmitSignal(SignalName.MouseExited, this);
    }

    public void Hover()
    {
        if (Hovering) return;
        Position = new Vector2(Position.X, Position.Y - HoverOffset);
        area.Position = new Vector2(area.Position.X, area.Position.Y + HoverOffset*2);
        Hovering = true;
    }

    public void StopHover()
    {
        if (!Hovering) return;
        Position = new Vector2(Position.X, Position.Y + HoverOffset);
        area.Position = new Vector2(area.Position.X, area.Position.Y - HoverOffset*2);
        Hovering = false;
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("mouse_click") && Hovering)
        {
            EmitSignal(SignalName.OnClick, this);
        }
    }

}
