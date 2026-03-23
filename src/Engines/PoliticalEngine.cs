using System;
using System.Linq;
using Godot;
using Warship.Core;
using Warship.Data;
using Warship.Events;

namespace Warship.Engines;

/// <summary>
/// Processes all political actions: bribe, threaten, eliminate, fund militia, etc.
/// Uses dice rolls with BSA-weighted success rates.
/// Routes all results back through EventBus as AuthorityChangedEvent / NotificationEvent.
/// </summary>
public partial class PoliticalEngine : Node
{
    private Random _rng = new(42);

    public override void _Ready()
    {
        EventBus.Instance!.Subscribe<PoliticalActionEvent>(OnPoliticalAction);
        GD.Print("[PoliticalEngine] Online. Listening for political actions.");
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
            case "public_address":
                ProcessPublicAddress(actor);
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
        var nation = WorldStateManager.Instance?.Data?.Nations[int.Parse(actor.NationId.Split('_')[1])];
        if (nation != null && nation.Treasury < 100f) { Notify("❌ Insufficient Funds! Requires $100M.", "danger"); return; }
        if (nation != null) nation.Treasury -= 100f;

        float gain = 5f + (float)(_rng.NextDouble() * 5);
        float old = actor.TerritoryAuthority;
        actor.TerritoryAuthority = MathF.Min(100f, actor.TerritoryAuthority + gain);

        // Small WA cost (looks militaristic to the world)
        float waLoss = 1f + (float)(_rng.NextDouble() * 2);
        actor.WorldAuthority = MathF.Max(0f, actor.WorldAuthority - waLoss);

        Notify($"💰 Funded domestic militia: TA +{gain:0.0}%, WA -{waLoss:0.0}%", "success");
        EmitChange(actor.Id, "TA", old, actor.TerritoryAuthority, "Funded militia");
    }

    private void ProcessPublicAddress(CharacterData actor)
    {
        float gain = 4f + (float)(_rng.NextDouble() * 4);
        float old = actor.WorldAuthority;
        actor.WorldAuthority = MathF.Min(100f, actor.WorldAuthority + gain);

        Notify($"🎙 Public address broadcast: WA +{gain:0.0}%", "success");
        EmitChange(actor.Id, "WA", old, actor.WorldAuthority, "Public address");
    }

    private void ProcessReviewIntel(CharacterData actor)
    {
        float gain = 3f + (float)(_rng.NextDouble() * 3);
        float old = actor.BehindTheScenesAuthority;
        actor.BehindTheScenesAuthority = MathF.Min(100f, actor.BehindTheScenesAuthority + gain);

        // Fog of War: broad intel boost on all rivals
        var world = WorldStateManager.Instance?.Data;
        if (world != null)
            IntelligenceEngine.GrantReviewIntelBonus(world, actor.NationId);

        Notify($"📋 Intel review complete: BSA +{gain:0.0}%", "info");
        EmitChange(actor.Id, "BSA", old, actor.BehindTheScenesAuthority, "Intel review");
    }

    // ─── Rival Actions ───────────────────────────────

    private void ProcessInvestigate(CharacterData actor, CharacterData target)
    {
        // Success based on actor's BSA
        float chance = actor.BehindTheScenesAuthority / 100f;
        bool success = _rng.NextDouble() < chance;

        if (success)
        {
            float bsaGain = 3f;
            actor.BehindTheScenesAuthority = MathF.Min(100f, actor.BehindTheScenesAuthority + bsaGain);

            // Fog of War: grant targeted intel points
            var world = WorldStateManager.Instance?.Data;
            if (world != null)
                IntelligenceEngine.GrantInvestigateBonus(world, actor.NationId, target.NationId);

            Notify($"🔍 Investigation on {target.Name} successful! BSA +{bsaGain:0.0}%, intel gained.", "success");
        }
        else
        {
            Notify($"🔍 Investigation on {target.Name} turned up nothing.", "warning");
        }
    }

    private void ProcessBribe(CharacterData actor, CharacterData target)
    {
        var nation = WorldStateManager.Instance?.Data?.Nations[int.Parse(actor.NationId.Split('_')[1])];
        if (nation != null && nation.Treasury < 50f) { Notify("❌ Insufficient Funds! Requires $50M.", "danger"); return; }
        if (nation != null) nation.Treasury -= 50f;

        float chance = (actor.BehindTheScenesAuthority * 0.6f + actor.TerritoryAuthority * 0.4f) / 100f;
        bool success = _rng.NextDouble() < chance;

        if (success)
        {
            // Steal some of their TA
            float stolen = 4f + (float)(_rng.NextDouble() * 4);
            target.TerritoryAuthority = MathF.Max(0f, target.TerritoryAuthority - stolen);
            actor.TerritoryAuthority = MathF.Min(100f, actor.TerritoryAuthority + stolen * 0.5f);
            actor.BehindTheScenesAuthority = MathF.Min(100f, actor.BehindTheScenesAuthority + 2f);

            Notify($"💵 Bribed {target.Name}'s loyalists! TA +{stolen * 0.5f:0.0}%, their TA -{stolen:0.0}%", "success");
        }
        else
        {
            // Backfire: they find out
            actor.BehindTheScenesAuthority = MathF.Max(0f, actor.BehindTheScenesAuthority - 5f);
            Notify($"💵 Bribe attempt on {target.Name} FAILED! They know. BSA -5%", "danger");
        }
    }

    private void ProcessThreaten(CharacterData actor, CharacterData target)
    {
        var nation = WorldStateManager.Instance?.Data?.Nations[int.Parse(actor.NationId.Split('_')[1])];
        if (nation != null && nation.Treasury < 10f) { Notify("❌ Insufficient Funds! Requires $10M.", "danger"); return; }
        if (nation != null) nation.Treasury -= 10f;

        // Requires high TA to be credible
        float chance = actor.TerritoryAuthority / 100f * 0.7f;
        bool success = _rng.NextDouble() < chance;

        if (success)
        {
            float waGain = 5f;
            float targetWaLoss = 6f;
            actor.WorldAuthority = MathF.Min(100f, actor.WorldAuthority + waGain);
            target.WorldAuthority = MathF.Max(0f, target.WorldAuthority - targetWaLoss);

            Notify($"⚠️ Threatened {target.Name}! WA +{waGain:0.0}%, their WA -{targetWaLoss:0.0}%", "success");
        }
        else
        {
            actor.WorldAuthority = MathF.Max(0f, actor.WorldAuthority - 3f);
            actor.TerritoryAuthority = MathF.Max(0f, actor.TerritoryAuthority - 2f);
            Notify($"⚠️ {target.Name} called your bluff! WA -3%, TA -2%", "danger");
        }
    }

    private void ProcessEliminate(CharacterData actor, CharacterData target)
    {
        var nation = WorldStateManager.Instance?.Data?.Nations[int.Parse(actor.NationId.Split('_')[1])];
        if (nation != null && nation.Treasury < 300f) { Notify("❌ Insufficient Funds! Requires $300M.", "danger"); return; }
        if (nation != null) nation.Treasury -= 300f;

        // Highest risk, highest reward. Needs strong BSA.
        float chance = (actor.BehindTheScenesAuthority - 30f) / 100f; // Need BSA > 30 to even attempt
        if (chance < 0.05f) chance = 0.05f; // Always 5% minimum

        bool success = _rng.NextDouble() < chance;

        if (success)
        {
            // Absorb their authority
            actor.TerritoryAuthority = MathF.Min(100f, actor.TerritoryAuthority + target.TerritoryAuthority * 0.3f);
            actor.WorldAuthority = MathF.Min(100f, actor.WorldAuthority + target.WorldAuthority * 0.2f);
            actor.BehindTheScenesAuthority = MathF.Min(100f, actor.BehindTheScenesAuthority + 10f);

            // Mark target as eliminated
            target.TerritoryAuthority = 0;
            target.WorldAuthority = 0;
            target.BehindTheScenesAuthority = 0;
            target.Role = "Eliminated";

            Notify($"🗡 {target.Name} has been... removed. You absorb their power.", "success");
        }
        else
        {
            // Catastrophic backfire
            actor.BehindTheScenesAuthority = MathF.Max(0f, actor.BehindTheScenesAuthority - 15f);
            actor.WorldAuthority = MathF.Max(0f, actor.WorldAuthority - 10f);
            target.BehindTheScenesAuthority = MathF.Min(100f, target.BehindTheScenesAuthority + 10f);

            Notify($"🗡 ASSASSINATION FAILED! {target.Name} survives and knows it was you! BSA -15%, WA -10%", "danger");
        }
    }

    // ─── Helpers ──────────────────────────────────────

    private void Notify(string msg, string type)
    {
        GD.Print($"[PoliticalEngine] {msg}");
        EventBus.Instance?.Publish(new NotificationEvent(msg, type));
    }

    private void EmitChange(string charId, string meter, float old, float newVal, string reason)
    {
        EventBus.Instance?.Publish(new AuthorityChangedEvent(charId, meter, old, newVal, reason));
    }
}
