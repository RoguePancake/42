using Godot;
using System;
using System.Linq;
using Warship.Core;
using Warship.Events;
using Warship.Data;

namespace Warship.Engines;

/// <summary>
/// Throws random Crisis events at the player when turns advance.
/// </summary>
public partial class CrisisEngine : Node
{
    public override void _Ready()
    {
        EventBus.Instance?.Subscribe<TurnAdvancedEvent>(OnTurnAdvanced);
        EventBus.Instance?.Subscribe<CrisisResolvedEvent>(OnCrisisResolved);
        GD.Print("[CrisisEngine] Online. Waiting for the right moment to strike.");
    }

    public override void _ExitTree()
    {
        EventBus.Instance?.Unsubscribe<TurnAdvancedEvent>(OnTurnAdvanced);
        EventBus.Instance?.Unsubscribe<CrisisResolvedEvent>(OnCrisisResolved);
    }

    private void OnTurnAdvanced(TurnAdvancedEvent ev)
    {
        if (ev.Turn > 1 && SimRng.NextDouble() < 0.20)
        {
            TriggerRandomCrisis();
        }
    }

    private void TriggerRandomCrisis()
    {
        int roll = SimRng.Next(3);

        switch(roll)
        {
            case 0:
                EventBus.Instance?.Publish(new CrisisTriggeredEvent(
                    "data_leak",
                    "URGENT: Ministry Data Leak",
                    "Whistleblowers have published classified Ministry documents online detailing your illegal surveillance programs. The international community is outraged.",
                    new string[] {
                        "Deny Everything (-5% Prestige)",
                        "Scapegoat the Director (+5% Stability, -$200M)",
                        "Purge the Ministry (-10% Stability, +10% Prestige)"
                    }
                ));
                break;
            case 1:
                EventBus.Instance?.Publish(new CrisisTriggeredEvent(
                    "border_skirmish",
                    "FLASHPOINT: Border Skirmish",
                    "Rogue elements of a rival military opened fire on your border patrol. Casualties are light, but the media is demanding a harsh response.",
                    new string[] {
                        "Stand down (-5% Prestige, +5% Stability)",
                        "Fund retaliation paramilitaries (-$200M, +10% Prestige)",
                        "Full military response (-$500M, +15% Prestige, +10 War Weariness)"
                    }
                ));
                break;
            case 2:
                EventBus.Instance?.Publish(new CrisisTriggeredEvent(
                    "economic_collapse",
                    "CRISIS: Bank Run",
                    "A shadow cartel is shorting your nation's currency, leading to widespread panic and bank runs in the capital.",
                    new string[] {
                        "Bail out the banks (-$500M, +10% Stability)",
                        "Arrest the bankers (-10% Stability, +$200M)",
                        "Seize cartel assets (-15% Prestige, +$400M)"
                    }
                ));
                break;
        }
    }

    private void OnCrisisResolved(CrisisResolvedEvent ev)
    {
        var world = WorldStateManager.Instance?.Data;
        if (world == null) return;

        if (world.PlayerNationId == null) return;
        var parts = world.PlayerNationId.Split('_');
        if (parts.Length < 2 || !int.TryParse(parts[1], out int pIdx)) return;
        if (pIdx < 0 || pIdx >= world.Nations.Count) return;
        var playerNation = world.Nations[pIdx];

        if (ev.CrisisId == "data_leak")
        {
            if (ev.ChoiceIndex == 0) { playerNation.Prestige = Math.Clamp(playerNation.Prestige - 5, 0, 100); }
            if (ev.ChoiceIndex == 1) { playerNation.Stability = Math.Clamp(playerNation.Stability + 5, 0, 100); playerNation.Treasury -= 200; }
            if (ev.ChoiceIndex == 2) { playerNation.Stability = Math.Clamp(playerNation.Stability - 10, 0, 100); playerNation.Prestige = Math.Clamp(playerNation.Prestige + 10, 0, 100); }
        }
        else if (ev.CrisisId == "border_skirmish")
        {
            if (ev.ChoiceIndex == 0) { playerNation.Prestige = Math.Clamp(playerNation.Prestige - 5, 0, 100); playerNation.Stability = Math.Clamp(playerNation.Stability + 5, 0, 100); }
            if (ev.ChoiceIndex == 1) { playerNation.Treasury -= 200; playerNation.Prestige = Math.Clamp(playerNation.Prestige + 10, 0, 100); }
            if (ev.ChoiceIndex == 2) { playerNation.Treasury -= 500; playerNation.Prestige = Math.Clamp(playerNation.Prestige + 15, 0, 100); playerNation.WarWeariness = Math.Clamp(playerNation.WarWeariness + 10, 0, 100); }
        }
        else if (ev.CrisisId == "economic_collapse")
        {
            if (ev.ChoiceIndex == 0) { playerNation.Treasury -= 500; playerNation.Stability = Math.Clamp(playerNation.Stability + 10, 0, 100); }
            if (ev.ChoiceIndex == 1) { playerNation.Stability = Math.Clamp(playerNation.Stability - 10, 0, 100); playerNation.Treasury += 200; }
            if (ev.ChoiceIndex == 2) { playerNation.Prestige = Math.Clamp(playerNation.Prestige - 15, 0, 100); playerNation.Treasury += 400; }
        }

        EventBus.Instance?.Publish(new NotificationEvent("Crisis Resolved.", "info"));
    }
}
