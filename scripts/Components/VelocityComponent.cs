using Godot;

namespace KongleJam.Components;

public partial class VelocityComponent : Node
{
    [Export] private Vector2 _velocity;

    public Vector2 Value => _velocity;

    public float X => _velocity.X;
    public float Y => _velocity.Y;

    public Vector2 GetVelocity()
    {
        return _velocity;
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
        return current < target ? Mathf.Min(current + maxDelta, target) : Mathf.Max(current - maxDelta, target);
    }

    public void SetVelocityX(float value)
    {
        _velocity.X = value;
    }

    public void SetVelocityY(float value)
    {
        _velocity.Y = value;
    }

    public void SetVelocity(float x, float y)
    {
        _velocity.X = x;
        _velocity.Y = y;
    }

    public void SetVelocity(Vector2 v)
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