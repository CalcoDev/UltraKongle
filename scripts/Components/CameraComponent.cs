using Godot;
using KongleJam.Managers;

namespace KongleJam.Components;

public partial class CameraComponent : Camera2D
{
    [Export] private Node2D Follow;

    [Export] public bool MouseOffset { get; set; } = true;
    [Export] private float MouseOffsetDist = 100f;
    [Export] private float PositionLerpSpeed = 2f;
    [Export] private float ZoomLerpSpeed = 2f;

    public Vector2 AdditionalOffset { get; set; }
    public float CZoom { get; set; } = 1f;
    public float AdditionalZoom { get; set; } = 0f;

    private FastNoiseLite _noise = new();

    private float _noiseSpeed;
    private float _noiseStrength;
    private float _shakeDecay;

    private float _noiseC = 0f;

    private float _shakeTime;
    private bool _shakeFade;

    public override void _Ready()
    {
        Game.Camera = this;

        _noise.NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin;
    }

    public override void _Process(double delta)
    {
        // Zoom
        Vector2 targetZoom = new Vector2(CZoom + AdditionalZoom, CZoom + AdditionalZoom);
        AdditionalZoom = 0f;
        Zoom = Zoom.Lerp(targetZoom, Game.DeltaTime * ZoomLerpSpeed);

        // Position
        Vector2 targetPos = Follow.Position + AdditionalOffset;
        AdditionalOffset = Vector2.Zero;
        if (MouseOffset)
        {
            var s1 = GetViewport().GetVisibleRect().Size * 1 / Zoom;
            var scaled = GetLocalMousePosition() / s1;
            targetPos += MouseOffsetDist * 2f * scaled;
        }

        if (_shakeFade)
            _noiseStrength = Mathf.Lerp(_noiseStrength, 0, _shakeDecay * Game.DeltaTime);
        else
            _shakeTime -= Game.DeltaTime;

        if (!_shakeFade && _shakeTime > 0)
        {
            _noiseC += Game.DeltaTime * _noiseSpeed;
            Vector2 shakeOffset = new Vector2(
                _noise.GetNoise2D(1, _noiseC),
                _noise.GetNoise2D(100, _noiseC)
            ) * _noiseStrength;
            targetPos += shakeOffset;
        }

        Position = Position.Lerp(targetPos, Game.DeltaTime * PositionLerpSpeed);
    }

    public void ShakeTime(float speed, float strength, float time)
    {
        _noiseSpeed = speed;
        _noiseStrength = strength;

        _shakeTime = time;
        _shakeFade = false;
    }

    public void ShakeFade(float speed, float strength, float decay)
    {
        _noiseSpeed = speed;
        _noiseStrength = strength;

        _shakeDecay = decay;
        _shakeFade = true;
    }
}