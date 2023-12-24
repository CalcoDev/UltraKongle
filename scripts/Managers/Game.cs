using Godot;
using Godot.Collections;
using KongleJam.Components;
using KongleJam.GameObjects;
using KongleJam.Resources;

namespace KongleJam.Managers;

public partial class Game : Node
{
    public static Game Instance { get; private set; }

    public static float DeltaTime { get; private set; }
    public static float FixedTime { get; private set; }

	[ExportGroup("Assignables")]
	[Export] public Array<Character> Characters;
    [Export] public Array<PackedScene> Maps;

    public static Camera Camera { get; set; }

    public DialogueManager Dialogue { get; set; }

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

    public override void _Ready()
    {
        // Instance.Dialogue.OnDialogueFinished += dialogue =>
        // {
        //     if (dialogue.Next == null)
        //         GD.Print("GAME: ANIM ENDED");
        //         // SetTimeScale(1);
        // };
    }

    public override void _Process(double delta)
    {
        // TODO(calco): Make sure we properly unintiailize the server when quitting.
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

    public static void PlayDialogue(Dialogue dialogue)
    {
        // SetTimeScale(0);
        GD.Print("GAME: ANIM BEGAN");
        Instance.Dialogue.StartDialogue(dialogue);
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

    public static void Pause()
    {
        Instance.GetTree().Paused = true;
    }

    public static void Unpause()
    {
        Instance.GetTree().Paused = false;
    }

    public static void TogglePause()
    {
        if (Instance.GetTree().Paused)
            Unpause();
        else
            Pause();
    }
}