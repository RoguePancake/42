/*  McpBridge.cs  –  Godot 4.5 Editor Plugin
 *  Uses System.Net instead of Godot networking to avoid API breaks.
 *  TCP server on port 6030 for MCP communication.
 */

#if TOOLS
using Godot;
using System;
using System.Collections.Generic;
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
    private byte[] _buffer = new byte[4096];
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

    private Dictionary<string, object?> HandleMethod(string method, JsonElement request)
    {
        return method switch
        {
            "ping" => new() { ["status"] = "ok", ["engine"] = "Godot 4.5", ["project"] = "WARSHIP" },

            "get_project_info" => new()
            {
                ["name"] = ProjectSettings.GetSetting("application/config/name").ToString(),
                ["main_scene"] = ProjectSettings.GetSetting("application/run/main_scene").ToString(),
            },

            "list_scenes" => new()
            {
                ["scenes"] = ListFilesRecursive("res://scenes", ".tscn"),
            },

            "list_scripts" => new()
            {
                ["scripts"] = ListFilesRecursive("res://src", ".cs"),
            },

            "get_scene_tree" => GetSceneTreeInfo(),

            "run_project" => RunProject(),

            "stop_project" => StopProject(),

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
