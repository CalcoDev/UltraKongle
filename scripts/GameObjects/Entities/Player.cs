using System;
using Godot;
using KongleJam.Components;
using KongleJam.Managers;
using KongleJam.Resources;
using KongleJam.Utils;
using Key = KongleJam.Utils.Key;

namespace KongleJam.GameObjects.Entities;

public partial class Player : Node2D
{
    [Export(PropertyHint.ResourceType, "Dialogue")] private Dialogue DIALOGUE;

    [ExportGroup("References")]
    [Export] private RigidBody2D _rb;
    [Export] private Area2D _groundedChecker;
    [Export] private StateMachineComponent _sm;

    // [ExportGroup("Yeet")]
    // [Export] private float _maxYeetDist;
    // [Export] private float _maxYeetForce;

    private const float YeetMaxDist = 100;
    private const float YeetMaxDistShow = 25;
    private const float YeetMaxForce = 300;

    private const float YeetMouseCamOffsetDist = 200;
    
    private const int YeetCountMax = 2;

    // Refs
    // private RigidBody2D _rb;

    private Node2D _interpolated;
    private Node2D _visuals;

    private Node2D _blob;
    private Node2D _arrow;

    // Input
    private Key _iYeet;
    private Key _iDive;
    private Key _iDiveMouse;
    private float _iRoll;

    // States
    public const int StNormal = 0;
    public const int StDive = 1;
    public const int StRoll = 2;

    private bool _isYeeting;

    // Yeeting
    private Vector2 _yeetDragStart;
    private Vector2 _yeetDragEnd;
    private bool _faceVelocity = true;
    private int _yeetCountRemaining;

    // Diving
    private const float DiveImpulse = 450;
    private const float DiveBounceImpulse = 150;

    // Roll
    private const float DiveRollImpulse = 275f;

    // General
    private bool _isGrounded = false;

    public override void _EnterTree()
    {
        _interpolated = GetNode<Node2D>("Interpolated");
        _visuals = GetNode<Node2D>("Visuals");
        _blob = GetNode<Node2D>("Blob");
        _arrow = GetNode<Node2D>("Arrow");

        InterpolationComponent interpComp = GetNode<InterpolationComponent>("InterpolationComponent");
        interpComp.Follow = _rb;
        interpComp.Follower = _interpolated;
    }

    public override void _Ready()
    {
        _groundedChecker.BodyEntered += OnEnterGround;
        _groundedChecker.BodyExited += OnExitGround;

        _yeetCountRemaining = YeetCountMax;

        _sm.Init(4, StNormal);
        _sm.SetCallbacks(StNormal, NormalUpdate, NormalPhysics);
        _sm.SetCallbacks(StDive, DiveUpdate, null, DiveEnter, DiveExit);
        _sm.SetCallbacks(StRoll, RollUpdate, RollPhysics, RollEnter, RollExit);
    }

    public override void _Process(double delta)
    {
        HandleInput();
        
        GlobalPosition = _interpolated.GlobalPosition;

        int newState = _sm.Update();
        _sm.SetState(newState);
    }

    public override void _PhysicsProcess(double delta)
    {
        _sm.Physics();
    }

    public override void _Draw()
    {
        if (_isYeeting)
        {
            DrawLine(_yeetDragStart - Position, _yeetDragEnd - Position, Colors.Aqua);
        }
    }

    // INPUT
    private void HandleInput()
    {
        _iYeet.Update("yeet");
        _iDive.Update("dive");
        _iDiveMouse.Update("dive_mouse");

        _iRoll = Input.GetAxis("roll_axis_neg", "roll_axis_pos");
    }

    // STATES

    // NORMAL STATE
   private int NormalUpdate()
   {
        // TODO(calco): Dive cooldown
        if (_iDive.Pressed || _iDiveMouse.Pressed && !_isGrounded)
            return StDive;

        // Yeeting
        if (_iYeet.Pressed && _yeetCountRemaining > 0)
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
            // var screenSize = GetViewport().GetVisibleRect().Size * 1 / Game.Camera.Zoom;
            // var scaledMousePos = GetLocalMousePosition() / screenSize;
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
                _yeetCountRemaining -= 1;

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

        return StNormal;
    }

    private void NormalPhysics()
    {

    }

    // DIVE STATE
    private float _diveRollDir = 0f;
    private float _diveRollDirTimer = 0f;
    private void DiveEnter()
    {
        _rb.LinearVelocity = Vector2.Zero;
        _rb.ApplyImpulse(Vector2.Down * DiveImpulse);

        _diveRollDirTimer = 0.4f;
        _diveRollDir = _iRoll;

        _visuals.RotationDegrees = 180f;
    }

    private int DiveUpdate()
    {
        // if (_isGrounded)
        //     return StNormal;

        _diveRollDirTimer -= Game.DeltaTime;
        if (_diveRollDirTimer > 0 && Calc.FloatEquals(_diveRollDir, 0))
            _diveRollDir = _iRoll;

        return StDive;
    }

    private void DiveExit()
    {
        // TODO(calco): Add a timer for transitioning into roll
        if (_diveRollDirTimer > 0 && Calc.FloatEquals(_diveRollDir, 0))
            _diveRollDir = _iRoll;
        
        _rb.LinearVelocity = Vector2.Zero;
        if (Calc.FloatEquals(_diveRollDir, 0))
            _rb.ApplyImpulse(Vector2.Up * DiveBounceImpulse);
        else
        {
            float sign = Mathf.Sign(_iRoll);
            _rb.ApplyImpulse(Vector2.Right.Rotated(Mathf.Pi / -10f * sign) * sign * DiveRollImpulse);
            _sm.SetStateForced(StRoll);
        }
    }

    // ROLL FUNCTION
    private float _prevFriction;
    private void RollEnter()
    {
        GD.Print($"SETTING PREV TO {_rb.PhysicsMaterialOverride.Friction}");
        _prevFriction = _rb.PhysicsMaterialOverride.Friction;
        _rb.PhysicsMaterialOverride.Friction = 0f;
    }
    
    private int RollUpdate()
    {
        if (Calc.FloatEquals(_iRoll, 0))
            return StNormal;


        // TODO(calco): Horrible code repeat.
        if (_iYeet.Pressed)
        {
            _isYeeting = true;
            _yeetDragStart = GetGlobalMousePosition();

            _blob.Visible = true;
            _arrow.Visible = true;
        }

        if (_isYeeting)
        {
            _yeetDragEnd = GetGlobalMousePosition();
            Vector2 offset = _yeetDragStart - _yeetDragEnd;

            float angle = -offset.AngleTo(Vector2.Up);
            _arrow.Rotation = angle;

            float dist = offset.Length();
            float t = Mathf.Min(dist / YeetMaxDist, 1f);
            _blob.Rotation = angle;
            _blob.GetChild<Node2D>(0).Position = new Vector2(-2, 8 + t * YeetMaxDistShow);

            QueueRedraw();
            if (_iYeet.Released)
            {
                _yeetCountRemaining -= 1;
                
                _isYeeting = false;
                _faceVelocity = true;
                _blob.Visible = false;
                _arrow.Visible = false;

                Vector2 dir = offset.Normalized();
                _rb.LinearVelocity = Vector2.Zero;
                _rb.ApplyImpulse(dir * t * YeetMaxForce);

                return StNormal;
            }
        }

        return StRoll;
    }
    
    private void RollPhysics()
    {
        // TODO(calco): A bit of slowing down and up
    }
    
    private void RollExit()
    {
        GD.Print($"SETTING FRICTION BACK: {_prevFriction}");
        _rb.PhysicsMaterialOverride.Friction = _prevFriction;
    }
    
    // CALLBACK FUNCTIONS
    private void OnEnterGround(Node body)
    {
        _yeetCountRemaining = YeetCountMax;
        _isGrounded = true;
    
        if (_sm.State == StDive)
            _sm.SetState(StNormal);
    }

    private void OnExitGround(Node body)
    {
        _isGrounded = false;
    }
}