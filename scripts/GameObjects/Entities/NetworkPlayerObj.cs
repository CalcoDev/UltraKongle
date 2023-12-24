using System.Collections;
using Godot;
using KongleJam.Components;
using KongleJam.Managers;

namespace KongleJam.GameObjects.Entities;

public partial class NetworkPlayerObj : Node2D
{
    [ExportGroup("Network sync")]
    [Export] private NetworkSyncComponent _sync;
    [Export] private HealthComponent _health;
    [Export] private AnimationPlayer _anim;
    
    public long NetworkId { get; set; } = -1;

    private Label _usernameLabel;
    private ProgressBar _healthbar;

    private Node2D _visuals;

    private bool _isDashing;
    private bool _wasDashing;
    
    public override void _Ready()
    {
        _usernameLabel = GetNode<Label>("%Username");
        _healthbar = GetNode<ProgressBar>("%Healthbar");
        _visuals = GetNode<Node2D>("Visuals");
        
        NetworkManager.Instance.OnPlayerDied += OnPlayerDeath;

        // TODO(calco): Already handled by networkmaager onm daeath
        // _health.OnDied += () => {
        // };

        _health.OnDied += () => {
            GD.Print($"{NetworkManager.GetRpcFormat()} Died health.");
        };
        _health.OnHealthChanged += (old, curr, max) => {
            _healthbar.MinValue = 0;
            _healthbar.MaxValue = max;
            _healthbar.Value = curr;
        };
        _usernameLabel.Text = NetworkManager.Instance.Players[NetworkId].Username;

        SyncHealth();
        SyncMaxHealth();

        _anim.Play("normal");
    }

    public override void _ExitTree()
    {
        NetworkManager.Instance.OnPlayerDied -= OnPlayerDeath;
    }

    private bool _dead = false;
    private void OnPlayerDeath(long id)
    {
        _anim.Play("normal");
        
        GD.Print($"{NetworkManager.GetRpcFormat()} Received signal of {id} death.");

        if (id != NetworkId)
            return;

        // TODO(calco): PARTICLE EFFECVET
        // QueueFree();
        _usernameLabel.AddThemeColorOverride("font_color", new Color("#a53030"));
        _healthbar.QueueFree();
        _health.QueueFree();

        _dead = true;
    }

    public override void _Process(double delta)
    {
        SyncGlobalPosition();
        SyncRotation();
        
        if (_dead)
            return;
        
        SyncDash();
        SyncHealth();
        SyncMaxHealth();
    }

    private void SyncDash()
    {
        if (!_sync.HasValue(NetworkId, "player_is_dashing"))
            return;
        bool dashing = _sync.GetValue(NetworkId, "player_is_dashing").As<bool>();
        _wasDashing = _isDashing;
        _isDashing = dashing;

        if (!_wasDashing && _isDashing)
            _anim.Play("dash");
        else if (_wasDashing && !_isDashing)
            _anim.Play("normal");
    }

    private void SyncHealth()
    {
        if (!_sync.HasValue(NetworkId, "player_health"))
            return;
        float health = _sync.GetValue(NetworkId, "player_health").As<float>();
        _health.Health = health;
    }

    private void SyncMaxHealth()
    {
        if (!_sync.HasValue(NetworkId, "player_maxhealth"))
            return;
        float maxhealth = _sync.GetValue(NetworkId, "player_maxhealth").As<float>();
        _health.MaxHealth = maxhealth;
    }
    
    private void SyncGlobalPosition()
    {
        if (!_sync.HasValue(NetworkId, "player_position"))
            return;
        Vector2 position = _sync.GetValue(NetworkId, "player_position").As<Vector2>();
        GlobalPosition = position;
    }

    private void SyncRotation()
    {
        if (!_sync.HasValue(NetworkId, "player_rotation"))
            return;
        float rotation = _sync.GetValue(NetworkId, "player_rotation").As<float>();
        _visuals.Rotation = rotation;
    }
}