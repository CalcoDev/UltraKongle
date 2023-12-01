using Godot;
using KongleJam.Components;

namespace KongleJam;

public partial class Player : Node2D
{
	private enum States
	{
		Locked,
		Normal,
		Yeet,
		Dive
	}

	// References
	[ExportGroup("References")]
	[Export] private CharacterBody2D _rb;
	[Export] private StateMachineComponent _sm;

	[Export] private VelocityComponent _vel;

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

	public override void _Ready()
	{
		_sm.UpdateSelf = false;

		_sm.SetCallbacks((int)States.Locked, null, null);
		_sm.SetCallbacks((int)States.Normal, NormalUpdate, NormalPhysics);
		_sm.SetCallbacks((int)States.Yeet, YeetUpdate, YeetPhysics);
		_sm.SetCallbacks((int)States.Dive, DiveUpdate, DivePhysics);
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
	}

	// NORMAL
	private int NormalUpdate()
	{
		// State transitions
		// Yeet
		if (_inpYeet.Pressed)
			return (int)States.Yeet;

		if (_inpDive.Pressed)
			return (int)States.Dive;

		// Rolling
		_vel.ApproachX();

		// Hopping

		return (int)States.Normal;
	}

	private void NormalPhysics()
	{

	}

	// YEET
	private int YeetUpdate()
	{
		return (int)States.Normal;
	}

	private void YeetPhysics()
	{

	}

	// DIVE
	private int DiveUpdate()
	{
		return (int)States.Normal;
	}

	private void DivePhysics()
	{

	}

}