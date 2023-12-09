using Godot;

namespace KongleJam.Components;

public partial class InterpolationComponent : Node
{
    [ExportCategory("Settings")]
    [Export] public bool UseGlobalTransform = true;
    // [Export] public bool UpdatePosition = true;
    // [Export] public bool UpdateRotation = true;
    [Export] public bool SyncPhysics = true;

    [ExportCategory("Follow")]
    [Export] public Node2D Follow { get; set; }
    [Export] public bool FollowTopLevel = true;

    [ExportCategory("Follower")]
    [Export] public Node2D Follower { get; set; }
    [Export] public bool FollowerTopLevel = false;

    private Transform2D _prevTransform;
    private Transform2D _currTransform;

    public override void _Ready()
    {
        ProcessPriority = 100;

        if (FollowerTopLevel)
            Follower.TopLevel = true;

        if (FollowTopLevel)
            Follow.TopLevel = true;
    }

    public override void _Process(double delta)
    {
        float t = (float)Engine.GetPhysicsInterpolationFraction();

        Transform2D tr = Transform2D.Identity;
        tr.Origin = _prevTransform.Origin.Lerp(_currTransform.Origin, t);
        tr.X = _prevTransform.X.Lerp(_currTransform.X, t);
        tr.Y = _prevTransform.Y.Lerp(_currTransform.Y, t);

        Follower.GlobalTransform = tr;
    }

    public override void _PhysicsProcess(double delta)
    {
        _prevTransform = _currTransform;
        _currTransform = UseGlobalTransform
            ? Follow.GlobalTransform
            : Follow.Transform;
    }
}