using System;
using System.Collections.Generic;
using Godot;

namespace KongleJam.GameObjects.Physics;

public partial class Actor : CharacterBody2D
{
    protected List<GodotObject> _collidingBodies;

    public virtual void Squish(KinematicCollision2D coll) { }

    public void MoveX(float amount, Action<KinematicCollision2D> onCollide = null)
    {
        var collX = MoveAndCollide(Vector2.Right * amount, safeMargin: 0.01f);
        if (collX != null)
            onCollide?.Invoke(collX);
    }

    public void MoveY(float amount, Action<KinematicCollision2D> onCollide = null)
    {
        var collY = MoveAndCollide(Vector2.Down * amount, safeMargin: 0.01f);
        if (collY != null)
            onCollide?.Invoke(collY);
    }
}