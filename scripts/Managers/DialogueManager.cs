using System;
using Godot;
using KongleJam.Resources;

namespace KongleJam.Managers;

public partial class DialogueManager : Node
{
    private enum State
    {
        Playing,
        Finished,
        Idle
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

        _anim.Play("exit");
        _state = State.Finished;
    }

    public override void _Process(double delta)
    {
        switch (_state)
        {
            case State.Idle:
            {
                return;
            }
            case State.Finished:
            {
                if (_dialogue == null)
                {
                    _state = State.Idle;
                    return;
                }

                bool next = _dialogue.AutoPlay ||
                            Input.IsActionJustPressed("next_dialogue");

                if (next)
                {
                    if (_dialogue.Next != null)
                        StartDialogue(_dialogue.Next);
                    else
                    {
                        _state = State.Idle;
                        _anim.Play("exit");
                    }
                }
                return;
            }
            case State.Playing:
            {
                if (Input.IsActionJustPressed("skip_dialogue"))
                    _index = _dialogue.Message.Length;

                _index += BaseSpeed * _dialogue.PlaybackSpeedMult * Game.DeltaTime;
                GD.Print($"Index: {_index} out of {_dialogue.Message.Length}");
                if (_index < 0f)
                    _index = 0f;
                else if (_index > _dialogue.Message.Length)
                {
                    _index = _dialogue.Message.Length;
                    _state = State.Finished;
                }

                _index = Mathf.Clamp(_index, 0f, _dialogue.Message.Length);
                _text.Text = _dialogue.Message[..(Mathf.FloorToInt(_index))];
                return;
            }
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void StartDialogue(Dialogue dialogue)
    {
        _speakerSprite.Texture = dialogue.Speaker.Texture;
        _dialogue = dialogue;
        _index = 0f;
        _text.Text = "";

        if (_state != State.Playing)
            _anim.Play("enter");
    }

    private void OnAnimFinished(StringName name)
    {
        if (name == "enter")
            _state = State.Playing;
        else if (name == "exit" && _dialogue != null)
            EmitSignal(SignalName.OnDialogueFinished, _dialogue);
    }
}