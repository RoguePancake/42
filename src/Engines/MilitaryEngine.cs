using Godot;
using System;
using System.Linq;
using Warship.Core;
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
    }
}
