using System;

namespace Warship.Core;

/// <summary>
/// Deterministic seeded RNG for all simulation randomness.
/// Ensures replays produce identical outcomes.
/// </summary>
public static class SimRng
{
    private static Random _rng = new(42);

    /// <summary>Initialize with a specific seed (called during world gen).</summary>
    public static void Init(int seed)
    {
        _rng = new Random(seed);
    }

    public static float NextFloat() => (float)_rng.NextDouble();
    public static double NextDouble() => _rng.NextDouble();
    public static int Next(int maxExclusive) => _rng.Next(maxExclusive);
    public static int Next(int minInclusive, int maxExclusive) => _rng.Next(minInclusive, maxExclusive);
}
