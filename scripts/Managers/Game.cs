using Godot;
using KongleJam.Components;
using KongleJam.GameObjects;

namespace KongleJam.Managers;

public partial class Game : Node
{
    public static Game Instance { get; private set; }

    public static float DeltaTime { get; private set; }
    public static float FixedTime { get; private set; }

    public static Camera Camera { get; set; }

    public override void _EnterTree()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            GD.PrintErr("ERROR: Game Manager already exists!");
            QueueFree();
        }
    }

    public override void _Process(double delta)
    {
        if (Input.IsActionJustPressed("quit"))
            GetTree().Quit();

        if (Input.IsActionJustPressed("fullscreen"))
        {
            DisplayServer.WindowSetMode(
                DisplayServer.WindowGetMode() ==
                DisplayServer.WindowMode.Fullscreen
                    ? DisplayServer.WindowMode.Windowed
                    : DisplayServer.WindowMode.Fullscreen);
        }

        DeltaTime = (float)GetProcessDeltaTime();
        FixedTime = (float)GetPhysicsProcessDeltaTime();
    }

    private static Tween timeTween;
    public static void TweenTime(float timescale, float time)
    {
        timeTween?.Kill();

        timeTween = Instance.GetTree().CreateTween();
        timeTween.SetEase(Tween.EaseType.OutIn);
        timeTween.TweenMethod(
            new Callable(Instance, MethodName.SetTimeScale),
            Engine.TimeScale,
            timescale,
            time
        );
    }

    private static void SetTimeScale(float time)
    {
        Engine.TimeScale = time;
    }

    public static void Hitstop(float time, float timescale = 0.05f)
    {
        Engine.TimeScale = timescale;
        Instance.GetTree().CreateTimer(time * timescale)
            .Connect(
                Timer.SignalName.Timeout,
                new Callable(Instance, MethodName.ResetTimeScale)
            );
    }

    private static void ResetTimeScale()
    {
        Engine.TimeScale = 1f;
    }
}