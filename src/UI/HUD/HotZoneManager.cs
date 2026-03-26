using Godot;
using Warship.Core;
using Warship.Events;

namespace Warship.UI.HUD;

/// <summary>
/// Manages up to 3 HotZoneMap instances, positioned in the top-right of the main section.
/// Subscribes to HotZonePinEvent and HotZoneClearEvent to control hot zone slots.
/// Refreshes all hot zones on TurnAdvancedEvent.
/// </summary>
public partial class HotZoneManager : Control
{
    private HotZoneMap[] _slots = new HotZoneMap[3];

    // Position in the main section area (top-right corner, just inside the RightSidebar)
    private const float RightSidebarWidth = 250f;
    private const float TopOffset = 100f; // Below top bars + view switcher tabs
    private const float SlotSpacing = 8f;
    private const float SlotWidth = 140f;
    private const float SlotHeight = 160f;

    public override void _Ready()
    {
        // Anchor to top-right, inset from RightSidebar
        AnchorLeft = 1f;
        AnchorRight = 1f;
        AnchorTop = 0f;
        AnchorBottom = 0f;
        OffsetLeft = -(RightSidebarWidth + SlotWidth + 12f);
        OffsetRight = -(RightSidebarWidth + 12f);
        OffsetTop = TopOffset;
        OffsetBottom = TopOffset + (SlotHeight + SlotSpacing) * 3;
        MouseFilter = MouseFilterEnum.Ignore;

        // Create 3 hot zone slots stacked vertically
        for (int i = 0; i < 3; i++)
        {
            var zone = new HotZoneMap();
            zone.Setup(i);
            zone.Position = new Vector2(0, i * (SlotHeight + SlotSpacing));
            AddChild(zone);
            _slots[i] = zone;
        }

        // Subscribe to events
        EventBus.Instance?.Subscribe<HotZonePinEvent>(OnPin);
        EventBus.Instance?.Subscribe<HotZoneClearEvent>(OnClear);
        EventBus.Instance?.Subscribe<TurnAdvancedEvent>(_ => RefreshAll());
    }

    private void OnPin(HotZonePinEvent ev)
    {
        if (ev.SlotIndex < 0 || ev.SlotIndex >= 3) return;
        _slots[ev.SlotIndex].PinToLocation(ev.CenterTileX, ev.CenterTileY, ev.Label);
    }

    private void OnClear(HotZoneClearEvent ev)
    {
        if (ev.SlotIndex < 0 || ev.SlotIndex >= 3) return;
        _slots[ev.SlotIndex].ClearPin();
    }

    private void RefreshAll()
    {
        for (int i = 0; i < 3; i++)
            if (_slots[i].IsPinned)
                _slots[i].Redraw();
    }

    /// <summary>
    /// Pin a hot zone to the next available slot. Returns the slot index, or -1 if all full.
    /// </summary>
    public int PinNextAvailable(int tileX, int tileY, string label)
    {
        for (int i = 0; i < 3; i++)
        {
            if (!_slots[i].IsPinned)
            {
                _slots[i].PinToLocation(tileX, tileY, label);
                return i;
            }
        }
        return -1; // All slots occupied
    }
}
