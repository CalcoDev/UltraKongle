using System;
using System.Collections;
using System.Collections.Generic;
using Godot;
using KongleJam.Utils;

namespace KongleJam.Components.Coroutines;

// TODO(calco): Make this more customizable
public partial class CoroutineComponent : Node
{
    [Export] public bool Finished { get; private set; } = false;
    [Export] public bool RemoveOnCompletion { get; set; } = true;
    [Export] public bool UpdateSelf { get; set; } = false;

    private Optional<IYieldable> _yieldable;

    [Signal]
    public delegate void OnFinishedEventHandler();

    // TODO(calco): Convert this to a singal. Sadly, they don't support interface sharing and idunno if it's worth making this a resource.
    public Action<IYieldable> OnYielded;

    private Stack<IEnumerator> _enumerators;

    public static CoroutineComponent Create(Node parent, IEnumerator function,
        bool removeOnComplete = true, bool updateSelf = false, string name = "")
    {
        CoroutineComponent c = new CoroutineComponent();
        c.Init(function, removeOnComplete, updateSelf, name);

        parent.AddChild(c);
        return c;
    }

    public void Init(IEnumerator function, bool removeOnComplete = true, bool updateSelf = false, string name = "")
    {
        GD.Print("CoroutineComponent: Initializing coroutine ", name);

        _enumerators = new Stack<IEnumerator>();

        if (function != null)
        {
            _enumerators.Push(function);

            if (name != "")
                Name = name;
            Name = nameof(function);
        }

        RemoveOnCompletion = removeOnComplete;
        UpdateSelf = updateSelf;

        Finished = false;
        OnYielded ??= null;
    }

    public override void _Process(double delta)
    {
        if (UpdateSelf)
            Update((float)delta);
    }

    public void Update(float delta)
    {
        if (_yieldable.HasValue && !_yieldable.Value.IsDone)
        {
            _yieldable.Value.Update(delta);
            return;
        }

        if (_enumerators.Count == 0)
        {
            Finish();
            return;
        }
        Finished = false;

        IEnumerator now = _enumerators.Peek();
        if (now.MoveNext())
        {
            if (now.Current is not IYieldable yieldable)
                return;

            _yieldable.Value = yieldable;
            OnYielded?.Invoke(_yieldable.Value);
        }
        else
        {
            _enumerators.Pop();
            if (_enumerators.Count == 0)
                Finish();
        }
    }

    public void Finish()
    {
        Finished = true;

        EmitSignal(SignalName.OnFinished);
        if (_yieldable.HasValue)
            OnYielded?.Invoke(_yieldable.Value);

        _yieldable.Clear();

        if (RemoveOnCompletion)
            QueueFree();
    }

    public void Cancel()
    {
        Finished = true;
        _yieldable.Clear();
        _enumerators.Clear();
    }

    public void Replace(IEnumerator function)
    {
        Finished = false;

        _yieldable.Clear();
        _enumerators.Clear();
        _enumerators.Push(function);
    }
}