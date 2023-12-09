using Godot;
using Key = KongleJam.Utils.Key;

namespace KongleJam.GameObjects.Entities;

public partial class Player : Node2D
{
    [ExportGroup("References")]
    [Export] private RigidBody2D _rb;

    // [ExportGroup("Yeet")]
    // [Export] private float _maxYeetDist;
    // [Export] private float _maxYeetForce;

    private const float YeetMaxDist = 100;
    private const float YeetMaxDistShow = 25;
    private const float YeetMaxForce = 200;

    // Refs
    private Node2D _visuals;

    private Node2D _blob;
    private Node2D _arrow;

    // Input
    private Key _iYeet;

    // States
    private bool _isYeeting;

    // Yeeting
    private Vector2 _yeetDragStart;
    private Vector2 _yeetDragEnd;

    public override void _Ready()
    {
        _visuals = GetNode<Node2D>("Visuals");
        _blob = GetNode<Node2D>("Blob");
        _arrow = GetNode<Node2D>("Arrow");
    }

    public override void _Process(double delta)
    {
        _iYeet.Update("yeet");

        if (_iYeet.Pressed)
        {
            _isYeeting = true;
            _yeetDragStart = GetGlobalMousePosition();

            _blob.Visible = true;
            _arrow.Visible = true;
        }

        if (_isYeeting)
        {
            QueueRedraw();
            _yeetDragEnd = GetGlobalMousePosition();
            Vector2 offset = _yeetDragStart - _yeetDragEnd;

            float angle = -offset.AngleTo(Vector2.Up);
            _visuals.Rotation = angle;
            _arrow.Rotation = angle;

            float dist = offset.Length();
            float t = Mathf.Min(dist / YeetMaxDist, 1f);
            _blob.Rotation = angle;
            _blob.GetChild<Node2D>(0).Position = new Vector2(-2, 8 + t * YeetMaxDistShow);

            if (_iYeet.Released)
            {
                _isYeeting = false;
                _blob.Visible = false;
                _arrow.Visible = false;

                Vector2 dir = offset.Normalized();
                _rb.ApplyImpulse(dir * t * YeetMaxForce);
            }
        }
    }

    public override void _Draw()
    {
        if (_isYeeting)
        {
            DrawLine(_yeetDragStart - Position, _yeetDragEnd - Position, Colors.Aqua);
        }
    }
}