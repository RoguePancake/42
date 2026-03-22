using Godot;
using System.Linq;
using Warship.Core;
using Warship.Events;

namespace Warship.Engines;

/// <summary>
/// Monitors the game state continuously (or on turn advance) to detect if the Win Condition is met.
/// Victory Condition: Player's Full Authority Index (FAI) >= 90.
/// </summary>
public partial class VictoryEngine : Node
{
    private bool _victoryFired = false;

    public override void _Ready()
    {
        EventBus.Instance!.Subscribe<TurnAdvancedEvent>(OnTurnAdvanced);
        EventBus.Instance!.Subscribe<AuthorityChangedEvent>(OnAuthorityChanged);
        GD.Print("[VictoryEngine] Online. Waiting for absolute power.");
    }

    private void OnTurnAdvanced(TurnAdvancedEvent ev)
    {
        CheckVictory();
    }

    private void OnAuthorityChanged(AuthorityChangedEvent ev)
    {
        CheckVictory();
    }

    private void CheckVictory()
    {
        if (_victoryFired) return;

        var world = WorldStateManager.Instance?.Data;
        if (world == null) return;

        var playerChar = world.Characters.FirstOrDefault(c => c.IsPlayer);
        if (playerChar == null) return;

        if (playerChar.FullAuthorityIndex >= 90f)
        {
            _victoryFired = true;
            GD.Print($"[VictoryEngine] FULL AUTHORITY ACHIEVED (FAI {playerChar.FullAuthorityIndex:0.0}). Triggering end game sequence.");
            EventBus.Instance?.Publish(new NotificationEvent("FULL AUTHORITY ACHIEVED.", "success"));
            
            // Wait 1 second for drama, then popup
            GetTree().CreateTimer(1.5).Timeout += () => 
            {
                var victoryPanel = GetNodeOrNull<UI.HUD.VictoryPanel>("/root/Main/UILayer/VictoryPanel");
                victoryPanel?.ShowVictory();
            };
        }
    }
}
