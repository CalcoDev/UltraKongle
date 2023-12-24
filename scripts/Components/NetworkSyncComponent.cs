using System.Collections.Generic;
using Godot;
using KongleJam.Managers;

namespace KongleJam.Components;

public partial class NetworkSyncComponent : Node
{
    private readonly Dictionary<long, Dictionary<string, Variant>> _dict = new();

    public override void _Ready()
    {
        NetworkManager.Instance.OnRpcSyncThing += (id, name, value) => {
            AddOrUpdate(id, name, value);
        };
    }

    public void SyncValue(string name, Variant value)
    {
        AddOrUpdate(NetworkManager.Id, name, value);
        NetworkManager.Instance.SyncThing(name, value);
    }

    public bool HasValue(long id, string name)
    {
        if (!_dict.ContainsKey(id))
            return false;
        
        return _dict[id].ContainsKey(name);
    }

    public Variant GetValue(long id, string name)
    {
        return _dict[id][name];
    }

    public bool SelfHasValue(string name)
    {
        return HasValue(NetworkManager.Id, name);
    }

    public Variant SelfGetValue(string name)
    {
        return GetValue(NetworkManager.Id, name);
    }

    private void AddOrUpdate(long id, string name, Variant value)
    {
        if (!_dict.ContainsKey(id))
            _dict.Add(id, new Dictionary<string, Variant>());
        
        if (!_dict[id].ContainsKey(name))
            _dict[id].Add(name, value);
        else
            _dict[id][name] = value;
    }
}