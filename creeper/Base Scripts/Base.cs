using Godot;
using System;

public partial class Base : AnimatedSprite2D
{
	public override void _Ready()
	{
		Play("idle");
	}
}
