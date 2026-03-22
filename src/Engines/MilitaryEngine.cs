using Godot;
using System;
using Warship.Core;
using Warship.Events;
using Warship.Data;

namespace Warship.Engines;

/// <summary>
/// Controls the massive armies autonomously. 
/// Translates High-Level Nation Orders (Border Watch, Stage, Attack) 
/// into individual troop steering commands.
/// Runs continuous simulation of troop movements.
/// </summary>
public partial class MilitaryEngine : Node
{
    private Random _rng = new(1337);
    private float _timer = 0f;
    private const float UpdateInterval = 0.5f; // Evaluate orders every 0.5 seconds

    public override void _Ready()
    {
        GD.Print("[MilitaryEngine] Online. Swarm control standing by.");
    }

    public override void _PhysicsProcess(double delta)
    {
        var world = WorldStateManager.Instance?.Data;
        if (world == null || world.Units == null) return;

        _timer += (float)delta;
        if (_timer >= UpdateInterval)
        {
            _timer = 0f;
            UpdateSwarm(world);
        }
    }

    private void UpdateSwarm(WorldData world)
    {
        // Cache nations
        var nations = world.Nations;

        // Process combat first (very simple: if two different nation soldiers are close -> fight)
        // O(n^2) is scary for 3000 units, but spatial hashing is better. 
        // For prototype, we'll keep combat simple or skip for now while we get movement right.

        foreach (var unit in world.Units)
        {
            if (!unit.IsAlive) continue;

            int natIdx = int.Parse(unit.NationId.Split('_')[1]);
            var nation = nations[natIdx];

            // Inherit global order if we are just a generic soldier 
            // (In FA-6 players set global orders, troops follow)
            if (unit.Type == UnitType.Soldier && unit.CurrentOrder != nation.GlobalMilitaryOrder)
            {
                unit.CurrentOrder = nation.GlobalMilitaryOrder;
            }

            Vector2 currentPos = new Vector2(unit.PixelX, unit.PixelY);
            Vector2 targetPos = currentPos;

            switch (unit.CurrentOrder)
            {
                case MilitaryOrder.BorderWatch:
                    // Random walk very slowly, but bias towards borders.
                    // For now: Simple random walk 10-20 pixels
                    targetPos += new Vector2((float)(_rng.NextDouble() * 40 - 20), (float)(_rng.NextDouble() * 40 - 20));
                    break;

                case MilitaryOrder.Stage:
                    if (nation.CommandTargetX >= 0)
                    {
                        Vector2 rallyPoint = new Vector2(nation.CommandTargetX * 64 + 32, nation.CommandTargetY * 64 + 32);
                        Vector2 dir = (rallyPoint - currentPos).Normalized();
                        // Walk towards rally, add some swarm noise so they don't form a single dot
                        targetPos += dir * 25f + new Vector2((float)(_rng.NextDouble() * 20 - 10), (float)(_rng.NextDouble() * 20 - 10));
                    }
                    break;

                case MilitaryOrder.Attack:
                    if (nation.CommandTargetX >= 0)
                    {
                        Vector2 attackPoint = new Vector2(nation.CommandTargetX * 64 + 32, nation.CommandTargetY * 64 + 32);
                        Vector2 dir = (attackPoint - currentPos).Normalized();
                        targetPos += dir * 35f + new Vector2((float)(_rng.NextDouble() * 15 - 7.5f), (float)(_rng.NextDouble() * 15 - 7.5f));
                    }
                    break;

                case MilitaryOrder.Patrol:
                case MilitaryOrder.Standby:
                default:
                    // Small jitter
                    targetPos += new Vector2((float)(_rng.NextDouble() * 10 - 5), (float)(_rng.NextDouble() * 10 - 5));
                    break;
            }

            // Keep them on the map loosely
            float maxW = world.MapWidth * 64;
            float maxH = world.MapHeight * 64;
            targetPos.X = Mathf.Clamp(targetPos.X, 0, maxW);
            targetPos.Y = Mathf.Clamp(targetPos.Y, 0, maxH);

            unit.TargetPixelX = targetPos.X;
            unit.TargetPixelY = targetPos.Y;
        }

        // --- FA-7 COMBAT SIMULATION ---
        // If swarms collide, they battle. Strength is heavily tied to the Nation's Leaders.
        for (int i = 0; i < world.Units.Count; i++)
        {
            var u1 = world.Units[i];
            if (!u1.IsAlive) continue;
            
            for (int j = i + 1; j < world.Units.Count; j++)
            {
                var u2 = world.Units[j];
                if (!u2.IsAlive) continue;
                if (u1.NationId == u2.NationId) continue;
                
                float dx = u1.PixelX - u2.PixelX;
                float dy = u1.PixelY - u2.PixelY;
                float distSq = dx * dx + dy * dy;
                
                // If within 20 pixels of each other (melee range)
                if (distSq < 400f)
                {
                    float u1Strength = GetNationStrength(world, u1.NationId);
                    float u2Strength = GetNationStrength(world, u2.NationId);
                    
                    if (_rng.NextDouble() * u1Strength > _rng.NextDouble() * u2Strength)
                    {
                        u2.IsAlive = false; // u1 wins
                        // Check if u2 was a character? Characters aren't in world.Units, they are in world.Characters.
                    }
                    else
                    {
                        u1.IsAlive = false; // u2 wins
                    }
                }
            }
        }

        // --- FA-7 CITY CAPTURE ---
        // If an enemy troop gets close enough to a city, they capture it! 
        // This is why eliminating leaders is powerful (troops die, leaving cities undefended).
        foreach (var unit in world.Units)
        {
            if (!unit.IsAlive) continue;

            foreach (var city in world.Cities)
            {
                if (city.NationId == unit.NationId) continue; // Already own it

                float cx = city.TileX * 64 + 32;
                float cy = city.TileY * 64 + 32;
                float distSq = (unit.PixelX - cx) * (unit.PixelX - cx) + (unit.PixelY - cy) * (unit.PixelY - cy);

                if (distSq < 900f) // Within 30 pixels (half a tile)
                {
                    // The city is captured!
                    var oldOwner = world.Nations[int.Parse(city.NationId.Split('_')[1])];
                    var newOwner = world.Nations[int.Parse(unit.NationId.Split('_')[1])];

                    city.NationId = unit.NationId;
                    
                    // Adjust territories
                    oldOwner.ProvinceCount = Math.Max(0, oldOwner.ProvinceCount - 1);
                    newOwner.ProvinceCount++;
                    
                    // Force the ownership map to update visually for the surrounding area
                    // Not writing the complex flood-fill for borders here, but we will notify!
                    if (newOwner.IsPlayer)
                    {
                        EventBus.Instance?.Publish(new NotificationEvent($"We captured {city.Name} from {oldOwner.Name}!", "success"));
                    }
                    else if (oldOwner.IsPlayer)
                    {
                        EventBus.Instance?.Publish(new NotificationEvent($"{city.Name} has fallen to {newOwner.Name}!", "danger"));
                    }
                }
            }
        }
    }

    private float GetNationStrength(WorldData world, string nationId)
    {
        // Base military effectiveness
        float maxTa = 10f; 
        
        // Find highest Territory Authority amongst ALIVE leaders.
        // If leaders are assassinated, troops lose morale and logistical support, making them easy to slaughter.
        foreach (var c in world.Characters)
        {
            if (c.NationId == nationId && c.Role != "Eliminated")
            {
                if (c.TerritoryAuthority > maxTa) maxTa = c.TerritoryAuthority;
            }
        }
        
        return maxTa / 10f; // Multiplier between 1.0 (dead leaders) and 10.0 (iron grip)
    }
}
