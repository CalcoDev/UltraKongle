
using Godot;

namespace KongleJam.Resources;

[GlobalClass]
public partial class Character : Resource
{
    [Export] public string Name;
    [Export(PropertyHint.MultilineText)] public string Description;
    [Export] public Texture2D Texture;

    [Export] public PackedScene LocalPrefab;
    [Export] public PackedScene ServerPrefab;
}