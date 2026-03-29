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
        // 20% chance of a crisis each turn, but not on turn 1
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
                        "Deny Everything (-5% WA, +2% BSA)",
                        "Scapegoat the Director (-10% BSA, +5% TA)",
                        "Purge the Ministry (-15% TA, +15% BSA)"
                    }
                ));
                break;
            case 1:
                EventBus.Instance?.Publish(new CrisisTriggeredEvent(
                    "border_skirmish",
                    "FLASHPOINT: Border Skirmish",
                    "Rogue elements of a rival military opened fire on your border patrol. Casualties are light, but the media is demanding a harsh response.",
                    new string[] {
                        "Ignore it (-10% TA, +5% WA)",
                        "Fund retaliation paramilitaries (+10% TA, -10% WA)",
                        "Launch Covert Assassination (+5% BSA, -15% WA)"
                    }
                ));
                break;
            case 2:
                EventBus.Instance?.Publish(new CrisisTriggeredEvent(
                    "economic_collapse",
                    "CRISIS: Bank Run",
                    "A shadow cartel is shorting your nation's currency, leading to widespread panic and bank runs in the capital.",
                    new string[] {
                        "Bail out the banks (-20% TA, +10% WA)",
                        "Arrest the bankers (+15% TA, -15% WA)",
                        "Seize cartel assets secretly (+20% BSA, -10% TA)"
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
        var playerChar = world.Characters.FirstOrDefault(c => c.IsPlayer);
        
        if (playerChar == null) return;

        // Apply consequences based on Crisis ID and Choice Index
        if (ev.CrisisId == "data_leak")
        {
            if (ev.ChoiceIndex == 0) { ModifyAuth(playerChar, "WA", -5); ModifyAuth(playerChar, "BSA", 2); }
            if (ev.ChoiceIndex == 1) { ModifyAuth(playerChar, "BSA", -10); ModifyAuth(playerChar, "TA", 5); }
            if (ev.ChoiceIndex == 2) { ModifyAuth(playerChar, "TA", -15); ModifyAuth(playerChar, "BSA", 15); }
        }
        else if (ev.CrisisId == "border_skirmish")
        {
            if (ev.ChoiceIndex == 0) { ModifyAuth(playerChar, "TA", -10); ModifyAuth(playerChar, "WA", 5); }
            if (ev.ChoiceIndex == 1) { ModifyAuth(playerChar, "TA", 10); ModifyAuth(playerChar, "WA", -10); }
            if (ev.ChoiceIndex == 2) { ModifyAuth(playerChar, "BSA", 5); ModifyAuth(playerChar, "WA", -15); }
        }
        else if (ev.CrisisId == "economic_collapse")
        {
            if (ev.ChoiceIndex == 0) { ModifyAuth(playerChar, "TA", -20); ModifyAuth(playerChar, "WA", 10); }
            if (ev.ChoiceIndex == 1) { ModifyAuth(playerChar, "TA", 15); ModifyAuth(playerChar, "WA", -15); }
            if (ev.ChoiceIndex == 2) { ModifyAuth(playerChar, "BSA", 20); ModifyAuth(playerChar, "TA", -10); }
        }
        
        // Refresh UI
        EventBus.Instance?.Publish(new NotificationEvent("Crisis Resolved.", "info"));
        // Trick Dossier panel into refreshing the player's stats
        EventBus.Instance?.Publish(new AuthorityChangedEvent(playerChar.Id, "ALL", 0, 0, "Crisis over"));
    }
    
    private void ModifyAuth(CharacterData c, string type, float amount)
    {
        if (type == "TA") c.TerritoryAuthority = Math.Clamp(c.TerritoryAuthority + amount, 0, 100);
        if (type == "WA") c.WorldAuthority = Math.Clamp(c.WorldAuthority + amount, 0, 100);
        if (type == "BSA") c.BehindTheScenesAuthority = Math.Clamp(c.BehindTheScenesAuthority + amount, 0, 100);
    }
}
