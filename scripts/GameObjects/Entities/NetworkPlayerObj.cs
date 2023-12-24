using Godot;
using KongleJam.Components;

namespace KongleJam.GameObjects.Entities;

public partial class NetworkPlayerObj : Node2D
{
    [ExportGroup("Network sync")]
    [Export] private NetworkSyncComponent _sync;

    public long NetworkId { get; set; } = -1;

    public override void _Process(double delta)
    {
        if (!_sync.HasValue(NetworkId, "player_position"))
            return;

        Vector2 position = _sync.GetValue(NetworkId, "player_position").As<Vector2>();
        GlobalPosition = position;       
    }
}