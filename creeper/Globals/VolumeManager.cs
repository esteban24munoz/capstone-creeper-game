using Godot;
using System;
using System.Collections.Generic;

public partial class VolumeManager : Node
{
    private List<AudioStreamPlayer2D> music = [];
    private List<AudioStreamPlayer2D> sfx = [];
    private Slider MusicSlider;
    private Slider SFXSlider;

    private static float _musicVolume = 1;
     private static float _sfxVolume = 1;

    public override void _Ready()
    {
        GetTree().TreeChanged += () => {
            music.Clear();
            sfx.Clear();
            Setup(GetTree().Root);
        };
    }

    public void Setup(Node parent)
    {
        if (parent.IsInGroup("MusicSlider")) {
            MusicSlider = (Slider)parent;
            MusicSlider.Value = _musicVolume;
            Callable callable = new(this, nameof(UpdateMusic));
            if (!MusicSlider.IsConnected(Slider.SignalName.ValueChanged, callable))
            {
                MusicSlider.Connect(Slider.SignalName.ValueChanged, callable);
            }
        }
        else if (parent.IsInGroup("SFXSlider"))
        {
            SFXSlider = (Slider)parent;
            SFXSlider.Value = _sfxVolume;
            Callable callable = new(this, nameof(UpdateSFX));
            if (!SFXSlider.IsConnected(Slider.SignalName.ValueChanged, callable))
            {
                SFXSlider.Connect(Slider.SignalName.ValueChanged, callable);
            }
        }
        else if (parent.IsInGroup("Music")) {
            AudioStreamPlayer2D musicPlayer = (AudioStreamPlayer2D)parent;
            if (!music.Contains(musicPlayer))
            {
                music.Add(musicPlayer);
                musicPlayer.VolumeLinear = _musicVolume;
            }
        }
        else if (parent.IsInGroup("SFX")) {
            AudioStreamPlayer2D sfxPlayer = (AudioStreamPlayer2D)parent;
            if (!sfx.Contains(sfxPlayer))
            {
                sfx.Add(sfxPlayer);
                sfxPlayer.VolumeLinear = _sfxVolume;
            }
        }

        foreach (var child in parent.GetChildren())
        {
            Setup(child);
        }
    }

    private void UpdateMusic(double value) 
    {
        foreach(var player in music)
        {
            player.VolumeLinear = (float)value;
            _musicVolume = (float)value;
        }
    }
    private void UpdateSFX(double value) 
    {
        sfx.Clear();
        Setup(GetTree().Root);
        foreach(var player in sfx)
        {
            player.VolumeLinear = (float)value;
            _sfxVolume = (float)value;
        }
    }
}

