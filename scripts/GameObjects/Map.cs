using Godot;
using KongleJam.GameObjects.Entities;
using KongleJam.Managers;
using KongleJam.Networking.Custom;
using KongleJam.Resources;
using System;
using System.Collections.Generic;
using System.Linq;

namespace KongleJam.GameObjects;

public partial class Map : Node2D
{
	public bool Started {get; private set; } = false;

	private readonly List<Vector2> _spawnPoints = new();
	
	private Timer _startTimer;
	
	private Control _timerUI;
	private Label _timerLabel;

    public override void _Ready()
    {
		Game.Pause();

		foreach (Node2D child in GetNode("%Spawnpoints").GetChildren().Cast<Node2D>())
			_spawnPoints.Add(child.GlobalPosition);

		_timerUI = GetNode<Control>("%TimerUI");
		_startTimer = GetNode<Timer>("%StartTimer");
		_timerLabel = _timerUI.GetNode<Label>("Label");

		Started = false;
		_timerUI.Visible = true;
		_startTimer.Timeout += () => {
			Game.Unpause();
			SetProcess(false);
			Started = true;
			_timerUI.Visible = false;
			Player.Instance.Locked = false;
		};
		_startTimer.Start();

		// Spawn players
		foreach (NetworkPlayer player in NetworkManager.Instance.Players.Values)
		{
			if (player.Index >= _spawnPoints.Count)
			{
				GD.PrintErr($"ERROR: Tried spawning a player with index higher than spawnpoint count! Not spawning him lol.");
				return;
			}
			
			Vector2 spawnPos = _spawnPoints[player.Index];
			Character character = Game.Instance.Characters[player.CharacterId];

			Node2D prefab;
			string name = $"player_{player.Id}";
			if (NetworkManager.IsSelfId(player.Id))
			{
				Player localPlayer = character.LocalPrefab.Instantiate<Player>();
				localPlayer.Locked = true;
				prefab = localPlayer;
			}
			else
			{
				NetworkPlayerObj networkPlayer = character.ServerPrefab.Instantiate<NetworkPlayerObj>();
				networkPlayer.NetworkId = player.Id;
				prefab = networkPlayer;
			}
			
			prefab.Name = name;
			prefab.GlobalPosition = spawnPos;
			
			AddChild(prefab);

			SetProcess(true);
			GD.Print($"{NetworkManager.GetRpcFormat()} Added player {player.Id} ({player.Username}) with index {player.Index} at position {spawnPos}!");
		}
    }

    public override void _Process(double delta)
    {
		if (!Started)
			_timerLabel.Text = $"{(int)_startTimer.TimeLeft + 1}";
    }
}
