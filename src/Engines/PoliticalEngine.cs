using System;
using System.Linq;
using Godot;
using Warship.Core;
using Warship.Data;
using Warship.Events;

namespace Warship.Engines;

/// <summary>
/// Processes all political actions: bribe, threaten, eliminate, fund militia, etc.
/// Uses dice rolls with prestige-weighted success rates.
/// Routes all results back through EventBus as NotificationEvent.
/// </summary>
public partial class PoliticalEngine : Node
{
    public override void _Ready()
    {
        EventBus.Instance?.Subscribe<PoliticalActionEvent>(OnPoliticalAction);
        GD.Print("[PoliticalEngine] Online. Listening for political actions.");
    }

    public override void _ExitTree()
    {
        EventBus.Instance?.Unsubscribe<PoliticalActionEvent>(OnPoliticalAction);
    }

    private void OnPoliticalAction(PoliticalActionEvent ev)
    {
        var world = WorldStateManager.Instance?.Data;
        if (world == null) return;

        var actor = world.Characters.FirstOrDefault(c => c.Id == ev.ActorId);
        var target = world.Characters.FirstOrDefault(c => c.Id == ev.TargetId);
        if (actor == null) return;

        switch (ev.ActionType)
        {
            case "fund_militia":
                ProcessFundMilitia(actor);
                break;
            case "fortify":
                ProcessFortify(actor);
                break;
            case "review_intel":
                ProcessReviewIntel(actor);
                break;
            case "investigate":
                if (target != null) ProcessInvestigate(actor, target);
                break;
            case "bribe":
                if (target != null) ProcessBribe(actor, target);
                break;
            case "threaten":
                if (target != null) ProcessThreaten(actor, target);
                break;
            case "eliminate":
                if (target != null) ProcessEliminate(actor, target);
                break;
        }
    }

    // ─── Self Actions ────────────────────────────────

    private void ProcessFundMilitia(CharacterData actor)
    {
        var nation = GetActorNation(actor);
        if (nation != null && nation.Treasury < 100f) { Notify("Insufficient Funds! Requires $100M.", "danger"); return; }
        if (nation != null)
        {
            nation.Treasury -= 100f;
            float stabilityGain = 3f + (float)(SimRng.NextDouble() * 3);
            nation.Stability = MathF.Min(100f, nation.Stability + stabilityGain);
            Notify($"Funded domestic militia: Stability +{stabilityGain:0.0}%, Treasury -$100M", "success");
        }
    }

    private void ProcessFortify(CharacterData actor)
    {
        var nation = GetActorNation(actor);
        if (nation != null && nation.Treasury < 150f) { Notify("Insufficient Funds! Requires $150M.", "danger"); return; }
        if (nation != null)
        {
            nation.Treasury -= 150f;
            float stabilityGain = 5f + (float)(SimRng.NextDouble() * 3);
            nation.Stability = MathF.Min(100f, nation.Stability + stabilityGain);
            Notify($"Fortified territories: Stability +{stabilityGain:0.0}%, Treasury -$150M", "success");
        }
    }

    private void ProcessReviewIntel(CharacterData actor)
    {
        var nation = GetActorNation(actor);
        if (nation != null)
        {
            float prestigeGain = 2f + (float)(SimRng.NextDouble() * 3);
            nation.Prestige = MathF.Min(100f, nation.Prestige + prestigeGain);
            Notify($"Intel review complete: Prestige +{prestigeGain:0.0}%", "info");
        }
    }

    // ─── Rival Actions ───────────────────────────────

    private void ProcessInvestigate(CharacterData actor, CharacterData target)
    {
        var nation = GetActorNation(actor);
        float chance = nation != null ? nation.Prestige / 100f : 0.3f;
        bool success = SimRng.NextDouble() < chance;

        if (success)
        {
            Notify($"Investigation on {target.Name} successful! Intel gathered.", "success");
        }
        else
        {
            Notify($"Investigation on {target.Name} turned up nothing.", "warning");
        }
    }

    private void ProcessBribe(CharacterData actor, CharacterData target)
    {
        var nation = GetActorNation(actor);
        if (nation != null && nation.Treasury < 50f) { Notify("Insufficient Funds! Requires $50M.", "danger"); return; }
        if (nation != null) nation.Treasury -= 50f;

        var targetNation = GetActorNation(target);
        float chance = nation != null ? nation.Prestige / 100f * 0.6f : 0.2f;
        bool success = SimRng.NextDouble() < chance;

        if (success)
        {
            if (targetNation != null)
            {
                float stabilityLoss = 4f + (float)(SimRng.NextDouble() * 4);
                targetNation.Stability = MathF.Max(0f, targetNation.Stability - stabilityLoss);
                Notify($"Bribed {target.Name}'s loyalists! Their stability -{stabilityLoss:0.0}%", "success");
            }
        }
        else
        {
            if (nation != null)
            {
                nation.Prestige = MathF.Max(0f, nation.Prestige - 5f);
                Notify($"Bribe attempt on {target.Name} FAILED! They know. Prestige -5%", "danger");
            }
        }
    }

    private void ProcessThreaten(CharacterData actor, CharacterData target)
    {
        var nation = GetActorNation(actor);
        if (nation != null && nation.Treasury < 10f) { Notify("Insufficient Funds! Requires $10M.", "danger"); return; }
        if (nation != null) nation.Treasury -= 10f;

        var targetNation = GetActorNation(target);
        float chance = nation != null ? nation.Prestige / 100f * 0.7f : 0.2f;
        bool success = SimRng.NextDouble() < chance;

        if (success)
        {
            if (targetNation != null)
            {
                float stabilityLoss = 6f;
                targetNation.Stability = MathF.Max(0f, targetNation.Stability - stabilityLoss);
            }
            if (nation != null)
            {
                nation.Prestige = MathF.Min(100f, nation.Prestige + 5f);
            }
            Notify($"Threatened {target.Name}! Their stability drops. Prestige +5%", "success");
        }
        else
        {
            if (nation != null)
            {
                nation.Prestige = MathF.Max(0f, nation.Prestige - 3f);
            }
            Notify($"{target.Name} called your bluff! Prestige -3%", "danger");
        }
    }

    private void ProcessEliminate(CharacterData actor, CharacterData target)
    {
        var nation = GetActorNation(actor);
        if (nation != null && nation.Treasury < 300f) { Notify("Insufficient Funds! Requires $300M.", "danger"); return; }
        if (nation != null) nation.Treasury -= 300f;

        float chance = nation != null ? (nation.Prestige - 30f) / 100f : 0.05f;
        if (chance < 0.05f) chance = 0.05f;

        bool success = SimRng.NextDouble() < chance;

        if (success)
        {
            var targetNation = GetActorNation(target);
            if (targetNation != null)
            {
                targetNation.Stability = MathF.Max(0f, targetNation.Stability - 25f);
            }
            target.Role = "Eliminated";
            Notify($"{target.Name} has been... removed. Their nation destabilized.", "success");
        }
        else
        {
            if (nation != null)
            {
                nation.Prestige = MathF.Max(0f, nation.Prestige - 15f);
                nation.Stability = MathF.Max(0f, nation.Stability - 5f);
            }
            Notify($"ASSASSINATION FAILED! {target.Name} survives and knows it was you! Prestige -15%", "danger");
        }
    }

    // ─── Helpers ──────────────────────────────────────

    private void Notify(string msg, string type)
    {
        GD.Print($"[PoliticalEngine] {msg}");
        EventBus.Instance?.Publish(new NotificationEvent(msg, type));
    }

    private static NationData? GetActorNation(CharacterData actor)
    {
        var world = WorldStateManager.Instance?.Data;
        if (world == null) return null;
        var parts = actor.NationId.Split('_');
        if (parts.Length < 2 || !int.TryParse(parts[1], out int idx)) return null;
        return idx >= 0 && idx < world.Nations.Count ? world.Nations[idx] : null;
    }
}
