using Godot;

namespace KongleJam.Resources;

[GlobalClass]
public partial class Speaker : Resource
{
    [Export] public string Name;
    [Export] public Texture2D Texture;
}