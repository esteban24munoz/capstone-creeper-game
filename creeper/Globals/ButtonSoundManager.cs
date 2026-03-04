using Godot;
using System;
using System.Collections.Generic;

public partial class ButtonSoundManager : AudioStreamPlayer2D
{
    private List<Button> buttons = [];
    public override void _Ready()
    {
        GetTree().TreeChanged += () => {GetButtons(GetTree().Root);};
    }

    public void GetButtons(Node parent)
    {
        if (parent.GetClass() == "Button") {
            Button button = (Button)parent;
            if (!buttons.Contains(button))
            {
                button.Pressed += PlaySound;
                buttons.Add(button);
            }
        }

        foreach (var child in parent.GetChildren())
        {
            GetButtons(child);
        }
    }

    public void PlaySound()
    {
        Play();
    }
}
