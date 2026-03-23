/*  McpBridge.cs  –  Godot 4.5 Editor Plugin
 *  Uses System.Net instead of Godot networking to avoid API breaks.
 *  TCP server on port 6030 for MCP communication.
 *
 *  Capabilities:
 *    - ping, list_scripts, list_scenes, get_project_info, get_scene_tree
 *    - run_project, stop_project
 *    - get_node_properties, set_node_property, add_node, remove_node
 *    - get_editor_log
 */

#if TOOLS
using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace Warship;

[Tool]
public partial class McpBridge : EditorPlugin
{
    private TcpListener? _listener;
    private TcpClient? _client;
    private NetworkStream? _stream;
    private int _port = 6030;
    private byte[] _buffer = new byte[16384];
    private string _partial = "";

    public override void _EnterTree()
    {
        try
        {
            _listener = new TcpListener(IPAddress.Loopback, _port);
            _listener.Start();
            _listener.Server.Blocking = false;
            GD.Print($"[MCP] Bridge listening on tcp://127.0.0.1:{_port}");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[MCP] Failed to listen on port {_port}: {ex.Message}");
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
        GD.Print("[MCP] Bridge stopped.");
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
                GD.Print("[MCP] Client connected.");
            }
        }
        catch { }

        if (_client == null || !_client.Connected || _stream == null)
            return;

        // Read available data (non-blocking)
        try
        {
            if (!_stream.DataAvailable) return;

            int bytesRead = _stream.Read(_buffer, 0, _buffer.Length);
            if (bytesRead <= 0) return;

            _partial += Encoding.UTF8.GetString(_buffer, 0, bytesRead);

            // Process complete lines
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
                    var result = HandleMethod(method, request);
                    SendResponse(id, result);
                }
                catch (Exception ex)
                {
                    GD.PrintErr($"[MCP] Parse error: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[MCP] Read error: {ex.Message}");
            _stream?.Close();
            _client?.Close();
            _client = null;
            _stream = null;
        }
    }

    // ─── Helpers ────────────────────────────────────────

    private JsonElement GetParams(JsonElement request)
    {
        return request.TryGetProperty("params", out var p) ? p : default;
    }

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

    /// <summary>Convert a JSON value to a Godot Variant for node.Set().</summary>
    private Variant ConvertJsonToVariant(JsonElement value)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.True: return true;
            case JsonValueKind.False: return false;
            case JsonValueKind.String: return value.GetString() ?? "";
            case JsonValueKind.Number:
                if (value.TryGetInt32(out int i)) return i;
                return (float)value.GetDouble();
            case JsonValueKind.Array:
                int len = value.GetArrayLength();
                if (len == 2)
                    return new Vector2((float)value[0].GetDouble(), (float)value[1].GetDouble());
                if (len == 4)
                    return new Color((float)value[0].GetDouble(), (float)value[1].GetDouble(),
                                     (float)value[2].GetDouble(), (float)value[3].GetDouble());
                if (len == 3)
                    return new Vector3((float)value[0].GetDouble(), (float)value[1].GetDouble(),
                                      (float)value[2].GetDouble());
                return value.ToString();
            default:
                return value.ToString();
        }
    }

    // ─── Method Router ──────────────────────────────────

    private Dictionary<string, object?> HandleMethod(string method, JsonElement request)
    {
        var parms = GetParams(request);

        return method switch
        {
            "ping" => new() { ["status"] = "ok", ["engine"] = "Godot 4.5", ["project"] = "WARSHIP" },

            "get_project_info" => new()
            {
                ["name"] = ProjectSettings.GetSetting("application/config/name").ToString(),
                ["main_scene"] = ProjectSettings.GetSetting("application/run/main_scene").ToString(),
            },

            "list_scenes" => new() { ["scenes"] = ListFilesRecursive("res://scenes", ".tscn") },
            "list_scripts" => new() { ["scripts"] = ListFilesRecursive("res://src", ".cs") },
            "get_scene_tree" => GetSceneTreeInfo(),
            "run_project" => RunProject(),
            "stop_project" => StopProject(),

            // Scene node manipulation
            "get_node_properties" => GetNodeProperties(parms),
            "set_node_property" => SetNodeProperty(parms),
            "add_node" => AddNode(parms),
            "remove_node" => RemoveNode(parms),

            // Editor log
            "get_editor_log" => GetEditorLog(parms),

            _ => new() { ["error"] = $"Unknown method: {method}" },
        };
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

    // ─── File Listing ───────────────────────────────────

    private List<string> ListFilesRecursive(string path, string ext)
    {
        var files = new List<string>();
        var dir = DirAccess.Open(path);
        if (dir == null) return files;

        dir.ListDirBegin();
        string fileName = dir.GetNext();
        while (fileName != "")
        {
            string fullPath = $"{path}/{fileName}";
            if (dir.CurrentIsDir())
                files.AddRange(ListFilesRecursive(fullPath, ext));
            else if (fileName.EndsWith(ext))
                files.Add(fullPath);
            fileName = dir.GetNext();
        }
        return files;
    }

    // ─── Scene Tree ─────────────────────────────────────

    private Dictionary<string, object?> GetSceneTreeInfo()
    {
        var tree = EditorInterface.Singleton.GetEditedSceneRoot();
        if (tree == null)
            return new() { ["error"] = "No scene open in editor" };

        return new() { ["root"] = DescribeNode(tree) };
    }

    private Dictionary<string, object?> DescribeNode(Node node)
    {
        var children = new List<Dictionary<string, object?>>();
        foreach (var child in node.GetChildren())
            children.Add(DescribeNode(child));

        return new()
        {
            ["name"] = node.Name.ToString(),
            ["type"] = node.GetClass(),
            ["children"] = children,
        };
    }

    // ─── Run / Stop ─────────────────────────────────────

    private Dictionary<string, object?> RunProject()
    {
        EditorInterface.Singleton.PlayMainScene();
        return new() { ["status"] = "running" };
    }

    private Dictionary<string, object?> StopProject()
    {
        EditorInterface.Singleton.StopPlayingScene();
        return new() { ["status"] = "stopped" };
    }

    // ─── Node Properties ────────────────────────────────

    private Dictionary<string, object?> GetNodeProperties(JsonElement parms)
    {
        var root = EditorInterface.Singleton.GetEditedSceneRoot();
        if (root == null)
            return new() { ["error"] = "No scene open in editor" };

        string nodePath = GetStringParam(parms, "nodePath");
        if (string.IsNullOrEmpty(nodePath))
            return new() { ["error"] = "nodePath is required" };

        Node? node = nodePath == "." ? root : root.GetNodeOrNull(nodePath);
        if (node == null)
            return new() { ["error"] = $"Node not found: {nodePath}" };

        var props = new Dictionary<string, object?>();
        props["_name"] = node.Name.ToString();
        props["_type"] = node.GetClass();

        // Extract commonly useful properties
        var propList = node.GetPropertyList();
        var interestingCategories = new HashSet<string> { "Node", "Node2D", "Control", "CanvasItem" };

        foreach (var propDict in propList)
        {
            string propName = propDict["name"].AsString();

            // Skip internal/private properties
            if (propName.StartsWith("_") || propName.Contains("/")) continue;

            // Include key properties
            var include = propName switch
            {
                "position" or "rotation" or "scale" or "visible" or "modulate" or
                "self_modulate" or "z_index" or "size" or "offset" or
                "anchor_left" or "anchor_right" or "anchor_top" or "anchor_bottom" or
                "text" or "texture" or "anchors_preset" or "layout_mode" or
                "mouse_filter" or "custom_minimum_size" => true,
                _ => false,
            };
            if (!include) continue;

            try
            {
                var val = node.Get(propName);
                props[propName] = VariantToSerializable(val);
            }
            catch { }
        }

        return props;
    }

    private object? VariantToSerializable(Variant val)
    {
        switch (val.VariantType)
        {
            case Variant.Type.Bool: return val.AsBool();
            case Variant.Type.Int: return val.AsInt32();
            case Variant.Type.Float: return val.AsDouble();
            case Variant.Type.String: return val.AsString();
            case Variant.Type.Vector2:
                var v2 = val.AsVector2();
                return new float[] { v2.X, v2.Y };
            case Variant.Type.Vector3:
                var v3 = val.AsVector3();
                return new float[] { v3.X, v3.Y, v3.Z };
            case Variant.Type.Color:
                var c = val.AsColor();
                return new float[] { c.R, c.G, c.B, c.A };
            case Variant.Type.Nil: return null;
            default: return val.ToString();
        }
    }

    private Dictionary<string, object?> SetNodeProperty(JsonElement parms)
    {
        var root = EditorInterface.Singleton.GetEditedSceneRoot();
        if (root == null)
            return new() { ["error"] = "No scene open in editor" };

        string nodePath = GetStringParam(parms, "nodePath");
        string property = GetStringParam(parms, "property");

        if (string.IsNullOrEmpty(nodePath) || string.IsNullOrEmpty(property))
            return new() { ["error"] = "nodePath and property are required" };

        Node? node = nodePath == "." ? root : root.GetNodeOrNull(nodePath);
        if (node == null)
            return new() { ["error"] = $"Node not found: {nodePath}" };

        if (!parms.TryGetProperty("value", out var valueEl))
            return new() { ["error"] = "value is required" };

        var variant = ConvertJsonToVariant(valueEl);
        node.Set(property, variant);

        return new()
        {
            ["status"] = "ok",
            ["node"] = nodePath,
            ["property"] = property,
            ["value"] = VariantToSerializable(node.Get(property)),
        };
    }

    // ─── Add / Remove Nodes ─────────────────────────────

    private Dictionary<string, object?> AddNode(JsonElement parms)
    {
        var root = EditorInterface.Singleton.GetEditedSceneRoot();
        if (root == null)
            return new() { ["error"] = "No scene open in editor" };

        string parentPath = GetStringParam(parms, "parentPath", ".");
        string nodeType = GetStringParam(parms, "nodeType");
        string nodeName = GetStringParam(parms, "nodeName");

        if (string.IsNullOrEmpty(nodeType) || string.IsNullOrEmpty(nodeName))
            return new() { ["error"] = "nodeType and nodeName are required" };

        Node? parent = parentPath == "." ? root : root.GetNodeOrNull(parentPath);
        if (parent == null)
            return new() { ["error"] = $"Parent node not found: {parentPath}" };

        // Create the node
        if (!ClassDB.ClassExists(nodeType))
            return new() { ["error"] = $"Unknown node type: {nodeType}" };

        if (!ClassDB.CanInstantiate(nodeType))
            return new() { ["error"] = $"Cannot instantiate: {nodeType}" };

        var newNode = ClassDB.Instantiate(nodeType).As<Node>();
        if (newNode == null)
            return new() { ["error"] = $"Failed to instantiate: {nodeType}" };

        newNode.Name = nodeName;
        parent.AddChild(newNode);
        newNode.Owner = root; // Required for the node to be saved with the scene

        return new()
        {
            ["status"] = "ok",
            ["name"] = nodeName,
            ["type"] = nodeType,
            ["parent"] = parentPath,
        };
    }

    private Dictionary<string, object?> RemoveNode(JsonElement parms)
    {
        var root = EditorInterface.Singleton.GetEditedSceneRoot();
        if (root == null)
            return new() { ["error"] = "No scene open in editor" };

        string nodePath = GetStringParam(parms, "nodePath");
        if (string.IsNullOrEmpty(nodePath))
            return new() { ["error"] = "nodePath is required" };

        if (nodePath == ".")
            return new() { ["error"] = "Cannot remove the scene root" };

        Node? node = root.GetNodeOrNull(nodePath);
        if (node == null)
            return new() { ["error"] = $"Node not found: {nodePath}" };

        string name = node.Name.ToString();
        node.GetParent()?.RemoveChild(node);
        node.QueueFree();

        return new() { ["status"] = "ok", ["removed"] = name };
    }

    // ─── Editor Log ─────────────────────────────────────

    private Dictionary<string, object?> GetEditorLog(JsonElement parms)
    {
        int lineCount = GetIntParam(parms, "lines", 100);
        if (lineCount <= 0) lineCount = 100;
        if (lineCount > 1000) lineCount = 1000;

        try
        {
            string logDir = ProjectSettings.GlobalizePath("user://logs/");
            if (!System.IO.Directory.Exists(logDir))
                return new() { ["error"] = $"Log directory not found: {logDir}" };

            // Find the most recent log file
            var logFiles = System.IO.Directory.GetFiles(logDir, "*.log")
                .OrderByDescending(f => System.IO.File.GetLastWriteTime(f))
                .ToArray();

            if (logFiles.Length == 0)
                return new() { ["error"] = "No log files found" };

            string logFile = logFiles[0];
            string[] allLines = System.IO.File.ReadAllLines(logFile);

            // Return the last N lines
            int skip = Math.Max(0, allLines.Length - lineCount);
            var lines = allLines.Skip(skip).ToList();

            return new()
            {
                ["file"] = System.IO.Path.GetFileName(logFile),
                ["totalLines"] = allLines.Length,
                ["returnedLines"] = lines.Count,
                ["lines"] = lines,
            };
        }
        catch (Exception ex)
        {
            return new() { ["error"] = $"Failed to read editor log: {ex.Message}" };
        }
    }
}
#endif
