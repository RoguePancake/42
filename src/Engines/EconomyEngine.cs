using Godot;
using System;
using System.Linq;
using Warship.Core;
using Warship.Data;
using Warship.Events;

namespace Warship.Engines;

/// <summary>
/// Macro-economy engine. Each tick:
/// - Collects tax income (modified by council tax rate)
/// - Deducts army upkeep (modified by defense budget)
/// - Replenishes resources from territory
/// - Handles bankruptcy consequences
/// </summary>
public partial class EconomyEngine : Node
{
    public override void _Ready()
    {
        EventBus.Instance?.Subscribe<TurnAdvancedEvent>(OnTurnAdvanced);
        GD.Print("[EconomyEngine] Online. The treasury hungers.");
    }

    private void OnTurnAdvanced(TurnAdvancedEvent ev)
    {
        var world = WorldStateManager.Instance?.Data;
        if (world == null) return;

        foreach (var nation in world.Nations)
        {
            if (!nation.IsAlive) continue;

            // ─── INCOME ───
            float baseIncome = CalculateBaseIncome(world, nation);
            float taxMultiplier = nation.Council.TaxRate / 0.20f; // 1.0x at 20% tax
            float income = baseIncome * taxMultiplier;

            // ─── ARMY UPKEEP ───
            float armyUpkeep = 0f;
            foreach (var army in world.Armies)
            {
                if (!army.IsAlive || army.NationId != nation.Id) continue;
                // Each unit costs based on type
                foreach (var (type, count) in army.Composition)
                {
                    float costPerUnit = type switch
                    {
                        UnitType.Infantry => 0.2f,
                        UnitType.MechInfantry => 0.5f,
                        UnitType.Tank => 1.5f,
                        UnitType.Artillery => 1.0f,
                        UnitType.Fighter => 2.0f,
                        UnitType.Bomber => 2.5f,
                        UnitType.Destroyer => 3.0f,
                        UnitType.Cruiser => 4.0f,
                        UnitType.Carrier => 6.0f,
                        UnitType.Submarine => 2.5f,
                        UnitType.NuclearMissile => 10.0f,
                        _ => 0.3f
                    };
                    armyUpkeep += count * costPerUnit;
                }
            }

            // Defense budget covers upkeep; excess is wasted, deficit comes from treasury
            float defenseBudget = income * nation.Council.DefenseBudgetPct;
            float civilianIncome = income - defenseBudget;
            float militaryDeficit = Math.Max(0, armyUpkeep - defenseBudget);

            nation.Treasury += civilianIncome - militaryDeficit;

            // ─── RESOURCE REPLENISHMENT (from territory) ───
            // Resources tick based on province count and terrain
            float provinceFactor = Math.Clamp(nation.ProvinceCount / 500f, 0.1f, 5f);
            nation.Iron = Math.Clamp(nation.Iron + 1f * provinceFactor, 0, 100);
            nation.Oil = Math.Clamp(nation.Oil + 0.8f * provinceFactor, 0, 100);
            nation.Electronics = Math.Clamp(nation.Electronics + 0.5f * provinceFactor, 0, 100);
            nation.Manpower = Math.Clamp(nation.Manpower + 1.5f * provinceFactor, 0, 100);
            nation.Food = Math.Clamp(nation.Food + 2f * provinceFactor, 0, 100);
            // Uranium barely replenishes (rare)
            nation.Uranium = Math.Clamp(nation.Uranium + 0.1f * provinceFactor, 0, 100);

            // Food consumption
            float foodConsumption = nation.ProvinceCount * 0.005f + world.Armies.Count(a => a.NationId == nation.Id && a.IsAlive) * 0.5f;
            nation.Food = Math.Clamp(nation.Food - foodConsumption, 0, 100);

            // Low food -> stability hit
            if (nation.Food < 10f)
            {
                nation.Stability = Math.Clamp(nation.Stability - 2f, 0, 100);
                if (nation.IsPlayer && ev.Turn % 5 == 0)
                    EventBus.Instance?.Publish(new NotificationEvent("FAMINE WARNING: Food critically low! Stability dropping.", "danger"));
            }

            // ─── STABILITY DRIFT ───
            // High war weariness pushes stability down
            if (nation.WarWeariness > 50f)
                nation.Stability = Math.Clamp(nation.Stability - (nation.WarWeariness - 50f) * 0.05f, 0, 100);

            // Martial law slowly erodes (people get restless)
            if (nation.Council.MartialLawActive)
                nation.Stability = Math.Clamp(nation.Stability - 0.5f, 0, 100);

            // ─── BANKRUPTCY ───
            if (nation.Treasury < -1000f)
            {
                nation.Stability = Math.Clamp(nation.Stability - 5f, 0, 100);
                nation.Prestige = Math.Clamp(nation.Prestige - 3f, 0, 100);

                // Armies lose morale
                foreach (var army in world.Armies.Where(a => a.NationId == nation.Id && a.IsAlive))
                    army.Morale = Math.Clamp(army.Morale - 5f, 0, 100);

                if (nation.IsPlayer && ev.Turn % 3 == 0)
                    EventBus.Instance?.Publish(new NotificationEvent(
                        $"ECONOMIC CRISIS! Debt: ${Math.Abs(nation.Treasury):0}M. Stability and morale collapsing!", "danger"));
            }

            // ─── PRESTIGE DRIFT ───
            // Large nations with high treasury slowly gain prestige
            if (nation.Treasury > 2000f && nation.Tier == NationTier.Large)
                nation.Prestige = Math.Clamp(nation.Prestige + 0.5f, 0, 100);

            // War weariness slowly decays in peacetime
            bool atWar = nation.Relations.Values.Any(r => r == DiplomaticStatus.AtWar);
            if (!atWar)
                nation.WarWeariness = Math.Clamp(nation.WarWeariness - 1f, 0, 100);
        }
    }

    private float CalculateBaseIncome(WorldData world, NationData nation)
    {
        float baseIncome = 50f; // $50M base
        float provinceIncome = nation.ProvinceCount * 0.1f; // $0.1M per tile

        float cityIncome = 0f;
        foreach (var city in world.Cities)
        {
            if (city.NationId != nation.Id) continue;
            cityIncome += city.IsCapital ? 150f : city.Size switch { 2 => 80f, _ => 40f };
        }

        // Trade trait bonus
        if (nation.Traits.Contains(NationTrait.TradeEmpire))
            cityIncome *= 2f;

        // Sovereign wealth interest
        if (nation.Traits.Contains(NationTrait.SovereignWealth) && nation.Treasury > 0)
            baseIncome += nation.Treasury * 0.02f;

        return baseIncome + provinceIncome + cityIncome;
    }
}
