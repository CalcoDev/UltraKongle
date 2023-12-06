using System;
using Godot;
using KongleJam.Components;
using KongleJam.GameObjects.Physics;
using KongleJam.Managers;
using KongleJam.Utils;

namespace KongleJam;

public partial class Player : Node2D
{
	private enum States
	{
		Locked,
		Normal,
		Dive,
		MAX
	}

	// References
	[ExportGroup("References")]
	[Export] private Actor _rb;
	// [Export] private RigidBody2D _rb;

	[Export] private StateMachineComponent _sm;

	[Export] private VelocityComponent _vel;

	[Export] private Area2D _groundChecker;
	[Export] private Area2D _leftWallChecker;
	[Export] private Area2D _rightWallChecker;

	[Export] private Line2D _line;

	[Export] private Timer _hopBuffer;
	[Export] private Timer _diveCooldownTimer;
	[Export] private Timer _diveHopTimer;

	// [ExportGroup("Movement")]
	// [Export] private float RollAccel;
	// [Export] private float RollReduce;
	//
	//

	[ExportGroup("New Movement")]
	[Export] private float GroundFriction;

	[Export] private float Gravity;
	[Export] private float MaxVSpeed;
	[Export] private float HopForce;
	[Export] private float WallJumpOffForce;

	[Export] private float MaxHSpeed;
	[Export] private float MaxSpeedDrag;

	[Export] private float RollSpeed;

	[Export] private float DiveSpeed;

	[Export] private float DiveDistMult;

	private Node2D _visuals;

	// Input
	private struct Key
	{
		public bool Pressed;
		public bool Released;
		public bool Down;

		public void Update(string name)
		{
			Pressed = Input.IsActionJustPressed(name);
			Released = Input.IsActionJustReleased(name);
			Down = Input.IsActionPressed(name);
		}
	}

	private float _inpRoll;
	private Key _inpYeet;
	private Key _inpHop;
	private Key _inpDive;
	private Key _inpInvertYeet;

	// Stuff
	private bool _isYeeting;

	private bool _isGrounded;
	private bool _isJumping;
	private bool _isDiveHopping;

	private float _diveDistance;
	private bool _diveHop;
	private Vector2 _diveHopDir;

	// Wall
	private bool _isNextToWall;
	private Vector2 _wallNormal;

	public bool IsClimbing =>
		_isNextToWall && Calc.SameSign(-_wallNormal.X, _inpRoll);
	private bool _wasClimbing;

	public override void _Ready()
	{
		_visuals = GetNode<Node2D>("%Smoothing2D/Visuals");

		_sm.UpdateSelf = false;

		_sm.Init((int)States.MAX, (int)States.Normal);
		_sm.SetCallbacks((int)States.Locked, null, null);
		_sm.SetCallbacks((int)States.Normal, NormalUpdate, NormalPhysics);
		_sm.SetCallbacks((int)States.Dive, DiveUpdate, DivePhysics, DiveEnter, DiveExit);

		_groundChecker.BodyEntered += OnEnterGround;
		_groundChecker.BodyExited += OnExitGround;

		// _leftWallChecker.BodyEntered += _ => OnEnterWall(true);
		// _rightWallChecker.BodyEntered += _ => OnEnterWall(false);

		_leftWallChecker.BodyExited += _ => OnExitWall();
		_rightWallChecker.BodyExited += _ => OnExitWall();
	}

	private void OnExitWall()
	{
		_isNextToWall = false;
	}

	public override void _Process(double delta)
	{
		UpdateInput();
		_sm.SetState(_sm.Update());
	}

	public override void _PhysicsProcess(double delta)
	{
		_sm.Physics();
	}

	// INPUT
	private void UpdateInput()
	{
		_inpRoll = Input.GetAxis("roll_axis_neg", "roll_axis_pos");
		_inpHop.Update("hop");

		_inpYeet.Update("yeet");
		_inpDive.Update("dive");

		_inpInvertYeet.Update("invert_yeet");

		// Timers
		if (_inpHop.Pressed)
			_hopBuffer.Start();
	}

	// NORMAL
	private int NormalUpdate()
	{
		// State transitions
		if (_inpDive.Pressed)
			return (int)States.Dive;

		// climb
		if (!_wasClimbing && IsClimbing)
		{
			// TODO(calco): Play climbing START animation
		}
		else if (_wasClimbing && !IsClimbing)
		{
			// TODO(calco): Play climbing STOP animation
		}

		// Gravity
		if (!_isGrounded)
		{
			if (IsClimbing)
				_vel.ApproachY(0, Gravity * Game.DeltaTime);
			else
				_vel.ApproachY(MaxVSpeed, Gravity * Game.DeltaTime);

			// TODO(calco): Yeet apex control maybe
		}
		else // Friction
		{
			_vel.ApproachX(0f, GroundFriction * Game.DeltaTime);
		}

		if (Mathf.Abs(_vel.X) > MaxHSpeed)
			_vel.ApproachX(MaxHSpeed, MaxSpeedDrag * Game.DeltaTime);

		// Rolling
		if (!Calc.FloatEquals(Mathf.Abs(_inpRoll), 0))
		{
			_vel.AddAdditional(Vector2.Right * _inpRoll * RollSpeed * Game.DeltaTime);
		}

		// Dive Hop
		if (_isGrounded && !_diveHop && _diveHopTimer.TimeLeft > 0f && _hopBuffer.TimeLeft > 0)
		{
			DiveHop();
		}

		// Hop
		if (_hopBuffer.TimeLeft > 0 && (IsClimbing || (!_isDiveHopping && _isGrounded))) // Hop
		{
			_isJumping = true;
			_vel.SetY(-HopForce);

			if (IsClimbing)
			{
				GD.Print("aaaaaaaaa");
				_vel.SetX(_wallNormal.X * WallJumpOffForce);
			}
		}

		// Yeet
		if (_inpYeet.Pressed)
			YeetBegin();
		if (_inpYeet.Released)
			YeetEnd();
		if (_isYeeting)
			YeetUpdate();

		// Camera Physics
		float speed = Mathf.Clamp(_vel.Get().Length(), 0f, 20f) / 20f;
		Game.Camera.AdditionalZoom = -(0.31f * speed);

		_wasClimbing = IsClimbing;

		return (int)States.Normal;
	}

	private void NormalPhysics()
	{
		Vector2 vel = _vel.GetClear();
		_rb.MoveX(vel.X * 60f * Game.FixedTime, OnCollideH);
		_rb.MoveY(vel.Y * 60f * Game.FixedTime, OnCollideV);
	}

	// YEET
	private void YeetBegin()
	{
		_isYeeting = true;
		_line.Visible = true;
		_line.Points = new Vector2[2];
	}

	private void YeetUpdate()
	{
		_line.SetPointPosition(0, _visuals.GlobalPosition);

		if (_inpInvertYeet.Down)
			_line.SetPointPosition(1, 2 * _visuals.GlobalPosition - GetGlobalMousePosition());
		else
			_line.SetPointPosition(1, GetGlobalMousePosition());
	}

	private void YeetEnd()
	{
		_isYeeting = false;
		_line.Visible = false;

		Vector2 force = _line.Points[1] - _line.Points[0];
		force /= 24;

		_vel.Set(force);
	}

	// DIVE
	private Tween _scaleTween;

	private void DiveEnter()
	{
		var state = GetWorld2D().DirectSpaceState;
		var query = PhysicsRayQueryParameters2D.Create(_rb.Position, _rb.Position + Vector2.Down * 10000f, 1 << 0);
		var result = state.IntersectRay(query);

		if (!result.ContainsKey("position"))
		{
			GD.Print("BRUH WTF");
			_sm.SetState((int)States.Normal);
			return;
		}

		_diveDistance = (_rb.Position - result["position"].AsVector2()).Length();
		_diveHopTimer.Start();
		_diveHop = false;

		_vel.Set(Vector2.Down * DiveSpeed);

		_diveHopDir = new Vector2(_inpRoll, -1).Normalized();

		Game.Hitstop(0.1f, 0.05f);
		Game.Camera.ShakeTime(100, 50, 0.15f);

		TweenScale(new Vector2(0.65f, 1.45f), 0.1f);
	}

	private int DiveUpdate()
	{
		if (_diveHopTimer.TimeLeft > 0f && _hopBuffer.TimeLeft > 0)
		{
			_diveHop = true;
		}

		Game.Camera.AdditionalOffset += Vector2.Down * 100f;

		// Camera Physics
		float speed = Mathf.Clamp(_vel.Get().Length(), 0f, 20f) / 20f;
		Game.Camera.AdditionalZoom = -(0.31f * speed);

		return _isGrounded ? (int)States.Normal : (int)States.Dive;
	}

	private void DivePhysics()
	{
		Vector2 vel = _vel.GetClear();
		_rb.MoveX(vel.X * 60f * Game.FixedTime, OnCollideH);
		_rb.MoveY(vel.Y * 60f * Game.FixedTime, OnCollideV);
	}

	private void DiveExit()
	{
		if (_diveHop)
			DiveHop();
		else
			_vel.Set(Vector2.Up * HopForce * 1f);

		_diveCooldownTimer.Start();

		TweenScale(new Vector2(1.45f, 0.65f), 0.1f);
		TweenScaleAdd(Vector2.One, 0.1f);
		// TODO(calco): IMPACT
	}

	private void DiveHop()
	{
		_isDiveHopping = true;

		float dist = _diveDistance * DiveDistMult;
		dist = Mathf.Clamp(dist, 2.5f, 20f);

		Vector2 dir;
		if (_diveHopDir == Vector2.Zero)
			dir = Vector2.Up * dist;
		else
			dir = _diveHopDir * dist;

		_vel.Set(dir);
	}

	// HELPER STUFF
	private void TweenScale(Vector2 next, float duration)
	{
		_scaleTween?.Kill();

		_scaleTween = GetTree().CreateTween().BindNode(this);
		_scaleTween.SetEase(Tween.EaseType.OutIn);
		_scaleTween.TweenProperty(_visuals, "scale", next, duration);
		_scaleTween.Play();
	}

	private void TweenScaleAdd(Vector2 next, float duration)
	{
		// _scaleTween.Pause();
		_scaleTween.TweenProperty(_visuals, "scale", next, duration);
		// _scaleTween.Play();
	}

	// COLLISIONS STUFF
	private void OnCollideH(KinematicCollision2D coll)
	{
		// GD.Print($"Collided with: {coll.GetCollider()} {coll.GetAngle(Vector2.Up) > 1.56f} {coll.GetNormal()}");

		// if (Calc.FloatEquals(_vel.X, 0f))
		// 	return;

		// Wall Cling
		Vector2 normal = coll.GetNormal();
		// 90 deg radians
		if (Calc.FloatEquals(normal.Y, 0f) && Mathf.Abs(normal.X) > 0f && coll.GetAngle(Vector2.Up) > 1.56f)
		{
			// GD.Print($"Collided with: {coll.GetCollider()} {coll.GetAngle(Vector2.Up) > 1.56f} {coll.GetNormal()}");
			_wallNormal = normal;
			_isNextToWall = true;
		}

		_vel.SetX(0f);
	}

	private void OnCollideV(KinematicCollision2D coll)
	{
		if (Calc.FloatEquals(_vel.Y, 0f))
			return;

		_vel.SetY(0f);
	}

	// GROUND STUFF
	private void OnEnterGround(Node body)
	{
		_isJumping = false;
		_isDiveHopping = false;
		_isGrounded = true;
	}

	private void OnExitGround(Node body)
	{
		_isGrounded = false;
	}
}