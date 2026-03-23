using Godot;
using Warship.Core;
using Warship.Events;

namespace Warship.Engines;

/// <summary>
/// Handles the macro-economics of Nations in the simulation.
/// Generates income per turn based on cities and territory.
/// </summary>
public partial class EconomyEngine : Node
{
    public override void _Ready()
    {
        EventBus.Instance?.Subscribe<TurnAdvancedEvent>(OnTurnAdvanced);
        GD.Print("[EconomyEngine] Booted up. The military-industrial complex is hungry.");
    }

    private void OnTurnAdvanced(TurnAdvancedEvent ev)
    {
        var world = WorldStateManager.Instance?.Data;
        if (world == null) return;

        foreach (var nation in world.Nations)
        {
            float income = CalculateIncome(world, nation);
            nation.Treasury += income;

            // Optional: Huge armies cost upkeep
            float armyUpkeep = 0f;
            foreach (var unit in world.Units)
            {
                if (unit.IsAlive && unit.NationId == nation.Id)
                {
                    armyUpkeep += 0.5f; // $0.5M per unit
                }
            }

            nation.Treasury -= armyUpkeep;

            // Bankrupt nations face authority penalties
            if (nation.Treasury < 0)
            {
                nation.Treasury = 0;
                // Penalize leader's TA
                foreach (var c in world.Characters)
                {
                    if (c.NationId == nation.Id && c.Role != "Eliminated")
                    {
                        float oldTA = c.TerritoryAuthority;
                        c.TerritoryAuthority -= 5.0f; // economic collapse ruins territory hold
                        if (c.TerritoryAuthority <= 0f) c.TerritoryAuthority = 1f;

                        EventBus.Instance?.Publish(new AuthorityChangedEvent(c.Id, "TA", oldTA, c.TerritoryAuthority, "Economic Collapse"));
                        
                        if (c.IsPlayer)
                        {
                            EventBus.Instance?.Publish(new NotificationEvent("ECONOMIC COLLAPSE! Treasury empty. Authority crashing.", "danger"));
                        }
                    }
                }
            }
        }
    }

    private float CalculateIncome(Data.WorldData world, Data.NationData nation)
    {
        float baseIncome = 50f; // $50M base
        float provinceIncome = nation.ProvinceCount * 2f; // $2M per tile
        
        float cityIncome = 0f;
        foreach (var city in world.Cities)
        {
            if (city.NationId == nation.Id)
            {
                cityIncome += city.IsCapital ? 150f : 50f;
            }
        }

        return baseIncome + provinceIncome + cityIncome;
    }
}
