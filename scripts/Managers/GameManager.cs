using Godot;

namespace KongleJam.Managers;

public partial class GameManager : Node
{
    public static GameManager Instance { get; private set; }

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
}