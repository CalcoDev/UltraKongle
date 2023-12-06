using System.Collections.Generic;
using System.Linq;
using Godot;

namespace KongleJam.Components;

public partial class VelocityComponent : Node
{
    [Export] private Vector2 _velocity;

    public Vector2 Value => _velocity;

    public float X => _velocity.X;
    public float Y => _velocity.Y;

    private readonly List<Vector2> _additional = new();

    public void AddAdditional(Vector2 vel)
    {
        _additional.Add(vel);
    }

    public void ClearAdditional()
    {
        _additional.Clear();
    }

    public Vector2 GetAdditional()
    {
        return _additional.Aggregate(Vector2.Zero, (current, add) => current + add);
    }

    public Vector2 Get()
    {
        return _velocity;
    }

    public Vector2 GetClear()
    {
        Vector2 vel = Get() + GetAdditional();
        ClearAdditional();
        return vel;
    }

    public void Approach(Vector2 target, float maxDelta)
    {
        ApproachX(target.X, maxDelta);
        ApproachY(target.Y, maxDelta);
    }

    public void ApproachX(float target, float maxDelta)
    {
        _velocity.X = Approach(_velocity.X, target, maxDelta);
    }

    public void ApproachY(float target, float maxDelta)
    {
        _velocity.Y = Approach(_velocity.Y, target, maxDelta);
    }

    private static float Approach(float current, float target, float maxDelta)
    {
        float difference = target - current;

        if (Mathf.Abs(difference) <= maxDelta)
            return target;

        return current + (difference > 0 ? maxDelta : -maxDelta);

        // return current < target ? Mathf.Min(current + maxDelta, target) : Mathf.Max(current - maxDelta, target);
    }

    public void SetX(float value)
    {
        _velocity.X = value;
    }

    public void SetY(float value)
    {
        _velocity.Y = value;
    }

    public void Set(float x, float y)
    {
        _velocity.X = x;
        _velocity.Y = y;
    }

    public void Set(Vector2 v)
    {
        _velocity = v;
    }

    public void AddX(float addon)
    {
        _velocity.X += addon;
    }

    public void AddY(float addon)
    {
        _velocity.Y += addon;
    }

    public void MultiplyX(float multiplier)
    {
        _velocity.X *= multiplier;
    }

    public void MultiplyY(float multiplier)
    {
        _velocity.Y *= multiplier;
    }
}