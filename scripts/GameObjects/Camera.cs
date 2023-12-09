using Godot;
using KongleJam.Managers;

namespace KongleJam.GameObjects;

public partial class Camera : Camera2D
{
	[Export] public Node2D Follow;

    [Export] public float PositionLerpSpeed = 2f;
    [Export] public float ZoomLerpSpeed = 2f;

    [Export] public bool Static = false;

    public new Vector2 Offset;

    public float ZoomScaled = 1f;
    public float AdditionalZoom = 0f;

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
        if (Static)
            return;

        // Zoom
        Vector2 targetZoom = new Vector2(ZoomScaled + AdditionalZoom, ZoomScaled + AdditionalZoom);
        AdditionalZoom = 0f;
        Zoom = Zoom.Lerp(targetZoom, Game.DeltaTime * ZoomLerpSpeed);

        // Position
        Vector2 targetPos = Follow.Position + Offset;
        Offset = Vector2.Zero;

        // Shake
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