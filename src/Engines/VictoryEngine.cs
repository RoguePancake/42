using Godot;
using System.Linq;
using Warship.Core;
using Warship.Events;

namespace Warship.Engines;

/// <summary>
/// Monitors the game state each turn to detect if the Win Condition is met.
/// Victory Condition: Player controls ALL territories for 20 consecutive turns.
/// </summary>
public partial class VictoryEngine : Node
{
    private const int RequiredConsecutiveTurns = 20;
    private bool _victoryFired = false;
    private int _consecutiveTurnsWithFullControl = 0;

    public override void _Ready()
    {
        EventBus.Instance?.Subscribe<TurnAdvancedEvent>(OnTurnAdvanced);
        GD.Print("[VictoryEngine] Online. Total domination required for 20 consecutive turns.");
    }

    public override void _ExitTree()
    {
        EventBus.Instance?.Unsubscribe<TurnAdvancedEvent>(OnTurnAdvanced);
    }

    private void OnTurnAdvanced(TurnAdvancedEvent ev)
    {
        CheckVictory();
    }

    private void CheckVictory()
    {
        if (_victoryFired) return;

        var world = WorldStateManager.Instance?.Data;
        if (world == null) return;

        var playerNation = world.Nations.FirstOrDefault(n => n.IsPlayer);
        if (playerNation == null) return;

        int playerIndex = world.Nations.IndexOf(playerNation);

        if (PlayerControlsAllTerritories(world, playerIndex))
        {
            _consecutiveTurnsWithFullControl++;
            GD.Print($"[VictoryEngine] Total territorial control: {_consecutiveTurnsWithFullControl}/{RequiredConsecutiveTurns} consecutive turns.");

            if (_consecutiveTurnsWithFullControl >= RequiredConsecutiveTurns)
            {
                _victoryFired = true;
                GD.Print($"[VictoryEngine] TOTAL DOMINATION ACHIEVED. All territories held for {RequiredConsecutiveTurns} consecutive turns.");
                EventBus.Instance?.Publish(new NotificationEvent("TOTAL DOMINATION ACHIEVED.", "success"));

                GetTree().CreateTimer(1.5).Timeout += () =>
                {
                    EventBus.Instance?.Publish(new NotificationEvent("VICTORY_TRIGGER", "victory"));
                };
            }
        }
        else
        {
            if (_consecutiveTurnsWithFullControl > 0)
            {
                GD.Print($"[VictoryEngine] Territorial control lost. Streak reset from {_consecutiveTurnsWithFullControl}.");
            }
            _consecutiveTurnsWithFullControl = 0;
        }
    }

    private bool PlayerControlsAllTerritories(Warship.Data.WorldData world, int playerIndex)
    {
        if (world.OwnershipMap == null) return false;

        int width = world.OwnershipMap.GetLength(0);
        int height = world.OwnershipMap.GetLength(1);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int owner = world.OwnershipMap[x, y];
                // Skip unclaimed tiles (-1), only check claimed territory
                if (owner != -1 && owner != playerIndex)
                    return false;
            }
        }

        return true;
    }
}
