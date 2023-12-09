using Godot;

namespace KongleJam.Resources;

[GlobalClass]
public partial class Dialogue : Resource
{
    [Export] public Speaker Speaker;
    [Export(PropertyHint.MultilineText)] public string Message = "";

    [Export] public bool AutoPlay = false;
    [Export] public float PlaybackSpeedMult = 1f;

    [Export] public Dialogue Next;
}