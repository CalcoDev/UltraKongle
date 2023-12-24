using Godot;

namespace KongleJam.Components;

public partial class InterpolationComponent : Node
{
    [ExportGroup("Settings")]
    [Export] public bool UseGlobalTransform = true;
    [Export] public bool SyncPhysics = true;

    [ExportGroup("Follow")]
    [Export] public Node2D Follow;
    [Export] public bool FollowTopLevel = true;

    [ExportGroup("Follower")]
    [Export] public Node2D Follower;
    [Export] public bool FollowerTopLevel = false;

    public Vector2 StartPos = Vector2.Zero;
    public bool ResetPos = false;

    private Transform2D _prevTransform;
    private Transform2D _currTransform;

    public override void _Ready()
    {
        ProcessPriority = 100;

        if (Follower == null || Follow == null)
            return;

        Vector2 prevPos = Follow.GlobalPosition;
        if (FollowerTopLevel)
            Follower.TopLevel = true;

        if (FollowTopLevel)
            Follow.TopLevel = true;
        
        if (ResetPos)
            prevPos = StartPos;
        
        Follow.GlobalPosition = prevPos;
        Follower.GlobalPosition = prevPos;
    }

    public override void _Process(double delta)
    {
        if (Follower == null || Follow == null)
            return;

        float t = (float)Engine.GetPhysicsInterpolationFraction();

        Transform2D tr = Transform2D.Identity;
        tr.Origin = _prevTransform.Origin.Lerp(_currTransform.Origin, t);
        tr.X = _prevTransform.X.Lerp(_currTransform.X, t);
        tr.Y = _prevTransform.Y.Lerp(_currTransform.Y, t);

        Follower.GlobalTransform = tr;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (Follower == null || Follow == null)
            return;

        _prevTransform = _currTransform;
        _currTransform = UseGlobalTransform
            ? Follow.GlobalTransform
            : Follow.Transform;
    }
}