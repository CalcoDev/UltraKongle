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
    public static Player Instance { get; private set; }

    [Export(PropertyHint.ResourceType, "Dialogue")] private Dialogue DIALOGUE;

    [ExportGroup("Network sync")]
    [Export] private NetworkSyncComponent _sync;

    [ExportGroup("References")]
    [Export] private RigidBody2D _rb;
    [Export] private Area2D _groundedChecker;
    [Export] private StateMachineComponent _sm;
    [Export] private HealthComponent _health;
    [Export] private AnimationPlayer _anim;

    private const float YeetMaxDist = 100;
    private const float YeetMaxDistShow = 25;
    private const float YeetMaxForce = 300;

    private const int YeetCountMax = 2;

    // Refs
    private Node2D _interpolated;
    private InterpolationComponent _interpComp;
    private Node2D _visuals;

    private Node2D _blob;
    private Node2D _arrow;

    // Input
    private Key _iYeet;
    private Key _iDash;

    // States
    public const int StNormal = 0;
    public const int StDash = 1;

    private bool _isYeeting;

    // Yeeting
    private Vector2 _yeetDragStart;
    private Vector2 _yeetDragEnd;
    private bool _faceVelocity = true;
    private int _yeetCountRemaining;

    // General
    private bool _isGrounded = false;

    // STUFF
    public bool Locked { get; set; } = false;

    private Label _usernameLabel;
    private ProgressBar _healthbar;

    private Timer _dashCooldownTimer;
    private Timer _dashCollTimer;


    public override void _EnterTree()
    {
        if (Instance != null)
        {
            GD.Print("WARNING: Player already exists! Overriding!");
        }
        Instance = this;

        _interpolated = GetNode<Node2D>("Interpolated");
        _visuals = GetNode<Node2D>("Visuals");
        _blob = GetNode<Node2D>("Blob");
        _arrow = GetNode<Node2D>("Arrow");

        _interpComp = GetNode<InterpolationComponent>("InterpolationComponent");
        _interpComp.ResetPos = true;
        _interpComp.StartPos = GlobalPosition;

        GD.Print($"{NetworkManager.GetRpcFormat()} Trying to teleport interp comp to {GlobalPosition}!");

        _interpComp.Follow = _rb;
        _interpComp.Follower = _interpolated;

        _usernameLabel = GetNode<Label>("%Username");
        _healthbar = GetNode<ProgressBar>("%Healthbar");

        _dashCollTimer = GetNode<Timer>("DashCollTimer");
        _dashCooldownTimer = GetNode<Timer>("DashCooldownTimer");
    }

    public override void _Ready()
    {
        _groundedChecker.BodyEntered += OnEnterGround;
        _groundedChecker.BodyExited += OnExitGround;

        _rb.BodyEntered += OnCollide;
        _rb.BodyExited += OnExitCollide;
        
        _yeetCountRemaining = YeetCountMax;

        _sm.Init(4, StNormal);
        _sm.SetCallbacks(StNormal, NormalUpdate, NormalPhysics);
        _sm.SetCallbacks(StDash, DashUpdate, null, DashEnter, DashExit);

        _health.OnDied += () => {
            GD.Print($"{NetworkManager.GetRpcFormat()} Died. Announcing death.");
            NetworkManager.Instance.AnnounceDeath();

            // TODO(calco): Some sort of death menu screen thing
            Locked = true;
        
            _usernameLabel.AddThemeColorOverride("font_color", new Color("#a53030"));
            _healthbar.QueueFree();
            _health.QueueFree();
        };
        _health.OnHealthChanged += (old, curr, max) => {
            _healthbar.MinValue = 0;
            _healthbar.MaxValue = max;
            _healthbar.Value = curr;

            _sync.SyncValue("player_health", Variant.From(curr));
            _sync.SyncValue("player_maxhealth", Variant.From(max));
        };
        _health.ResetHealth();

        _usernameLabel.Text = NetworkManager.Username;

        _dashCollTimer.Start();
        _dashCooldownTimer.Start();
    }

    public override void _Process(double delta)
    {
        if (Locked)
        {
            GlobalPosition = _interpolated.GlobalPosition;
            _sync.SyncValue("player_position", Variant.From(GlobalPosition));
            return;
        }

        HandleInput();

        if (Input.IsActionJustPressed("ui_focus_next"))
        {
            _health.TakeDamage(25f);
        }
        
        GlobalPosition = _interpolated.GlobalPosition;

        int newState = _sm.Update();
        _sm.SetState(newState);

        if (Game.Camera.IsOutOfBounds(GlobalPosition))
        {
            Vector2 wrapped = Game.Camera.WrapAroundBounds(GlobalPosition);
            _interpComp.ForceSetPosition(wrapped);
        }

        _sync.SyncValue("player_position", Variant.From(GlobalPosition));
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
        _iDash.Update("dash");
    }

    // STATES

    // NORMAL STATE
   private int NormalUpdate()
   {
        // GD.Print(_dashCooldownTimer.TimeLeft);
        if (_iDash.Pressed && Calc.FloatEquals((float)_dashCooldownTimer.TimeLeft, 0f))
            return StDash;

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
            _yeetDragEnd = GetGlobalMousePosition();
            Vector2 offset = _yeetDragStart - _yeetDragEnd;

            float angle = -offset.AngleTo(Vector2.Up);
            _visuals.Rotation = angle;
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
            }
        }
        else
        {
            _sync.SyncValue("player_rotation", Variant.From(_visuals.Rotation));
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

    // DASH STATE
    private Vector2 _dashDir;
    private Vector2 _dashStartPos;

    private const float DashSpeed = 400f;
    private const float DashEndVelocity = 150f;
    private const float MaxDashDist = 75;

    private void DashEnter()
    {
        _sync.SyncValue("player_is_dashing", Variant.From(true));
        _anim.Play("dash");
        // Game.Hitstop(0.1f);
        // Game.Camera.ShakeTime(5000f, 1000f, 0.2f);

        _dashCollTimer.Start(0.05f);

        _rb.GravityScale = 0;
        _rb.LinearVelocity = Vector2.Zero;
        _rb.AngularVelocity = 0;

        _dashDir = (GetGlobalMousePosition() - GlobalPosition).Normalized();
        _dashStartPos = GlobalPosition;
        
        _visuals.Rotation = _dashDir.AngleTo(Vector2.Up);
    }

    private bool _collided = false;
    private int DashUpdate()
    {
        float distSqr = (GlobalPosition - _dashStartPos).LengthSquared();
        if (distSqr >= MaxDashDist * MaxDashDist)
            return StNormal;
        
        _rb.LinearVelocity = _dashDir * DashSpeed;
        if (Calc.FloatEquals((float)_dashCollTimer.TimeLeft, 0f) && _collided)
            return StNormal;

        return StDash;
    }

    private void DashExit()
    {
        _sync.SyncValue("player_is_dashing", Variant.From(false));
        _anim.Play("normal");
        _rb.GravityScale = 1;
        _rb.LinearVelocity = _dashDir * DashEndVelocity;

        _dashCooldownTimer.Start(0.75f);
    }

    // CALLBACK FUNCTIONS
    private void OnEnterGround(Node body)
    {
        _yeetCountRemaining = YeetCountMax;
        _isGrounded = true;
    }

    private void OnExitGround(Node body)
    {
        _isGrounded = false;
    }

    private void OnCollide(Node body)
    {
        _collided = true;
    }

    private void OnExitCollide(Node body)
    {
        _collided = false;
    }
}