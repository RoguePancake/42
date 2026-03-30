using Godot;
using Warship.Data;

namespace Warship.Core;

/// <summary>
/// Holds the master WorldData — the single source of truth for ALL game state.
/// All mutations go through here or through events. Never mutate data directly from UI.
/// </summary>
public partial class WorldStateManager : Node
{
    public static WorldStateManager? Instance { get; private set; }

    public WorldData World { get; set; } = new();

    public override void _EnterTree()
    {
        if (Instance != null) { QueueFree(); return; }
        Instance = this;
        GD.Print("[WorldState] Online.");
    }
}
