using System;
using System.Linq;
using Godot;
using Warship.Core;
using Warship.Data;
using Warship.Events;

namespace Warship.Engines;

/// <summary>
/// Processes CouncilActionEvents and applies them to game state.
/// Each action has costs, requirements, and consequences.
/// Adviser opinions are generated before resolution.
/// </summary>
public partial class CouncilEngine : Node
{
    private Random _rng = new(777);

    public override void _Ready()
    {
        EventBus.Instance!.Subscribe<CouncilActionEvent>(OnCouncilAction);
        GD.Print("[CouncilEngine] Online. The council awaits orders.");
    }

    private void OnCouncilAction(CouncilActionEvent ev)
    {
        var world = WorldStateManager.Instance?.Data;
        if (world == null) return;

        int nIdx = int.Parse(ev.NationId.Split('_')[1]);
        if (nIdx >= world.Nations.Count) return;
        var nation = world.Nations[nIdx];
        var council = nation.Council;

        // Generate adviser opinions first
        foreach (var adviser in council.Advisers)
        {
            bool approves = EvaluateAdviserOpinion(adviser, ev.Category, ev.ActionId, nation);
            string advice = GenerateAdvice(adviser, ev.ActionId, approves);
            adviser.CurrentAdvice = advice;
            adviser.ApprovesCurrentProposal = approves;

            EventBus.Instance?.Publish(new AdviserOpinionEvent(
                adviser.Id, ev.ActionId, approves, advice));
        }

        // Process the action
        switch (ev.Category)
        {
            case CouncilActionCategory.Domestic:
                ProcessDomestic(nation, ev.ActionId);
                break;
            case CouncilActionCategory.Military:
                ProcessMilitary(nation, ev.ActionId, world);
                break;
            case CouncilActionCategory.Diplomatic:
                ProcessDiplomatic(nation, ev.ActionId, world);
                break;
            case CouncilActionCategory.Intelligence:
                ProcessIntelligence(nation, ev.ActionId, world);
                break;
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  DOMESTIC ACTIONS
    // ═══════════════════════════════════════════════════════════════

    private void ProcessDomestic(NationData nation, string actionId)
    {
        switch (actionId)
        {
            case "adjust_tax_rate":
                // Cycle tax rate: low -> medium -> high -> punitive -> low
                float oldRate = nation.Council.TaxRate;
                nation.Council.TaxRate = oldRate switch
                {
                    < 0.15f => 0.20f,   // low -> medium
                    < 0.25f => 0.35f,   // medium -> high
                    < 0.40f => 0.50f,   // high -> punitive
                    _ => 0.10f,         // punitive -> low
                };
                float newRate = nation.Council.TaxRate;
                string level = newRate switch { < 0.15f => "LOW", < 0.25f => "MEDIUM", < 0.40f => "HIGH", _ => "PUNITIVE" };

                // High taxes hurt stability, low taxes help it
                float stabilityDelta = (0.25f - newRate) * 20f; // +3 at low, -5 at punitive
                nation.Stability = Math.Clamp(nation.Stability + stabilityDelta, 0, 100);

                Notify(nation, $"Tax rate set to {level} ({newRate * 100:0}%). Stability {(stabilityDelta >= 0 ? "+" : "")}{stabilityDelta:0.0}%",
                    stabilityDelta >= 0 ? "info" : "warning");
                break;

            case "fund_infrastructure":
                if (!SpendTreasury(nation, 200f, "infrastructure")) return;
                nation.Stability = Math.Clamp(nation.Stability + 5f, 0, 100);
                nation.Food = Math.Clamp(nation.Food + 8f, 0, 100);
                Notify(nation, "Infrastructure funded: Stability +5, Food +8. Cost: $200M", "success");
                break;

            case "declare_martial_law":
                if (nation.Council.MartialLawActive)
                {
                    nation.Council.MartialLawActive = false;
                    nation.Stability = Math.Clamp(nation.Stability - 10f, 0, 100);
                    nation.Prestige = Math.Clamp(nation.Prestige + 5f, 0, 100);
                    Notify(nation, "Martial law LIFTED. Stability -10 (unrest), Prestige +5", "info");
                }
                else
                {
                    nation.Council.MartialLawActive = true;
                    nation.Stability = Math.Clamp(nation.Stability + 15f, 0, 100);
                    nation.Prestige = Math.Clamp(nation.Prestige - 10f, 0, 100);
                    Notify(nation, "MARTIAL LAW declared! Stability +15, Prestige -10", "warning");
                }
                break;

            case "hold_elections":
            case "hold_feast":
            case "revolutionary_rally":
            case "rally_warriors":
                if (!SpendTreasury(nation, 50f, "public event")) return;
                nation.Stability = Math.Clamp(nation.Stability + 8f, 0, 100);
                nation.Prestige = Math.Clamp(nation.Prestige + 3f, 0, 100);
                Notify(nation, "Public event held: Stability +8, Prestige +3. Cost: $50M", "success");
                break;

            case "suppress_dissent":
            case "purge_dissidents":
            case "disappear_dissident":
            case "exile_traitor":
                if (!SpendTreasury(nation, 100f, "suppression")) return;
                nation.Stability = Math.Clamp(nation.Stability + 12f, 0, 100);
                nation.Prestige = Math.Clamp(nation.Prestige - 8f, 0, 100);
                nation.WarWeariness = Math.Clamp(nation.WarWeariness - 5f, 0, 100);
                Notify(nation, "Dissent suppressed: Stability +12, Prestige -8. The people fear you.", "warning");
                break;

            case "deregulate_markets":
            case "corporate_subsidy":
            case "issue_bonds":
                if (!SpendTreasury(nation, 150f, "economic policy")) return;
                nation.Electronics = Math.Clamp(nation.Electronics + 10f, 0, 100);
                nation.Treasury += 100f; // Net cost is only 50
                Notify(nation, "Economic stimulus: Electronics +10, Treasury +$100M (net cost $50M)", "success");
                break;

            case "ennoble_loyalist":
                if (!SpendTreasury(nation, 80f, "ennoblement")) return;
                // Boost a random adviser's loyalty
                var advisers = nation.Council.Advisers;
                if (advisers.Count > 0)
                {
                    var target = advisers[_rng.Next(advisers.Count)];
                    target.Loyalty = Math.Clamp(target.Loyalty + 0.15f, 0, 1);
                    Notify(nation, $"Ennobled {target.Name}: Loyalty +15%. Cost: $80M", "success");
                }
                break;

            case "install_surveillance":
            case "blackmail_official":
                if (!SpendTreasury(nation, 120f, "covert domestic op")) return;
                nation.Stability = Math.Clamp(nation.Stability + 6f, 0, 100);
                Notify(nation, "Surveillance expanded: Stability +6. Cost: $120M", "info");
                break;

            case "fortify_settlement":
                if (!SpendTreasury(nation, 250f, "fortification")) return;
                nation.Iron = Math.Clamp(nation.Iron - 10f, 0, 100);
                Notify(nation, "Settlement fortified: Iron -10. Cost: $250M. Defenses improved.", "success");
                break;

            case "declare_blood_oath":
                nation.WarWeariness = Math.Clamp(nation.WarWeariness - 20f, 0, 100);
                nation.Stability = Math.Clamp(nation.Stability + 5f, 0, 100);
                Notify(nation, "Blood oath declared! War Weariness -20, Stability +5", "success");
                break;

            case "production_quota":
                nation.Manpower = Math.Clamp(nation.Manpower + 12f, 0, 100);
                nation.Iron = Math.Clamp(nation.Iron + 8f, 0, 100);
                nation.Stability = Math.Clamp(nation.Stability - 3f, 0, 100);
                Notify(nation, "Production quotas raised: Manpower +12, Iron +8, Stability -3", "info");
                break;

            case "ration_supplies":
                nation.Food = Math.Clamp(nation.Food + 15f, 0, 100);
                nation.Stability = Math.Clamp(nation.Stability - 5f, 0, 100);
                Notify(nation, "Rationing imposed: Food +15, Stability -5", "warning");
                break;

            default:
                Notify(nation, $"Council acknowledged: {actionId}", "info");
                break;
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  MILITARY ACTIONS
    // ═══════════════════════════════════════════════════════════════

    private void ProcessMilitary(NationData nation, string actionId, WorldData world)
    {
        switch (actionId)
        {
            case "set_defense_budget":
                // Cycle: low (20%) -> medium (35%) -> high (50%) -> war footing (70%) -> low
                float oldBudget = nation.Council.DefenseBudgetPct;
                nation.Council.DefenseBudgetPct = oldBudget switch
                {
                    < 0.25f => 0.35f,
                    < 0.40f => 0.50f,
                    < 0.55f => 0.70f,
                    _ => 0.20f,
                };
                string budgetLevel = nation.Council.DefenseBudgetPct switch
                {
                    < 0.25f => "LOW", < 0.40f => "MEDIUM", < 0.55f => "HIGH", _ => "WAR FOOTING"
                };
                Notify(nation, $"Defense budget: {budgetLevel} ({nation.Council.DefenseBudgetPct * 100:0}% of income)", "info");
                break;

            case "authorize_operation":
            case "authorize_naval_operation":
                if (!SpendTreasury(nation, 300f, "military operation")) return;
                nation.Oil = Math.Clamp(nation.Oil - 10f, 0, 100);

                // Boost all armies' organization
                foreach (var army in world.Armies.Where(a => a.NationId == nation.Id && a.IsAlive))
                {
                    army.Organization = Math.Clamp(army.Organization + 15f, 0, 100);
                    army.Supply = Math.Clamp(army.Supply + 10f, 0, 100);
                }
                Notify(nation, "Operation authorized: All armies +15 Org, +10 Supply. Oil -10. Cost: $300M", "success");
                break;

            case "approve_conscription":
                if (nation.Council.ConscriptionActive)
                {
                    nation.Council.ConscriptionActive = false;
                    nation.Stability = Math.Clamp(nation.Stability + 5f, 0, 100);
                    Notify(nation, "Conscription ended. Stability +5", "info");
                }
                else
                {
                    nation.Council.ConscriptionActive = true;
                    nation.Manpower = Math.Clamp(nation.Manpower + 20f, 0, 100);
                    nation.Stability = Math.Clamp(nation.Stability - 8f, 0, 100);
                    nation.WarWeariness = Math.Clamp(nation.WarWeariness + 5f, 0, 100);
                    Notify(nation, "CONSCRIPTION activated! Manpower +20, Stability -8, War Weariness +5", "warning");
                }
                break;

            case "nuclear_authorization":
                if (nation.Council.NuclearAuthGranted)
                {
                    nation.Council.NuclearAuthGranted = false;
                    Notify(nation, "Nuclear authorization REVOKED. Stand down.", "info");
                }
                else
                {
                    if (nation.Uranium < 10f)
                    {
                        Notify(nation, "Insufficient uranium for nuclear authorization!", "danger");
                        return;
                    }
                    nation.Council.NuclearAuthGranted = true;
                    nation.Prestige = Math.Clamp(nation.Prestige - 15f, 0, 100);
                    Notify(nation, "NUCLEAR LAUNCH AUTHORIZED. God help us all. Prestige -15", "danger");
                }
                break;

            case "mobilize_reserves":
                if (!SpendTreasury(nation, 200f, "mobilization")) return;
                nation.Manpower = Math.Clamp(nation.Manpower + 15f, 0, 100);
                foreach (var army in world.Armies.Where(a => a.NationId == nation.Id && a.IsAlive))
                {
                    army.Morale = Math.Clamp(army.Morale + 10f, 0, 100);
                }
                Notify(nation, "Reserves mobilized: Manpower +15, all armies Morale +10. Cost: $200M", "success");
                break;

            case "commission_warship":
            case "blockade_order":
                if (!SpendTreasury(nation, 400f, "naval commission")) return;
                nation.Iron = Math.Clamp(nation.Iron - 15f, 0, 100);
                nation.Electronics = Math.Clamp(nation.Electronics - 10f, 0, 100);
                Notify(nation, "Naval order issued: Iron -15, Electronics -10. Cost: $400M", "success");
                break;

            default:
                Notify(nation, $"Military command acknowledged: {actionId}", "info");
                break;
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  DIPLOMATIC ACTIONS
    // ═══════════════════════════════════════════════════════════════

    private void ProcessDiplomatic(NationData nation, string actionId, WorldData world)
    {
        // For now, diplomatic actions target a random non-player nation
        // TODO: target selection UI
        var targetNation = world.Nations.FirstOrDefault(n => n.Id != nation.Id && n.IsAlive);
        if (targetNation == null) return;

        switch (actionId)
        {
            case "propose_treaty":
                if (!SpendTreasury(nation, 100f, "treaty proposal")) return;
                var currentRelation = nation.Relations.GetValueOrDefault(targetNation.Id, DiplomaticStatus.Neutral);
                if (currentRelation < DiplomaticStatus.Neutral)
                {
                    // Improve by one step
                    var improved = currentRelation - 1;
                    nation.Relations[targetNation.Id] = improved;
                    targetNation.Relations[nation.Id] = improved;
                    Notify(nation, $"Treaty proposed to {targetNation.Name}: Relations improved to {improved}. Cost: $100M", "success");
                }
                else
                {
                    Notify(nation, $"Relations with {targetNation.Name} already {currentRelation}. No improvement possible.", "info");
                }
                break;

            case "declare_war":
                nation.Relations[targetNation.Id] = DiplomaticStatus.AtWar;
                targetNation.Relations[nation.Id] = DiplomaticStatus.AtWar;
                nation.Prestige = Math.Clamp(nation.Prestige - 5f, 0, 100);
                nation.WarWeariness = Math.Clamp(nation.WarWeariness + 10f, 0, 100);
                Notify(nation, $"WAR DECLARED on {targetNation.Name}! Prestige -5, War Weariness +10", "danger");
                break;

            case "impose_sanctions":
                nation.Relations[targetNation.Id] = DiplomaticStatus.Hostile;
                targetNation.Relations[nation.Id] = DiplomaticStatus.Hostile;
                Notify(nation, $"Sanctions imposed on {targetNation.Name}. Relations: HOSTILE", "warning");
                break;

            case "request_aid":
                var relation = nation.Relations.GetValueOrDefault(targetNation.Id, DiplomaticStatus.Neutral);
                if (relation <= DiplomaticStatus.Friendly)
                {
                    float aid = 100f + (float)_rng.NextDouble() * 200f;
                    nation.Treasury += aid;
                    Notify(nation, $"{targetNation.Name} grants ${aid:0}M in aid!", "success");
                }
                else
                {
                    Notify(nation, $"{targetNation.Name} refuses our aid request. Relations too poor.", "warning");
                }
                break;

            case "send_envoy":
                if (!SpendTreasury(nation, 30f, "envoy")) return;
                nation.Prestige = Math.Clamp(nation.Prestige + 3f, 0, 100);
                Notify(nation, $"Envoy sent to {targetNation.Name}. Prestige +3. Cost: $30M", "info");
                break;

            case "recall_ambassador":
                var rel = nation.Relations.GetValueOrDefault(targetNation.Id, DiplomaticStatus.Neutral);
                if (rel < DiplomaticStatus.Hostile)
                {
                    nation.Relations[targetNation.Id] = (DiplomaticStatus)Math.Min((int)rel + 1, (int)DiplomaticStatus.Hostile);
                    Notify(nation, $"Ambassador recalled from {targetNation.Name}. Relations worsened.", "warning");
                }
                break;

            default:
                Notify(nation, $"Diplomatic action acknowledged: {actionId}", "info");
                break;
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  INTELLIGENCE ACTIONS
    // ═══════════════════════════════════════════════════════════════

    private void ProcessIntelligence(NationData nation, string actionId, WorldData world)
    {
        var targetNation = world.Nations.FirstOrDefault(n => n.Id != nation.Id && n.IsAlive);
        if (targetNation == null) return;

        // Intelligence success is modified by adviser competence
        var intelAdviser = nation.Council.Advisers.FirstOrDefault(a => a.Role == AdviserRole.Intelligence);
        float competenceBonus = intelAdviser?.Competence ?? 0.5f;

        switch (actionId)
        {
            case "deploy_spy":
            case "deploy_agent":
                if (!SpendTreasury(nation, 150f, "spy deployment")) return;
                float spyChance = 0.4f + competenceBonus * 0.3f;
                if (_rng.NextDouble() < spyChance)
                {
                    // Success: reveal some info about target
                    Notify(nation, $"Spy planted in {targetNation.Name}! Intel: Treasury ~${targetNation.Treasury:0}M, Stability ~{targetNation.Stability:0}%", "success");
                }
                else
                {
                    nation.Prestige = Math.Clamp(nation.Prestige - 5f, 0, 100);
                    Notify(nation, $"Spy caught by {targetNation.Name}! Prestige -5", "danger");
                }
                break;

            case "counter-intelligence":
            case "counter_intelligence":
                if (!SpendTreasury(nation, 100f, "counter-intel")) return;
                nation.Stability = Math.Clamp(nation.Stability + 3f, 0, 100);
                Notify(nation, "Counter-intelligence sweep complete. Stability +3. Cost: $100M", "success");
                break;

            case "approve_assassination":
                if (!SpendTreasury(nation, 500f, "assassination")) return;
                float assassinChance = 0.15f + competenceBonus * 0.25f;
                if (_rng.NextDouble() < assassinChance)
                {
                    // Find a target character
                    var targetChar = world.Characters.FirstOrDefault(c =>
                        c.NationId == targetNation.Id && c.Role != "Eliminated");
                    if (targetChar != null)
                    {
                        targetChar.Role = "Eliminated";
                        targetChar.TerritoryAuthority = 0;
                        targetChar.WorldAuthority = 0;
                        targetChar.BehindTheScenesAuthority = 0;
                        targetNation.Stability = Math.Clamp(targetNation.Stability - 20f, 0, 100);
                        Notify(nation, $"ASSASSINATION successful! {targetChar.Name} of {targetNation.Name} eliminated. Their stability -20", "success");
                    }
                }
                else
                {
                    nation.Prestige = Math.Clamp(nation.Prestige - 20f, 0, 100);
                    nation.Relations[targetNation.Id] = DiplomaticStatus.Hostile;
                    targetNation.Relations[nation.Id] = DiplomaticStatus.Hostile;
                    Notify(nation, $"Assassination FAILED! {targetNation.Name} furious. Prestige -20, Relations: HOSTILE", "danger");
                }
                break;

            case "sabotage_mission":
                if (!SpendTreasury(nation, 200f, "sabotage")) return;
                float sabotageChance = 0.3f + competenceBonus * 0.3f;
                if (_rng.NextDouble() < sabotageChance)
                {
                    float damage = 100f + (float)_rng.NextDouble() * 200f;
                    targetNation.Treasury = Math.Max(0, targetNation.Treasury - damage);
                    targetNation.Iron = Math.Clamp(targetNation.Iron - 10f, 0, 100);
                    Notify(nation, $"Sabotage in {targetNation.Name}: Their treasury -${damage:0}M, Iron -10", "success");
                }
                else
                {
                    Notify(nation, $"Sabotage attempt in {targetNation.Name} failed. Operatives compromised.", "danger");
                }
                break;

            case "steal_technology":
                if (!SpendTreasury(nation, 250f, "tech theft")) return;
                float techChance = 0.25f + competenceBonus * 0.3f;
                if (_rng.NextDouble() < techChance)
                {
                    nation.Electronics = Math.Clamp(nation.Electronics + 15f, 0, 100);
                    Notify(nation, $"Technology stolen from {targetNation.Name}! Electronics +15", "success");
                }
                else
                {
                    Notify(nation, "Tech theft failed. No intelligence gained.", "warning");
                }
                break;

            case "deep_cover_operation":
            case "fabricate_evidence":
            case "double_agent":
                if (!SpendTreasury(nation, 300f, "deep op")) return;
                float deepChance = 0.2f + competenceBonus * 0.4f;
                if (_rng.NextDouble() < deepChance)
                {
                    targetNation.Stability = Math.Clamp(targetNation.Stability - 12f, 0, 100);
                    Notify(nation, $"Deep operation success in {targetNation.Name}! Their stability -12", "success");
                }
                else
                {
                    nation.Prestige = Math.Clamp(nation.Prestige - 10f, 0, 100);
                    Notify(nation, "Deep operation failed. Evidence trail leads back to us. Prestige -10", "danger");
                }
                break;

            default:
                Notify(nation, $"Intelligence action acknowledged: {actionId}", "info");
                break;
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  ADVISER OPINION GENERATION
    // ═══════════════════════════════════════════════════════════════

    private bool EvaluateAdviserOpinion(AdviserData adviser, CouncilActionCategory category, string actionId, NationData nation)
    {
        // Adviser opinion depends on their role, hawkishness, and the action type
        float approval = 0.5f; // baseline neutral

        // Military advisers like military actions, economic advisers like economic ones, etc.
        bool isOwnDomain = (adviser.Role == AdviserRole.Military && category == CouncilActionCategory.Military)
            || (adviser.Role == AdviserRole.Economic && category == CouncilActionCategory.Domestic)
            || (adviser.Role == AdviserRole.Intelligence && category == CouncilActionCategory.Intelligence)
            || (adviser.Role == AdviserRole.Diplomatic && category == CouncilActionCategory.Diplomatic);

        if (isOwnDomain) approval += 0.2f; // They like actions in their domain

        // Hawks like aggressive actions
        bool isAggressive = actionId.Contains("war") || actionId.Contains("attack") || actionId.Contains("sanction")
            || actionId.Contains("assassin") || actionId.Contains("purge") || actionId.Contains("martial")
            || actionId.Contains("nuclear") || actionId.Contains("conscription") || actionId.Contains("sabotage");
        if (isAggressive)
            approval += (adviser.Hawkishness - 0.5f) * 0.4f; // Hawks approve, doves disapprove

        // Expensive actions are disapproved by economic advisers
        bool isExpensive = actionId.Contains("infrastructure") || actionId.Contains("commission")
            || actionId.Contains("operation") || actionId.Contains("technology");
        if (isExpensive && adviser.Role == AdviserRole.Economic && nation.Treasury < 500f)
            approval -= 0.3f;

        // Low stability makes advisers nervous about risky actions
        if (nation.Stability < 30f && isAggressive)
            approval -= 0.2f;

        // Loyalty affects honesty (disloyal advisers may lie)
        if (adviser.Loyalty < 0.4f)
            approval = 1.0f - approval; // Traitors recommend the opposite

        // Add some randomness
        approval += (float)(_rng.NextDouble() - 0.5) * 0.2f;

        return approval > 0.5f;
    }

    private string GenerateAdvice(AdviserData adviser, string actionId, bool approves)
    {
        if (approves)
        {
            return adviser.Role switch
            {
                AdviserRole.Military => "The military stands ready. I recommend we proceed.",
                AdviserRole.Economic => "The numbers support this. I approve.",
                AdviserRole.Intelligence => "Our sources confirm this is viable.",
                AdviserRole.Diplomatic => "This will serve our international position well.",
                _ => "I support this course of action.",
            };
        }
        else
        {
            return adviser.Role switch
            {
                AdviserRole.Military => "This is reckless. Our forces aren't prepared.",
                AdviserRole.Economic => "We can't afford this. The treasury won't survive.",
                AdviserRole.Intelligence => "Our intel suggests this will backfire.",
                AdviserRole.Diplomatic => "This will damage our standing with other nations.",
                _ => "I advise caution. This is unwise.",
            };
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  HELPERS
    // ═══════════════════════════════════════════════════════════════

    private bool SpendTreasury(NationData nation, float amount, string purpose)
    {
        if (nation.Treasury < amount)
        {
            Notify(nation, $"Insufficient funds for {purpose}! Need ${amount:0}M, have ${nation.Treasury:0}M", "danger");
            return false;
        }
        nation.Treasury -= amount;
        return true;
    }

    private void Notify(NationData nation, string msg, string type)
    {
        if (nation.IsPlayer)
        {
            GD.Print($"[Council] {msg}");
            EventBus.Instance?.Publish(new NotificationEvent(msg, type));
        }
    }
}
