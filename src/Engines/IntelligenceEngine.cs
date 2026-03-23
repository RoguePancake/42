using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Warship.Core;
using Warship.Data;
using Warship.Events;

namespace Warship.Engines;

/// <summary>
/// Manages Fog of War. Each nation accumulates intel on rivals based on BSA.
/// Intel decays over time, requiring sustained investment. The DossierPanel
/// and AIEngine query this to get fogged stat values.
/// </summary>
public partial class IntelligenceEngine : Node
{
    // BSA threshold: at this level, passive intel gain = decay (breakeven)
    private const float BsaBreakeven = 40f;
    private const float PassiveGainRate = 5f;   // max pts/turn at BSA 100
    private const float DecayRate = 2f;          // pts lost per turn
    private const float InvestigateBonus = 15f;  // pts from "investigate" action
    private const float ReviewIntelBonus = 5f;   // pts from "review_intel" (all targets)

    // IntelLevel thresholds
    private static readonly (float threshold, IntelLevel level)[] Thresholds =
    {
        (80f, IntelLevel.Complete),
        (60f, IntelLevel.Confirmed),
        (40f, IntelLevel.Observed),
        (20f, IntelLevel.Rumor),
        (0f, IntelLevel.Unknown)
    };

    public override void _Ready()
    {
        EventBus.Instance!.Subscribe<TurnAdvancedEvent>(OnTurnAdvanced);
        GD.Print("[IntelligenceEngine] Online. Fog of war active.");
    }

    private void OnTurnAdvanced(TurnAdvancedEvent ev)
    {
        var world = WorldStateManager.Instance?.Data;
        if (world == null) return;

        var aliveNations = world.Nations.Where(n => true).ToList();

        foreach (var observer in aliveNations)
        {
            // Find the observer's leader (for BSA)
            var leader = world.Characters.FirstOrDefault(c =>
                c.NationId == observer.Id && c.Role != "Eliminated");
            if (leader == null) continue;

            float bsa = leader.BehindTheScenesAuthority;

            foreach (var target in aliveNations)
            {
                if (target.Id == observer.Id) continue;

                var record = GetOrCreateRecord(world, observer.Id, target.Id);
                float oldPoints = record.IntelPoints;
                IntelLevel oldLevel = PointsToLevel(oldPoints);

                // Passive gain based on BSA
                float gain = (bsa / 100f) * PassiveGainRate;
                // Decay
                float net = gain - DecayRate;
                record.IntelPoints = Math.Clamp(record.IntelPoints + net, 0f, 100f);
                record.LastUpdatedTurn = ev.Turn;

                IntelLevel newLevel = PointsToLevel(record.IntelPoints);

                // Publish change if meaningful
                if (MathF.Abs(record.IntelPoints - oldPoints) > 0.01f)
                {
                    EventBus.Instance?.Publish(new IntelChangedEvent(
                        observer.Id, target.Id, oldPoints, record.IntelPoints, "Passive"));
                }

                // Notify player when crossing an intel level threshold
                if (observer.IsPlayer && newLevel != oldLevel)
                {
                    string direction = newLevel > oldLevel ? "improved" : "degraded";
                    EventBus.Instance?.Publish(new NotificationEvent(
                        $"Intel on {target.Name} {direction} to {newLevel}.", "info"));
                }
            }
        }
    }

    // ─── Static Helpers (callable by other engines/UI) ──────────

    public static IntelLevel PointsToLevel(float points)
    {
        foreach (var (threshold, level) in Thresholds)
        {
            if (points >= threshold) return level;
        }
        return IntelLevel.Unknown;
    }

    public static IntelLevel GetIntelLevel(WorldData world, string observerNationId, string targetNationId)
    {
        var record = world.IntelRecords.FirstOrDefault(r =>
            r.ObserverNationId == observerNationId && r.TargetNationId == targetNationId);
        return record != null ? PointsToLevel(record.IntelPoints) : IntelLevel.Unknown;
    }

    public static float GetIntelPoints(WorldData world, string observerNationId, string targetNationId)
    {
        var record = world.IntelRecords.FirstOrDefault(r =>
            r.ObserverNationId == observerNationId && r.TargetNationId == targetNationId);
        return record?.IntelPoints ?? 0f;
    }

    /// <summary>
    /// Returns a fogged version of a stat value. Returns -1 for Unknown (display as "???").
    /// The seed parameter ensures stable values within a turn.
    /// </summary>
    public static float GetFoggedValue(float realValue, IntelLevel level, int seed)
    {
        switch (level)
        {
            case IntelLevel.Complete:
                return realValue;
            case IntelLevel.Confirmed:
                return MathF.Round(realValue / 5f) * 5f;
            case IntelLevel.Observed:
            {
                var rng = new Random(seed);
                float rounded = MathF.Round(realValue / 10f) * 10f;
                float offset = (float)(rng.NextDouble() * 10 - 5);
                return Math.Clamp(rounded + offset, 0f, 100f);
            }
            case IntelLevel.Rumor:
            {
                var rng = new Random(seed);
                float rounded = MathF.Round(realValue / 25f) * 25f;
                float offset = (float)(rng.NextDouble() * 24 - 12);
                return Math.Clamp(rounded + offset, 0f, 100f);
            }
            default: // Unknown
                return -1f;
        }
    }

    public static IntelRecord GetOrCreateRecord(WorldData world, string observerNationId, string targetNationId)
    {
        var record = world.IntelRecords.FirstOrDefault(r =>
            r.ObserverNationId == observerNationId && r.TargetNationId == targetNationId);
        if (record == null)
        {
            record = new IntelRecord
            {
                ObserverNationId = observerNationId,
                TargetNationId = targetNationId,
                IntelPoints = 0f,
                LastUpdatedTurn = world.TurnNumber
            };
            world.IntelRecords.Add(record);
        }
        return record;
    }

    /// <summary>
    /// Called by PoliticalEngine when "investigate" succeeds.
    /// Grants bonus intel points on a specific target.
    /// </summary>
    public static void GrantInvestigateBonus(WorldData world, string observerNationId, string targetNationId)
    {
        var record = GetOrCreateRecord(world, observerNationId, targetNationId);
        float old = record.IntelPoints;
        record.IntelPoints = Math.Clamp(record.IntelPoints + InvestigateBonus, 0f, 100f);
        EventBus.Instance?.Publish(new IntelChangedEvent(
            observerNationId, targetNationId, old, record.IntelPoints, "Investigation"));
    }

    /// <summary>
    /// Called by PoliticalEngine when "review_intel" is used.
    /// Grants small intel bonus on ALL rival nations.
    /// </summary>
    public static void GrantReviewIntelBonus(WorldData world, string observerNationId)
    {
        foreach (var nation in world.Nations)
        {
            if (nation.Id == observerNationId) continue;
            var record = GetOrCreateRecord(world, observerNationId, nation.Id);
            float old = record.IntelPoints;
            record.IntelPoints = Math.Clamp(record.IntelPoints + ReviewIntelBonus, 0f, 100f);
            EventBus.Instance?.Publish(new IntelChangedEvent(
                observerNationId, nation.Id, old, record.IntelPoints, "Intel Review"));
        }
    }
}
