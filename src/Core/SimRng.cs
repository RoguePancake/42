using System;

namespace Warship.Core;

/// <summary>
/// Seeded deterministic RNG. Use this for ALL randomness in the game.
/// Same seed = same results = deterministic replays.
/// </summary>
public class SimRng
{
    private int _state;

    public SimRng(int seed) => _state = seed;

    public int Seed => _state;

    /// <summary>Next integer (full range).</summary>
    public int Next()
    {
        _state ^= _state << 13;
        _state ^= _state >> 17;
        _state ^= _state << 5;
        return _state;
    }

    /// <summary>Next integer in [0, max).</summary>
    public int Next(int max) => Math.Abs(Next()) % max;

    /// <summary>Next integer in [min, max).</summary>
    public int Next(int min, int max) => min + Math.Abs(Next()) % (max - min);

    /// <summary>Next float in [0, 1).</summary>
    public float NextFloat() => (Math.Abs(Next()) & 0x7FFFFFFF) / (float)0x7FFFFFFF;

    /// <summary>Next float in [min, max).</summary>
    public float NextFloat(float min, float max) => min + NextFloat() * (max - min);

    /// <summary>Deterministic hash: (seed, index) → float in [0,1).</summary>
    public static float Hash(int seed, int index)
    {
        int h = seed ^ (index * 374761393);
        h = (h ^ (h >> 13)) * 1274126177;
        h = h ^ (h >> 16);
        return (h & 0x7FFFFFFF) / (float)0x7FFFFFFF;
    }

    /// <summary>Deterministic hash: (seed, index) → positive int.</summary>
    public static int HashInt(int seed, int index)
    {
        int h = seed ^ (index * 374761393);
        h = (h ^ (h >> 13)) * 1274126177;
        return Math.Abs(h ^ (h >> 16));
    }
}
