using Godot;
using System;
using System.Linq;
using Warship.Core;
using Warship.Events;

namespace Warship.Engines;

/// <summary>
/// Controls Rival VIPs. When a turn drops, the AI analyzes the board
/// and attempts to expand territory and weaken rivals through
/// military and political actions.
/// </summary>
public partial class AIEngine : Node
{
    public override void _Ready()
    {
        EventBus.Instance?.Subscribe<TurnAdvancedEvent>(OnTurnAdvanced);
        GD.Print("[AIEngine] Online. Rivals are plotting.");
    }

    public override void _ExitTree()
    {
        EventBus.Instance?.Unsubscribe<TurnAdvancedEvent>(OnTurnAdvanced);
    }

    private void OnTurnAdvanced(TurnAdvancedEvent ev)
    {
        var world = WorldStateManager.Instance?.Data;
        if (world == null) return;

        var rivals = world.Characters.Where(c => !c.IsPlayer && c.Role != "Eliminated").ToList();

        foreach (var rival in rivals)
        {
            if (SimRng.NextDouble() > 0.6) continue;

            var targetOptions = world.Characters.Where(c => c.Id != rival.Id && c.Role != "Eliminated").ToList();
            if (targetOptions.Count == 0) continue;

            var target = targetOptions[SimRng.Next(targetOptions.Count)];
            bool targetIsPlayer = SimRng.NextDouble() < 0.4f;
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
        var world = WorldStateManager.Instance?.Data;
        if (world == null) return "none";

        var rivalNation = GetNation(rival);

        // If stability is critically low, fund militia to stabilize
        if (rivalNation != null && rivalNation.Stability < 30f)
            return "fund_militia";

        // If prestige is high and they're aggressive, try elimination
        if (rivalNation != null && rivalNation.Prestige > 60f)
        {
            if (SimRng.NextDouble() < 0.1f)
                return "eliminate";
        }

        // Main actions — weighted random
        double r = SimRng.NextDouble();
        if (r < 0.25)
            return "fortify";
        else if (r < 0.50)
            return "review_intel";
        else if (r < 0.75)
            return "investigate";
        else
            return "threaten";
    }

    private static Warship.Data.NationData? GetNation(Warship.Data.CharacterData character)
    {
        var world = WorldStateManager.Instance?.Data;
        if (world == null) return null;
        var parts = character.NationId.Split('_');
        if (parts.Length < 2 || !int.TryParse(parts[1], out int idx)) return null;
        return idx >= 0 && idx < world.Nations.Count ? world.Nations[idx] : null;
    }
}
