using Godot;
using KongleJam.Resources;

namespace KongleJam.Managers;

public partial class DialogueManager : Node
{
    private enum State
    {
        Idle,
        Playing,
        Finished
    }

    [ExportGroup("References")]
    [Export] private AnimationPlayer _anim;
    [Export] private Control _dialogueControl;
    [Export] private TextureRect _speakerSprite;
    [Export] private Label _text;

    [ExportGroup("Settings")]
    [Export] public float BaseSpeed;

    private State _state;
    private Dialogue _dialogue;
    private float _index;

    [Signal]
    public delegate void OnDialogueFinishedEventHandler(Dialogue dialogue);

    public override void _EnterTree()
    {
        Game.Instance.Dialogue = this;
    }

    public override void _Ready()
    {
        _anim.AnimationFinished += OnAnimFinished;
    }

    public override void _Process(double delta)
    {
        if (_state == State.Finished)
        {
            bool next = _dialogue.AutoPlay ||
                        Input.IsActionJustPressed("next_dialogue");

            if (_dialogue.Next != null && next)
                StartDialogue(_dialogue.Next);
        }

        if (_state == State.Idle)
            return;

        if (Input.IsActionJustPressed("skip_dialogue"))
            _index = _dialogue.Message.Length;

        _index += BaseSpeed * _dialogue.PlaybackSpeedMult * Game.DeltaTime;
        if (_index < 0f)
            _index = 0f;
        else if (_index > _dialogue.Message.Length)
        {
            _index = _dialogue.Message.Length;

            _state = State.Finished;
            if (_dialogue.Next == null)
                _anim.Play("exit");
        }

        _index = Mathf.Clamp(_index, 0f, _dialogue.Message.Length);
        _text.Text = _dialogue.Message[..(Mathf.FloorToInt(_index) + 1)];
    }

    public void StartDialogue(Dialogue dialogue)
    {
        _speakerSprite.Texture = dialogue.Speaker.Texture;
        _dialogue = dialogue;

        if (_state != State.Playing)
            _anim.Play("enter");
    }

    private void OnAnimFinished(StringName name)
    {
        if (name == "enter")
            _state = State.Playing;
        else if (name == "exit")
            EmitSignal(SignalName.OnDialogueFinished, _dialogue);
    }
}