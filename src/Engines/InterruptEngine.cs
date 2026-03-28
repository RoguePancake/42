using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using Warship.Core;
using Warship.Data;
using Warship.Events;

namespace Warship.Engines;

/// <summary>
/// "The Phone Rings" — evaluates world state each tick and fires timed interrupts.
/// Replaces CrisisEngine. Each interrupt has a countdown timer, choices, and a
/// default outcome that fires on timeout.
/// </summary>
public partial class InterruptEngine : Node
{
    private int _lastCheckTick;
    private const int MinTicksBetweenInterrupts = 8;  // Don't spam — at least 8 ticks apart
    private int _ticksSinceLastInterrupt;
    private readonly HashSet<string> _firedOneTimeIds = new();

    public override void _Ready()
    {
        EventBus.Instance!.Subscribe<TurnAdvancedEvent>(OnTick);
        EventBus.Instance!.Subscribe<InterruptResolvedEvent>(OnResolved);
        GD.Print("[InterruptEngine] Online. The phone will ring.");
    }

    private void OnTick(TurnAdvancedEvent ev)
    {
        _ticksSinceLastInterrupt++;

        if (_ticksSinceLastInterrupt < MinTicksBetweenInterrupts) return;

        var world = WorldStateManager.Instance?.Data;
        if (world == null) return;

        // Evaluate trigger conditions against world state
        var interrupt = EvaluateTriggers(world, ev.Turn);
        if (interrupt != null)
        {
            _ticksSinceLastInterrupt = 0;
            FireInterrupt(interrupt);
        }
    }

    private InterruptData? EvaluateTriggers(WorldData world, int tick)
    {
        var playerNation = GetPlayerNation(world);
        var playerChar = world.Characters.FirstOrDefault(c => c.IsPlayer);
        if (playerNation == null || playerChar == null) return null;

        // Check each trigger condition. First match wins per tick.

        // CRITICAL: Border skirmish escalating (enemy army near player territory)
        if (!_firedOneTimeIds.Contains("border_clash") && tick > 15)
        {
            var enemyNear = world.Armies.FirstOrDefault(a =>
                a.NationId != world.PlayerNationId && a.IsAlive &&
                TileDistance(a.TileX, a.TileY, playerNation.CapitalX, playerNation.CapitalY) < 40);

            if (enemyNear != null)
            {
                _firedOneTimeIds.Add("border_clash");
                var enemyNation = world.Nations.FirstOrDefault(n => n.Id == enemyNear.NationId);
                return new InterruptData
                {
                    Id = "border_clash",
                    Title = "BORDER CLASH",
                    Description = $"A {enemyNation?.Name ?? "foreign"} patrol has engaged your border guards. Shots fired. Casualties reported. Your generals are asking for orders — this could escalate fast.",
                    Priority = InterruptPriority.Critical,
                    TimerSeconds = 15f,
                    Choices = new[]
                    {
                        new InterruptChoice { Label = "De-escalate", EffectDescription = "Stand down. Lose face but avoid war." },
                        new InterruptChoice { Label = "Reinforce the border", EffectDescription = "Send troops. Shows strength but costs money." },
                        new InterruptChoice { Label = "Retaliate", EffectDescription = "Strike back hard. Risk full war." }
                    },
                    DefaultChoiceIndex = 0
                };
            }
        }

        // CRITICAL: Coup plotters (low stability / low authority)
        if (!_firedOneTimeIds.Contains("coup_attempt") && tick > 25 &&
            playerChar.FullAuthorityIndex < 35f)
        {
            _firedOneTimeIds.Add("coup_attempt");
            return new InterruptData
            {
                Id = "coup_attempt",
                Title = "COUP ATTEMPT",
                Description = "Military officers are moving on the presidential palace. Your security chief has 10 seconds to get you to the bunker. What are your orders?",
                Priority = InterruptPriority.Critical,
                TimerSeconds = 10f,
                Choices = new[]
                {
                    new InterruptChoice { Label = "Flee to the bunker", EffectDescription = "Survive but look weak. Authority drops." },
                    new InterruptChoice { Label = "Rally loyalist troops", EffectDescription = "Risky — could crush it or die trying." },
                    new InterruptChoice { Label = "Negotiate with plotters", EffectDescription = "Share power. Stability rises but authority splits." }
                },
                DefaultChoiceIndex = 0
            };
        }

        // URGENT: Economic crisis (treasury dropping fast)
        if (!_firedOneTimeIds.Contains("bank_run") && tick > 10 &&
            playerNation.Treasury < 500f)
        {
            _firedOneTimeIds.Add("bank_run");
            return new InterruptData
            {
                Id = "bank_run",
                Title = "BANK RUN",
                Description = "Panic is spreading through the financial district. Citizens are withdrawing everything. Your currency is in freefall. The finance minister needs a decision NOW.",
                Priority = InterruptPriority.Urgent,
                TimerSeconds = 30f,
                Choices = new[]
                {
                    new InterruptChoice { Label = "Bail out the banks", EffectDescription = "Costs treasury but stabilizes economy." },
                    new InterruptChoice { Label = "Freeze withdrawals", EffectDescription = "Saves money but infuriates the public." },
                    new InterruptChoice { Label = "Seize foreign assets", EffectDescription = "Bold move. Angers other nations." }
                },
                DefaultChoiceIndex = 1
            };
        }

        // URGENT: Anti-war protests (high war weariness — simulate via low prestige)
        if (!_firedOneTimeIds.Contains("protests") && tick > 20 &&
            playerNation.Prestige < 20f)
        {
            _firedOneTimeIds.Add("protests");
            return new InterruptData
            {
                Id = "protests",
                Title = "MASS PROTESTS",
                Description = "Thousands have taken to the streets demanding peace. Riot police are overwhelmed. International media is broadcasting live. Your approval is tanking.",
                Priority = InterruptPriority.Urgent,
                TimerSeconds = 45f,
                Choices = new[]
                {
                    new InterruptChoice { Label = "Address the nation", EffectDescription = "Speech boosts prestige but commits you to peace." },
                    new InterruptChoice { Label = "Crack down", EffectDescription = "Restore order by force. Prestige drops further." },
                    new InterruptChoice { Label = "Make concessions", EffectDescription = "Reduce military spending. Generals unhappy." }
                },
                DefaultChoiceIndex = 2
            };
        }

        // ROUTINE: Diplomatic offer (periodic)
        if (tick > 12 && tick % 30 == 0 && !_firedOneTimeIds.Contains($"diplomacy_{tick}"))
        {
            var otherNation = world.Nations.FirstOrDefault(n => n.Id != world.PlayerNationId && n.IsAlive);
            if (otherNation != null)
            {
                _firedOneTimeIds.Add($"diplomacy_{tick}");
                return new InterruptData
                {
                    Id = $"diplomacy_{tick}",
                    Title = "DIPLOMATIC CABLE",
                    Description = $"The ambassador from {otherNation.Name} has arrived with a proposal: a non-aggression pact and limited trade agreement. They want an answer before they leave the capital.",
                    Priority = InterruptPriority.Routine,
                    TimerSeconds = 90f,
                    Choices = new[]
                    {
                        new InterruptChoice { Label = "Accept the pact", EffectDescription = "Peace with this nation. Trade income rises." },
                        new InterruptChoice { Label = "Counter-offer", EffectDescription = "Demand better terms. They may walk away." },
                        new InterruptChoice { Label = "Reject", EffectDescription = "No deal. Relations cool." }
                    },
                    DefaultChoiceIndex = 2
                };
            }
        }

        // ROUTINE: Intelligence report (periodic)
        if (tick > 8 && tick % 20 == 0 && !_firedOneTimeIds.Contains($"intel_{tick}"))
        {
            var largestEnemy = world.Nations
                .Where(n => n.Id != world.PlayerNationId && n.IsAlive)
                .OrderByDescending(n => n.ProvinceCount)
                .FirstOrDefault();

            if (largestEnemy != null)
            {
                int enemyArmies = world.Armies.Count(a => a.NationId == largestEnemy.Id && a.IsAlive);
                _firedOneTimeIds.Add($"intel_{tick}");
                return new InterruptData
                {
                    Id = $"intel_{tick}",
                    Title = "INTELLIGENCE BRIEFING",
                    Description = $"Your spies report that {largestEnemy.Name} has mobilized {enemyArmies} army groups near the northern frontier. Satellite imagery suggests staging for an offensive. Confidence: MEDIUM.",
                    Priority = InterruptPriority.Routine,
                    TimerSeconds = 60f,
                    Choices = new[]
                    {
                        new InterruptChoice { Label = "Mobilize reserves", EffectDescription = "Prepare defenses. Costs treasury." },
                        new InterruptChoice { Label = "Deploy spies", EffectDescription = "Get better intel. Takes time." },
                        new InterruptChoice { Label = "Ignore it", EffectDescription = "Could be bluffing. Save resources." }
                    },
                    DefaultChoiceIndex = 2
                };
            }
        }

        return null;
    }

    private void FireInterrupt(InterruptData data)
    {
        GD.Print($"[InterruptEngine] THE PHONE RINGS: {data.Title} ({data.Priority})");

        // CRITICAL interrupts auto-pause the simulation
        if (data.Priority == InterruptPriority.Critical)
        {
            var clock = SimulationClock.Instance;
            if (clock != null && !clock.IsPaused)
                clock.TogglePause();
        }

        EventBus.Instance?.Publish(new InterruptTriggeredEvent(
            data.Id, data.Title, data.Description,
            data.TimerSeconds, data.Choices, data.DefaultChoiceIndex, data.Priority));
    }

    private void OnResolved(InterruptResolvedEvent ev)
    {
        var world = WorldStateManager.Instance?.Data;
        if (world == null) return;

        var playerNation = GetPlayerNation(world);
        var playerChar = world.Characters.FirstOrDefault(c => c.IsPlayer);
        if (playerNation == null || playerChar == null) return;

        string timeoutStr = ev.WasTimeout ? " (TIMEOUT)" : "";
        GD.Print($"[InterruptEngine] Resolved: {ev.Id} → Choice {ev.ChoiceIndex}{timeoutStr}");

        // Apply effects based on interrupt ID and choice
        ApplyEffects(ev.Id, ev.ChoiceIndex, playerNation, playerChar);

        EventBus.Instance?.Publish(new NotificationEvent(
            ev.WasTimeout ? "Phone went unanswered. Default outcome applied." : "Decision made.",
            ev.WasTimeout ? "warning" : "info"));
    }

    private void ApplyEffects(string id, int choice, NationData nation, CharacterData pc)
    {
        switch (id)
        {
            case "border_clash":
                if (choice == 0) { nation.Prestige -= 5; }        // De-escalate
                if (choice == 1) { nation.Treasury -= 200; }      // Reinforce
                if (choice == 2) { nation.Prestige += 10; nation.Treasury -= 300; }  // Retaliate
                break;

            case "coup_attempt":
                if (choice == 0) { pc.TerritoryAuthority = Math.Max(0, pc.TerritoryAuthority - 15); }
                if (choice == 1) { pc.TerritoryAuthority = Math.Min(100, pc.TerritoryAuthority + 20); nation.Treasury -= 150; }
                if (choice == 2) { pc.TerritoryAuthority = Math.Max(0, pc.TerritoryAuthority - 10); nation.Prestige += 5; }
                break;

            case "bank_run":
                if (choice == 0) { nation.Treasury -= 300; nation.Prestige += 5; }
                if (choice == 1) { nation.Prestige -= 10; }
                if (choice == 2) { nation.Treasury += 200; nation.Prestige -= 15; }
                break;

            case "protests":
                if (choice == 0) { nation.Prestige += 15; }
                if (choice == 1) { nation.Prestige -= 10; pc.TerritoryAuthority = Math.Min(100, pc.TerritoryAuthority + 5); }
                if (choice == 2) { nation.Treasury -= 100; nation.Prestige += 5; }
                break;

            default:
                // Diplomatic/intel — generic effects
                if (id.StartsWith("diplomacy_"))
                {
                    if (choice == 0) { nation.Treasury += 50; nation.Prestige += 3; }
                    if (choice == 1) { nation.Prestige -= 2; }
                    if (choice == 2) { nation.Prestige -= 5; }
                }
                else if (id.StartsWith("intel_"))
                {
                    if (choice == 0) { nation.Treasury -= 100; }
                    if (choice == 1) { nation.Treasury -= 50; }
                    // choice 2 = ignore, no cost
                }
                break;
        }
    }

    private static NationData? GetPlayerNation(WorldData world)
    {
        if (world.PlayerNationId == null) return null;
        int idx = int.Parse(world.PlayerNationId.Split('_')[1]);
        return idx < world.Nations.Count ? world.Nations[idx] : null;
    }

    private static float TileDistance(int x1, int y1, int x2, int y2)
    {
        float dx = x1 - x2;
        float dy = y1 - y2;
        return MathF.Sqrt(dx * dx + dy * dy);
    }
}
