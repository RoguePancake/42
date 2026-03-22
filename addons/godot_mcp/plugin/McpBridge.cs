/*  McpBridge.cs  –  Godot 4 Editor Plugin
 *  Starts a lightweight TCP server inside the editor so an external
 *  MCP server process can query / control the running project.
 *
 *  Protocol:  JSON-RPC 2.0 over TCP  (one JSON object per line).
 *  Default port: 6030  (configurable via Editor Settings).
 */

#if TOOLS
using Godot;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Warship;

[Tool]
public partial class McpBridge : EditorPlugin
{
    private TcpServer? _server;
    private StreamPeerTcp? _peer;
    private int _port = 6030;

    public override void _EnterTree()
    {
        _server = new TcpServer();
        var err = _server.Listen((ushort)_port);
        if (err != Error.Ok)
        {
            GD.PrintErr($"[MCP] Failed to listen on port {_port}: {err}");
            return;
        }
        GD.Print($"[MCP] Bridge listening on tcp://127.0.0.1:{_port}");
    }

    public override void _ExitTree()
    {
        _peer?.DisconnectFromHost();
        _server?.Stop();
        GD.Print("[MCP] Bridge stopped.");
    }

    public override void _Process(double delta)
    {
        if (_server == null) return;

        if (_server.IsConnectionAvailable())
        {
            _peer?.DisconnectFromHost();
            _peer = _server.TakeConnection();
            GD.Print("[MCP] Client connected.");
        }

        if (_peer == null || _peer.GetStatus() != StreamPeerTcp.Status.Connected)
            return;

        int avail = _peer.GetAvailableBytes();
        if (avail <= 0) return;

        byte[] data = _peer.GetData(avail)[1].AsByteArray();
        string raw = Encoding.UTF8.GetString(data);

        foreach (string line in raw.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
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
                GD.PrintErr($"[MCP] Error: {ex.Message}");
            }
        }
    }

    private Dictionary<string, object?> HandleMethod(string method, JsonElement request)
    {
        return method switch
        {
            "ping" => new() { ["status"] = "ok", ["engine"] = "Godot 4", ["project"] = "WARSHIP" },

            "get_project_info" => new()
            {
                ["name"] = ProjectSettings.GetSetting("application/config/name").ToString(),
                ["main_scene"] = ProjectSettings.GetSetting("application/run/main_scene").ToString(),
            },

            "list_scenes" => new()
            {
                ["scenes"] = ListFilesRecursive("res://scenes", "*.tscn"),
            },

            "list_scripts" => new()
            {
                ["scripts"] = ListFilesRecursive("res://src", "*.cs"),
            },

            "get_scene_tree" => GetSceneTreeInfo(),

            "run_project" => RunProject(),

            "stop_project" => StopProject(),

            _ => new() { ["error"] = $"Unknown method: {method}" },
        };
    }

    private void SendResponse(int id, Dictionary<string, object?> result)
    {
        var response = new Dictionary<string, object?>
        {
            ["jsonrpc"] = "2.0",
            ["id"] = id,
            ["result"] = result,
        };
        string json = JsonSerializer.Serialize(response) + "\n";
        _peer?.PutData(Encoding.UTF8.GetBytes(json));
    }

    private List<string> ListFilesRecursive(string path, string pattern)
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
                files.AddRange(ListFilesRecursive(fullPath, pattern));
            else if (fileName.EndsWith(pattern.Replace("*", "")))
                files.Add(fullPath);
            fileName = dir.GetNext();
        }
        return files;
    }

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
}
#endif
