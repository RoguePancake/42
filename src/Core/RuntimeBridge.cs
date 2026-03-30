/*  RuntimeBridge.cs  –  Live Game State Inspector
 *  TCP server on port 6031, runs inside the GAME process (not the editor).
 *  Exposes WorldStateManager data for MCP queries while the game is running.
 */

using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace Warship.Core;

public partial class RuntimeBridge : Node
{
    private TcpListener? _listener;
    private TcpClient? _client;
    private NetworkStream? _stream;
    private const int Port = 6031;
    private readonly byte[] _buffer = new byte[65536];
    private string _partial = "";

    public override void _Ready()
    {
        try
        {
            _listener = new TcpListener(IPAddress.Loopback, Port);
            _listener.Start();
            _listener.Server.Blocking = false;
            GD.Print($"[RuntimeBridge] Listening on tcp://127.0.0.1:{Port}");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[RuntimeBridge] Failed to listen on port {Port}: {ex.Message}");
        }
    }

    public override void _ExitTree()
    {
        try
        {
            _stream?.Close();
            _client?.Close();
            _listener?.Stop();
        }
        catch { }
        GD.Print("[RuntimeBridge] Stopped.");
    }

    public override void _Process(double delta)
    {
        if (_listener == null) return;

        // Accept new connections (non-blocking)
        try
        {
            if (_listener.Pending())
            {
                _stream?.Close();
                _client?.Close();
                _client = _listener.AcceptTcpClient();
                _client.NoDelay = true;
                _stream = _client.GetStream();
            }
        }
        catch { }

        if (_client == null || !_client.Connected || _stream == null)
            return;

        try
        {
            if (!_stream.DataAvailable) return;

            int bytesRead = _stream.Read(_buffer, 0, _buffer.Length);
            if (bytesRead <= 0) return;

            _partial += Encoding.UTF8.GetString(_buffer, 0, bytesRead);

            while (_partial.Contains('\n'))
            {
                int idx = _partial.IndexOf('\n');
                string line = _partial.Substring(0, idx).Trim();
                _partial = _partial.Substring(idx + 1);

                if (string.IsNullOrEmpty(line)) continue;

                try
                {
                    var request = JsonSerializer.Deserialize<JsonElement>(line);
                    string method = request.GetProperty("method").GetString() ?? "";
                    var id = request.TryGetProperty("id", out var idEl) ? idEl.GetInt32() : 0;
                    var parms = request.TryGetProperty("params", out var p) ? p : default;
                    var result = HandleMethod(method, parms);
                    SendResponse(id, result);
                }
                catch (Exception ex)
                {
                    GD.PrintErr($"[RuntimeBridge] Parse error: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[RuntimeBridge] Read error: {ex.Message}");
            _stream?.Close();
            _client?.Close();
            _client = null;
            _stream = null;
        }
    }

    // ─── Helpers ────────────────────────────────────────

    private string GetStringParam(JsonElement parms, string key, string fallback = "")
    {
        if (parms.ValueKind == JsonValueKind.Object && parms.TryGetProperty(key, out var v))
            return v.GetString() ?? fallback;
        return fallback;
    }

    private int GetIntParam(JsonElement parms, string key, int fallback = 0)
    {
        if (parms.ValueKind == JsonValueKind.Object && parms.TryGetProperty(key, out var v))
            return v.TryGetInt32(out int i) ? i : fallback;
        return fallback;
    }

    private void SendResponse(int id, Dictionary<string, object?> result)
    {
        if (_stream == null || !_stream.CanWrite) return;
        var response = new Dictionary<string, object?>
        {
            ["jsonrpc"] = "2.0",
            ["id"] = id,
            ["result"] = result,
        };
        string json = JsonSerializer.Serialize(response) + "\n";
        byte[] bytes = Encoding.UTF8.GetBytes(json);
        _stream.Write(bytes, 0, bytes.Length);
    }

    // ─── Method Router ──────────────────────────────────

    private Dictionary<string, object?> HandleMethod(string method, JsonElement parms)
    {
        var wsm = WorldStateManager.Instance;
        if (wsm == null)
            return new() { ["error"] = "WorldStateManager not initialized" };

        var data = wsm.Data;

        return method switch
        {
            "runtime_ping" => new()
            {
                ["status"] = "ok",
                ["turn"] = data.TurnNumber,
            },

            "get_turn_info" => new()
            {
                ["turn"] = data.TurnNumber,
                ["year"] = data.Year,
                ["month"] = data.Month,
                ["playerNationId"] = data.PlayerNationId,
            },

            "get_world_state" => GetWorldState(data),
            "get_nations" => GetNations(data),
            "get_nation" => GetNation(data, parms),
            "get_units" => GetUnits(data, parms),
            "get_characters" => GetCharacters(data, parms),
            "get_map_tile" => GetMapTile(data, parms),
            "get_map_region" => GetMapRegion(data, parms),

            _ => new() { ["error"] = $"Unknown method: {method}" },
        };
    }

    // ─── State Queries ──────────────────────────────────

    private Dictionary<string, object?> GetWorldState(Data.WorldData data)
    {
        return new()
        {
            ["turn"] = data.TurnNumber,
            ["year"] = data.Year,
            ["month"] = data.Month,
            ["playerNationId"] = data.PlayerNationId,
            ["mapWidth"] = data.MapWidth,
            ["mapHeight"] = data.MapHeight,
            ["nationCount"] = data.Nations.Count,
            ["cityCount"] = data.Cities.Count,
            ["armyCount"] = data.Armies.Count,
            ["characterCount"] = data.Characters.Count,
            ["nations"] = data.Nations.Select(SerializeNation).ToList(),
            ["cities"] = data.Cities.Select(SerializeCity).ToList(),
            ["armies"] = data.Armies.Where(a => a.IsAlive).Select(SerializeArmy).ToList(),
            ["characters"] = data.Characters.Select(SerializeCharacter).ToList(),
        };
    }

    private Dictionary<string, object?> GetNations(Data.WorldData data)
    {
        return new()
        {
            ["nations"] = data.Nations.Select(SerializeNation).ToList(),
        };
    }

    private Dictionary<string, object?> GetNation(Data.WorldData data, JsonElement parms)
    {
        string nationId = GetStringParam(parms, "nationId");
        if (string.IsNullOrEmpty(nationId))
            return new() { ["error"] = "nationId is required" };

        var nation = data.Nations.FirstOrDefault(n => n.Id == nationId);
        if (nation == null)
            return new() { ["error"] = $"Nation not found: {nationId}" };

        var cities = data.Cities.Where(c => c.NationId == nationId).Select(SerializeCity).ToList();
        var armies = data.Armies.Where(a => a.NationId == nationId && a.IsAlive).Select(SerializeArmy).ToList();
        var characters = data.Characters.Where(c => c.NationId == nationId).Select(SerializeCharacter).ToList();

        var result = SerializeNation(nation);
        result["cities"] = cities;
        result["armies"] = armies;
        result["characters"] = characters;
        return result;
    }

    private Dictionary<string, object?> GetUnits(Data.WorldData data, JsonElement parms)
    {
        string nationId = GetStringParam(parms, "nationId");
        var armies = data.Armies.Where(a => a.IsAlive);
        if (!string.IsNullOrEmpty(nationId))
            armies = armies.Where(a => a.NationId == nationId);

        return new()
        {
            ["armies"] = armies.Select(SerializeArmy).ToList(),
        };
    }

    private Dictionary<string, object?> GetCharacters(Data.WorldData data, JsonElement parms)
    {
        string nationId = GetStringParam(parms, "nationId");
        IEnumerable<Data.CharacterData> chars = data.Characters;
        if (!string.IsNullOrEmpty(nationId))
            chars = chars.Where(c => c.NationId == nationId);

        return new()
        {
            ["characters"] = chars.Select(SerializeCharacter).ToList(),
        };
    }

    private Dictionary<string, object?> GetMapTile(Data.WorldData data, JsonElement parms)
    {
        int x = GetIntParam(parms, "x", -1);
        int y = GetIntParam(parms, "y", -1);

        if (x < 0 || x >= data.MapWidth || y < 0 || y >= data.MapHeight)
            return new() { ["error"] = $"Tile ({x},{y}) out of bounds (map is {data.MapWidth}x{data.MapHeight})" };

        int terrain = data.TerrainMap![x, y];
        int owner = data.OwnershipMap![x, y];
        var armiesHere = data.Armies
            .Where(a => a.IsAlive && a.TileX == x && a.TileY == y)
            .Select(SerializeArmy).ToList();

        string? ownerNation = owner >= 0 && owner < data.Nations.Count
            ? data.Nations[owner].Id : null;

        return new()
        {
            ["x"] = x,
            ["y"] = y,
            ["terrain"] = terrain,
            ["terrainName"] = ((Data.TerrainType)terrain).ToString(),
            ["ownerId"] = ownerNation,
            ["armies"] = armiesHere,
        };
    }

    private Dictionary<string, object?> GetMapRegion(Data.WorldData data, JsonElement parms)
    {
        int x = GetIntParam(parms, "x");
        int y = GetIntParam(parms, "y");
        int w = Math.Min(GetIntParam(parms, "w", 10), 20);
        int h = Math.Min(GetIntParam(parms, "h", 10), 20);

        // Clamp to map bounds
        int x2 = Math.Min(x + w, data.MapWidth);
        int y2 = Math.Min(y + h, data.MapHeight);
        x = Math.Max(0, x);
        y = Math.Max(0, y);

        var terrain = new List<List<int>>();
        var ownership = new List<List<int>>();

        for (int ty = y; ty < y2; ty++)
        {
            var terrainRow = new List<int>();
            var ownerRow = new List<int>();
            for (int tx = x; tx < x2; tx++)
            {
                terrainRow.Add(data.TerrainMap![tx, ty]);
                ownerRow.Add(data.OwnershipMap![tx, ty]);
            }
            terrain.Add(terrainRow);
            ownership.Add(ownerRow);
        }

        return new()
        {
            ["x"] = x, ["y"] = y,
            ["w"] = x2 - x, ["h"] = y2 - y,
            ["terrain"] = terrain,
            ["ownership"] = ownership,
        };
    }

    // ─── Serialization Helpers ──────────────────────────

    private Dictionary<string, object?> SerializeNation(Data.NationData n)
    {
        return new()
        {
            ["id"] = n.Id,
            ["name"] = n.Name,
            ["archetype"] = n.Archetype.ToString(),
            ["color"] = new float[] { n.NationColor.R, n.NationColor.G, n.NationColor.B, n.NationColor.A },
            ["isPlayer"] = n.IsPlayer,
            ["capitalX"] = n.CapitalX,
            ["capitalY"] = n.CapitalY,
            ["provinceCount"] = n.ProvinceCount,
            ["treasury"] = n.Treasury,
            ["prestige"] = n.Prestige,
            ["militaryOrder"] = n.GlobalMilitaryOrder.ToString(),
        };
    }

    private Dictionary<string, object?> SerializeCity(Data.CityData c)
    {
        return new()
        {
            ["id"] = c.Id,
            ["nationId"] = c.NationId,
            ["name"] = c.Name,
            ["tileX"] = c.TileX,
            ["tileY"] = c.TileY,
            ["isCapital"] = c.IsCapital,
            ["size"] = c.Size,
        };
    }

    private Dictionary<string, object?> SerializeArmy(Data.ArmyData a)
    {
        return new()
        {
            ["id"] = a.Id,
            ["nationId"] = a.NationId,
            ["name"] = a.Name,
            ["tileX"] = a.TileX,
            ["tileY"] = a.TileY,
            ["totalStrength"] = a.TotalStrength,
            ["morale"] = a.Morale,
            ["supply"] = a.Supply,
            ["order"] = a.CurrentOrder.ToString(),
            ["formation"] = a.Formation.ToString(),
            ["primaryDomain"] = a.PrimaryDomain.ToString(),
        };
    }

    private Dictionary<string, object?> SerializeCharacter(Data.CharacterData c)
    {
        return new()
        {
            ["id"] = c.Id,
            ["nationId"] = c.NationId,
            ["name"] = c.Name,
            ["role"] = c.Role,
            ["isPlayer"] = c.IsPlayer,
            ["tileX"] = c.TileX,
            ["tileY"] = c.TileY,
        };
    }
}
