using System;
using Godot;
using KongleJam.Resources;

namespace KongleJam.Managers;

public partial class DialogueManager : Node
{
    [Serializable]
    private enum State
    {
        EnterAnimation,
        ExitAnimation,
        Switching,
        Playing,
        Finished,
        Idle,
    }

    [ExportGroup("References")]
    [Export] private AnimationPlayer _anim;
    [Export] private Control _dialogueControl;
    [Export] private TextureRect _speakerSprite;
    [Export] private Label _text;

    [ExportGroup("Settings")]
    [Export] public float BaseSpeed;

    [Export] private State _state;
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
        _state = State.Idle;
    }

    public override void _Process(double delta)
    {
        switch (_state)
        {
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
                    {
                        _state = State.Switching;
                        StartDialogue(_dialogue.Next);
                    }
                    else
                    {
                        _state = State.ExitAnimation;
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
                return;
        }
    }

    public void StartDialogue(Dialogue dialogue)
    {
        _index = 0f;
        _text.Text = "";

        if (_state != State.Switching || _dialogue?.Speaker == dialogue.Speaker)
            if (dialogue.Speaker != null)
                _speakerSprite.Texture = dialogue.Speaker.Texture;

        switch (_state)
        {
            case State.Switching when _dialogue?.Speaker != dialogue.Speaker:
                _anim.Play("exit");
                break;
            case State.Switching when _dialogue?.Speaker == dialogue.Speaker:
                _state = State.Playing;
                break;
            case State.Idle:
                _anim.Play("enter");
                _state = State.EnterAnimation;
                break;
            default:
                GD.PrintErr($"Tried playing a dialogue while not idle! State {_state}");
                break;
        }

        _dialogue = dialogue;
    }

    private void OnAnimFinished(StringName name)
    {
        if (name == "enter")
        {
            _state = State.Playing;
        }
        else if (name == "exit" && _dialogue != null)
        {
            if (_state == State.Switching)
            {
                _anim.Play("enter");
                _speakerSprite.Texture = _dialogue.Speaker.Texture;
            }
            else
                _state = State.Idle;

            EmitSignal(SignalName.OnDialogueFinished, _dialogue);
        }
    }
}