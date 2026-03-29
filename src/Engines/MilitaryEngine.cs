using Godot;
using System;
using System.Linq;
using Warship.Core;
using Warship.Events;
using Warship.Data;

namespace Warship.Engines;

/// <summary>
/// Processes army movement, per-army orders, and combat resolution.
/// Uses the Army system (not legacy Units). Fires BattleResolvedEvent on combat.
/// Runs on physics tick for smooth movement interpolation.
/// </summary>
public partial class MilitaryEngine : Node
{
    private float _combatTimer = 0f;
    private const float CombatInterval = 1.0f;

    public override void _Ready()
    {
        EventBus.Instance?.Subscribe<ArmyOrderEvent>(OnArmyOrder);
        EventBus.Instance?.Subscribe<ArmyFormationEvent>(OnArmyFormation);
        EventBus.Instance?.Subscribe<TurnAdvancedEvent>(OnTurnAdvanced);
        GD.Print("[MilitaryEngine] Online. Army command standing by.");
    }

    public override void _ExitTree()
    {
        EventBus.Instance?.Unsubscribe<ArmyOrderEvent>(OnArmyOrder);
        EventBus.Instance?.Unsubscribe<ArmyFormationEvent>(OnArmyFormation);
        EventBus.Instance?.Unsubscribe<TurnAdvancedEvent>(OnTurnAdvanced);
    }

    private void OnArmyOrder(ArmyOrderEvent ev)
    {
        var army = WorldStateManager.Instance?.Data?.Armies.FirstOrDefault(a => a.Id == ev.ArmyId);
        if (army != null) army.CurrentOrder = ev.Order;
    }

    private void OnArmyFormation(ArmyFormationEvent ev)
    {
        var army = WorldStateManager.Instance?.Data?.Armies.FirstOrDefault(a => a.Id == ev.ArmyId);
        if (army != null) army.Formation = ev.Formation;
    }

    private void OnTurnAdvanced(TurnAdvancedEvent ev)
    {
        var world = WorldStateManager.Instance?.Data;
        if (world == null) return;

        foreach (var army in world.Armies.Where(a => a.IsAlive))
        {
            // Supply drain varies by order
            float supplyDrain = army.CurrentOrder switch
            {
                MilitaryOrder.Attack => 2f,
                MilitaryOrder.Patrol => 1f,
                MilitaryOrder.Stage => 0.5f,
                _ => 0.3f
            };
            army.Supply = Math.Clamp(army.Supply - supplyDrain, 0, 100);

            if (army.Supply < 20f)
                army.Morale = Math.Clamp(army.Morale - 2f, 0, 100);

            // Organization slowly recovers
            army.Organization = Math.Clamp(army.Organization + 1f, 0, 100);

            // Council policy effects
            int nIdx = ParseNationIdx(army.NationId, world.Nations.Count);
            if (nIdx < 0) continue;
            var nation = world.Nations[nIdx];

            // Conscription adds infantry
            if (nation.Council.ConscriptionActive && army.TotalStrength < 500)
            {
                army.Composition[UnitType.Infantry] = (army.Composition.TryGetValue(UnitType.Infantry, out var _ic) ? _ic : 0) + 5;
            }

            // High defense budget resupplies
            if (nation.Council.DefenseBudgetPct > 0.3f)
            {
                float resupply = (nation.Council.DefenseBudgetPct - 0.3f) * 10f;
                army.Supply = Math.Clamp(army.Supply + resupply, 0, 100);
            }

            // Martial law boosts morale
            if (nation.Council.MartialLawActive)
                army.Morale = Math.Clamp(army.Morale + 1f, 0, 100);
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        var world = WorldStateManager.Instance?.Data;
        if (world == null || world.Armies.Count == 0) return;

        ProcessArmyMovement(world);

        _combatTimer += (float)delta;
        if (_combatTimer >= CombatInterval)
        {
            _combatTimer = 0f;
            ProcessArmyCombat(world);
            ProcessCitySieges(world);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  ARMY MOVEMENT
    // ═══════════════════════════════════════════════════════════════

    private void ProcessArmyMovement(WorldData world)
    {
        const int TileSize = 32;

        foreach (var army in world.Armies)
        {
            if (!army.IsAlive) continue;

            int nIdx = ParseNationIdx(army.NationId, world.Nations.Count);
            if (nIdx < 0) continue;
            var nation = world.Nations[nIdx];

            switch (army.CurrentOrder)
            {
                case MilitaryOrder.Attack:
                case MilitaryOrder.Stage:
                    // Move toward command target if no individual target set
                    if (nation.CommandTargetX >= 0 &&
                        army.TargetTileX == army.TileX && army.TargetTileY == army.TileY)
                    {
                        army.TargetTileX = nation.CommandTargetX;
                        army.TargetTileY = nation.CommandTargetY;
                        army.TargetPixelX = nation.CommandTargetX * TileSize + TileSize / 2f;
                        army.TargetPixelY = nation.CommandTargetY * TileSize + TileSize / 2f;
                    }
                    break;

                case MilitaryOrder.Patrol:
                    if (Math.Abs(army.PixelX - army.TargetPixelX) < 10f &&
                        Math.Abs(army.PixelY - army.TargetPixelY) < 10f)
                    {
                        int range = 5;
                        army.TargetTileX = Math.Clamp(army.TileX + SimRng.Next(-range, range + 1), 0, world.MapWidth - 1);
                        army.TargetTileY = Math.Clamp(army.TileY + SimRng.Next(-range, range + 1), 0, world.MapHeight - 1);
                        army.TargetPixelX = army.TargetTileX * TileSize + TileSize / 2f;
                        army.TargetPixelY = army.TargetTileY * TileSize + TileSize / 2f;
                    }
                    break;

                case MilitaryOrder.Standby:
                    // Retreat toward capital
                    if (nation.CapitalX >= 0 && nation.CapitalY >= 0 &&
                        army.TargetTileX == army.TileX && army.TargetTileY == army.TileY)
                    {
                        army.TargetTileX = nation.CapitalX;
                        army.TargetTileY = nation.CapitalY;
                        army.TargetPixelX = nation.CapitalX * TileSize + TileSize / 2f;
                        army.TargetPixelY = nation.CapitalY * TileSize + TileSize / 2f;
                    }
                    break;
            }

            // Update tile from pixel position
            int tx = (int)(army.PixelX / TileSize);
            int ty = (int)(army.PixelY / TileSize);
            army.TileX = Math.Clamp(tx, 0, world.MapWidth - 1);
            army.TileY = Math.Clamp(ty, 0, world.MapHeight - 1);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  COMBAT — Army-vs-army when within engagement range
    // ═══════════════════════════════════════════════════════════════

    private void ProcessArmyCombat(WorldData world)
    {
        const float EngagementRange = 100f;

        for (int i = 0; i < world.Armies.Count; i++)
        {
            var a1 = world.Armies[i];
            if (!a1.IsAlive || a1.TotalStrength <= 0) continue;

            int n1 = ParseNationIdx(a1.NationId, world.Nations.Count);
            if (n1 < 0) continue;

            for (int j = i + 1; j < world.Armies.Count; j++)
            {
                var a2 = world.Armies[j];
                if (!a2.IsAlive || a2.TotalStrength <= 0) continue;
                if (a1.NationId == a2.NationId) continue;

                // Must be at war
                int n2 = ParseNationIdx(a2.NationId, world.Nations.Count);
                if (n2 < 0) continue;
                var relation = world.Nations[n1].Relations.TryGetValue(world.Nations[n2].Id, out var _rel) ? _rel : DiplomaticStatus.Neutral;
                if (relation != DiplomaticStatus.AtWar) continue;

                float dx = a1.PixelX - a2.PixelX;
                float dy = a1.PixelY - a2.PixelY;
                if (dx * dx + dy * dy > EngagementRange * EngagementRange) continue;

                // At least one side must be attacking to initiate combat
                if (a1.CurrentOrder != MilitaryOrder.Attack && a2.CurrentOrder != MilitaryOrder.Attack)
                    continue;

                ResolveBattle(world, a1, a2);
            }
        }
    }

    private void ResolveBattle(WorldData world, ArmyData attacker, ArmyData defender)
    {
        float atkPower = attacker.TotalAttackPower * (attacker.Morale / 100f) * (attacker.Organization / 100f);
        float defPower = defender.TotalDefensePower * (defender.Morale / 100f) * (defender.Organization / 100f);

        atkPower *= FormationAttackMod(attacker.Formation);
        defPower *= FormationDefenseMod(defender.Formation);

        // Terrain bonus for defender
        if (world.TerrainMap != null)
        {
            int terrain = world.TerrainMap[
                Math.Clamp(defender.TileX, 0, world.MapWidth - 1),
                Math.Clamp(defender.TileY, 0, world.MapHeight - 1)];
            defPower *= TerrainDefenseBonus(terrain);
        }

        if (attacker.Supply < 30f) atkPower *= 0.6f;
        if (defender.Supply < 30f) defPower *= 0.6f;

        atkPower *= 0.8f + (float)SimRng.NextDouble() * 0.4f;
        defPower *= 0.8f + (float)SimRng.NextDouble() * 0.4f;

        float ratio = atkPower / Math.Max(defPower, 1f);
        int atkLoss = (int)(attacker.TotalStrength * Math.Clamp(1f / ratio * 0.15f, 0.02f, 0.3f));
        int defLoss = (int)(defender.TotalStrength * Math.Clamp(ratio * 0.15f, 0.02f, 0.3f));
        bool attackerWon = atkPower > defPower;

        ApplyLosses(attacker, atkLoss);
        ApplyLosses(defender, defLoss);

        if (attackerWon)
        {
            attacker.Morale = Math.Clamp(attacker.Morale + 5f, 0, 100);
            defender.Morale = Math.Clamp(defender.Morale - 15f, 0, 100);
            defender.Organization = Math.Clamp(defender.Organization - 20f, 0, 100);
            if (defender.Morale < 20f) defender.CurrentOrder = MilitaryOrder.Standby;
        }
        else
        {
            defender.Morale = Math.Clamp(defender.Morale + 5f, 0, 100);
            attacker.Morale = Math.Clamp(attacker.Morale - 15f, 0, 100);
            attacker.Organization = Math.Clamp(attacker.Organization - 20f, 0, 100);
        }

        if (attacker.TotalStrength <= 0) attacker.IsAlive = false;
        if (defender.TotalStrength <= 0) defender.IsAlive = false;

        // War weariness on nations
        int aN = ParseNationIdx(attacker.NationId, world.Nations.Count);
        int dN = ParseNationIdx(defender.NationId, world.Nations.Count);
        if (aN < 0 || dN < 0) return;
        world.Nations[aN].WarWeariness = Math.Clamp(world.Nations[aN].WarWeariness + atkLoss * 0.02f, 0, 100);
        world.Nations[dN].WarWeariness = Math.Clamp(world.Nations[dN].WarWeariness + defLoss * 0.02f, 0, 100);

        EventBus.Instance?.Publish(new BattleResolvedEvent(attacker.Id, defender.Id, attackerWon, atkLoss, defLoss));

        // Player notifications
        bool atkIsPlayer = world.Nations[aN].IsPlayer;
        bool defIsPlayer = world.Nations[dN].IsPlayer;
        if (atkIsPlayer || defIsPlayer)
        {
            string winner = attackerWon ? attacker.Name : defender.Name;
            string loser = attackerWon ? defender.Name : attacker.Name;
            bool playerWon = (atkIsPlayer && attackerWon) || (defIsPlayer && !attackerWon);
            EventBus.Instance?.Publish(new NotificationEvent(
                $"Battle: {winner} defeats {loser}! Losses: {atkLoss}/{defLoss}",
                playerWon ? "success" : "danger"));
        }
    }

    private static void ApplyLosses(ArmyData army, int totalLosses)
    {
        var types = army.Composition.OrderByDescending(kv => kv.Value).Select(kv => kv.Key).ToList();
        int remaining = totalLosses;
        foreach (var type in types)
        {
            if (remaining <= 0) break;
            int count = army.Composition[type];
            int removed = Math.Min(count, remaining);
            army.Composition[type] = count - removed;
            remaining -= removed;
            if (army.Composition[type] <= 0) army.Composition.Remove(type);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  CITY SIEGES
    // ═══════════════════════════════════════════════════════════════

    private void ProcessCitySieges(WorldData world)
    {
        const int TileSize = 32;
        const float SiegeRange = 80f;

        foreach (var army in world.Armies)
        {
            if (!army.IsAlive || army.CurrentOrder != MilitaryOrder.Attack) continue;

            int armyN = ParseNationIdx(army.NationId, world.Nations.Count);
            if (armyN < 0) continue;

            foreach (var city in world.Cities)
            {
                if (city.NationId == army.NationId) continue;

                // Must be at war with city owner
                int cityN = ParseNationIdx(city.NationId, world.Nations.Count);
                if (cityN < 0) continue;
                var rel = world.Nations[armyN].Relations.TryGetValue(world.Nations[cityN].Id, out var _sr) ? _sr : DiplomaticStatus.Neutral;
                if (rel != DiplomaticStatus.AtWar) continue;

                float cx = city.TileX * TileSize + TileSize / 2f;
                float cy = city.TileY * TileSize + TileSize / 2f;
                float dx = army.PixelX - cx;
                float dy = army.PixelY - cy;
                if (dx * dx + dy * dy > SiegeRange * SiegeRange) continue;

                city.HP -= Math.Max(1, army.TotalStrength / 50);

                if (city.HP <= 0)
                {
                    string oldId = city.NationId;
                    city.NationId = army.NationId;
                    city.HP = city.MaxHP / 2;

                    var oldN = world.Nations[cityN];
                    var newN = world.Nations[armyN];
                    oldN.ProvinceCount = Math.Max(0, oldN.ProvinceCount - city.ControlRadius);
                    newN.ProvinceCount += city.ControlRadius;

                    EventBus.Instance?.Publish(new CityCapturedEvent(city.Id, oldId, army.NationId));

                    if (newN.IsPlayer)
                        EventBus.Instance?.Publish(new NotificationEvent($"CAPTURED {city.Name} from {oldN.Name}!", "success"));
                    else if (oldN.IsPlayer)
                        EventBus.Instance?.Publish(new NotificationEvent($"{city.Name} has FALLEN to {newN.Name}!", "danger"));
                }
            }
        }
    }

    /// <summary>Safely parse nation index from "N_0" format IDs. Returns -1 if invalid.</summary>
    private static int ParseNationIdx(string nationId, int nationCount)
    {
        var parts = nationId.Split('_');
        if (parts.Length < 2 || !int.TryParse(parts[1], out int idx)) return -1;
        return idx >= 0 && idx < nationCount ? idx : -1;
    }

    private static float FormationAttackMod(FormationType f) => f switch
    {
        FormationType.Wedge => 1.15f, FormationType.Column => 0.9f,
        FormationType.Circle => 0.75f, _ => 1.0f
    };

    private static float FormationDefenseMod(FormationType f) => f switch
    {
        FormationType.Circle => 1.20f, FormationType.Spread => 1.05f,
        FormationType.Wedge => 0.95f, FormationType.Column => 0.90f, _ => 1.0f
    };

    private static float TerrainDefenseBonus(int t) => t switch
    {
        (int)TerrainType.Forest => 1.25f, (int)TerrainType.Hills => 1.30f,
        (int)TerrainType.Mountain => 1.40f, (int)TerrainType.Snow => 1.10f, _ => 1.0f
    };
}
