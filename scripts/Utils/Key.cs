using Godot;

namespace KongleJam.Utils;

public struct Key
{
    public bool Pressed;
    public bool Released;
    public bool Down;

    public void Update(string name)
    {
        Pressed = Input.IsActionJustPressed(name);
        Released = Input.IsActionJustReleased(name);
        Down = Input.IsActionPressed(name);
    }
}