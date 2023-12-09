using Godot;
using KongleJam.Managers;
using KongleJam.Resources;
using Key = KongleJam.Utils.Key;

namespace KongleJam.GameObjects.Entities;

public partial class Player : Node2D
{
    [Export(PropertyHint.ResourceType, "Dialogue")] private Dialogue DIALOGUE;

    [ExportGroup("References")]
    [Export] private RigidBody2D _rb;

    // [ExportGroup("Yeet")]
    // [Export] private float _maxYeetDist;
    // [Export] private float _maxYeetForce;

    private const float YeetMaxDist = 100;
    private const float YeetMaxDistShow = 25;
    private const float YeetMaxForce = 300;

    private const float YeetMouseCamOffsetDist = 200;

    // Refs
    // private RigidBody2D _rb;

    private Node2D _interpolated;
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
    private bool _faceVelocity = true;

    public override void _Ready()
    {
        _interpolated = GetNode<Node2D>("Interpolated");
        _visuals = GetNode<Node2D>("Visuals");
        _blob = GetNode<Node2D>("Blob");
        _arrow = GetNode<Node2D>("Arrow");
    }

    public override void _Process(double delta)
    {
        if (Input.IsActionJustPressed("hop"))
        {
            Game.PlayDialogue(DIALOGUE);
        }

        // Input
        _iYeet.Update("yeet");

        // _visuals.Rotation = 0;

        // Interpolated
        // _visuals.GlobalPosition = _interpolated.GlobalPosition;
        GlobalPosition = _interpolated.GlobalPosition;

        // Yeeting
        if (_iYeet.Pressed)
        {
            _isYeeting = true;
            _faceVelocity = false;
            _yeetDragStart = GetGlobalMousePosition();

            _blob.Visible = true;
            _arrow.Visible = true;
        }

        if (_isYeeting)
        {
            // Camera Offset
            var screenSize = GetViewport().GetVisibleRect().Size * 1 / Game.Camera.Zoom;
            var scaledMousePos = GetLocalMousePosition() / screenSize;
            // Game.Camera.Offset += YeetMouseCamOffsetDist * 2f * scaledMousePos;

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
                _faceVelocity = true;
                _blob.Visible = false;
                _arrow.Visible = false;

                Vector2 dir = offset.Normalized();
                _rb.LinearVelocity = Vector2.Zero;
                _rb.ApplyImpulse(dir * t * YeetMaxForce);
            }

            QueueRedraw();
        }

        if (_faceVelocity)
        {
            _visuals.Rotation = -_rb.LinearVelocity.AngleTo(Vector2.Up);
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