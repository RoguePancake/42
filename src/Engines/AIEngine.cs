using Godot;
using System;
using System.Linq;
using Warship.Core;
using Warship.Events;

namespace Warship.Engines;

/// <summary>
/// Controls Rival VIPs. When a turn drops, the AI analyzes the board 
/// and attempts to increase their own Full Authority Index (FAI) by 
/// targeting the weakest links or the player.
/// </summary>
public partial class AIEngine : Node
{
    private Random _rng = new();

    public override void _Ready()
    {
        EventBus.Instance!.Subscribe<TurnAdvancedEvent>(OnTurnAdvanced);
        GD.Print("[AIEngine] Online. Rivals are plotting.");
    }

    private void OnTurnAdvanced(TurnAdvancedEvent ev)
    {
        var world = WorldStateManager.Instance?.Data;
        if (world == null) return;

        // Everyone who is alive and not the player
        var rivals = world.Characters.Where(c => !c.IsPlayer && c.Role != "Eliminated").ToList();

        foreach (var rival in rivals)
        {
            // AI decides if it wants to act this turn (60% chance)
            if (_rng.NextDouble() > 0.6) continue;

            // Pick a target: normally the player, or another rival with high FAI
            var targetOptions = world.Characters.Where(c => c.Id != rival.Id && c.Role != "Eliminated").ToList();
            if (targetOptions.Count == 0) continue;

            // Target the player 40% of the time, else random rival
            var target = targetOptions[_rng.Next(targetOptions.Count)];
            bool targetIsPlayer = _rng.NextDouble() < 0.4f;
            if (targetIsPlayer)
            {
                var p = targetOptions.FirstOrDefault(c => c.IsPlayer);
                if (p != null) target = p;
            }

            string action = DetermineBestAction(rival, target);
            if (action != "none")
            {
                GD.Print($"[AIEngine] Rival {rival.Name} chose action {action} against {target?.Name}");
                EventBus.Instance?.Publish(new PoliticalActionEvent(rival.Id, target?.Id ?? "", action));
            }
        }
    }

    private string DetermineBestAction(Warship.Data.CharacterData rival, Warship.Data.CharacterData target)
    {
        // 1. If TA is critically low, they panic and fund militia
        if (rival.TerritoryAuthority < 30f)
            return "fund_militia";

        // 2. If BSA is extremely high, they get cocky and might try to eliminate
        if (rival.BehindTheScenesAuthority > 60f && target.FullAuthorityIndex > rival.FullAuthorityIndex)
        {
            if (_rng.NextDouble() < 0.1f) // 10% chance to go for the kill
                return "eliminate";
        }

        // 3. Main actions
        double r = _rng.NextDouble();
        if (r < 0.25)
            return "public_address"; // Boost own WA
        else if (r < 0.50)
            return "review_intel"; // Boost own BSA
        else if (r < 0.75)
            return "investigate"; // Gain intel on target
        else
            return "threaten"; // Attack target WA
    }
}
