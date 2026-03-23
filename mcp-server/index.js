#!/usr/bin/env node

/**
 * WARSHIP MCP Server
 * Bridges Claude AI (via stdio MCP protocol) to the Godot editor (via TCP on port 6030)
 * and the running game (via TCP on port 6031).
 *
 * Tools:
 *   Editor (port 6030):
 *     godot_ping, godot_list_scripts, godot_list_scenes, godot_get_project_info,
 *     godot_get_scene_tree, godot_run, godot_stop, godot_get_node_properties,
 *     godot_set_node_property, godot_add_node, godot_remove_node, godot_get_editor_log
 *   File I/O (local):
 *     read_game_file, write_game_file, edit_game_file, create_game_file
 *   Runtime (port 6031):
 *     game_ping, game_get_state, game_get_nations, game_get_nation,
 *     game_get_units, game_get_characters, game_get_turn, game_get_tile, game_get_region
 */

import { Server } from "@modelcontextprotocol/sdk/server/index.js";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import {
  CallToolRequestSchema,
  ListToolsRequestSchema,
} from "@modelcontextprotocol/sdk/types.js";
import net from "net";
import path from "path";
import fs from "fs";
import { fileURLToPath } from "url";

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

// Project root is one level up from mcp-server/
const PROJECT_ROOT = path.resolve(__dirname, "..");

const GODOT_PORT = 6030;
const RUNTIME_PORT = 6031;
const TCP_HOST = "127.0.0.1";

// ─── TCP Communication ─────────────────────────────────

function sendTcp(port, method, params = {}, timeoutMsg = "") {
  return new Promise((resolve, reject) => {
    const client = new net.Socket();
    const timeout = setTimeout(() => {
      client.destroy();
      reject(new Error(timeoutMsg || `TCP connection timeout on port ${port}`));
    }, 5000);

    client.connect(port, TCP_HOST, () => {
      const request = JSON.stringify({
        jsonrpc: "2.0",
        id: 1,
        method,
        params,
      }) + "\n";
      client.write(request);
    });

    let data = "";
    client.on("data", (chunk) => {
      data += chunk.toString();
      if (data.includes("\n")) {
        clearTimeout(timeout);
        client.destroy();
        try {
          const response = JSON.parse(data.trim());
          resolve(response.result || response);
        } catch (e) {
          reject(new Error(`Invalid JSON response: ${data}`));
        }
      }
    });

    client.on("error", (err) => {
      clearTimeout(timeout);
      reject(new Error(timeoutMsg || `Cannot connect on port ${port}: ${err.message}`));
    });
  });
}

function sendToGodot(method, params = {}) {
  return sendTcp(GODOT_PORT, method, params,
    "Godot editor not reachable on port 6030. Is it running with the MCP plugin enabled?");
}

function sendToRuntime(method, params = {}) {
  return sendTcp(RUNTIME_PORT, method, params,
    "Game is not running or RuntimeBridge is not loaded (port 6031).");
}

// ─── Path Safety ────────────────────────────────────────

const PROTECTED_FILES = ["project.godot", "addons/godot_mcp/plugin.cfg"];

function validatePath(relPath) {
  if (!relPath || typeof relPath !== "string") {
    return { ok: false, error: "Path is required" };
  }
  if (path.isAbsolute(relPath)) {
    return { ok: false, error: "Absolute paths are not allowed" };
  }
  if (relPath.includes("..")) {
    return { ok: false, error: "Path traversal (..) is not allowed" };
  }
  const resolved = path.resolve(PROJECT_ROOT, relPath);
  if (!resolved.startsWith(PROJECT_ROOT)) {
    return { ok: false, error: "Path resolves outside project directory" };
  }
  const normalized = path.relative(PROJECT_ROOT, resolved);
  if (PROTECTED_FILES.includes(normalized)) {
    return { ok: false, error: `Protected file cannot be written: ${normalized}` };
  }
  return { ok: true, resolved, normalized };
}

// ─── MCP Server Setup ───────────────────────────────────

const server = new Server(
  { name: "warship-godot-mcp", version: "2.0.0" },
  { capabilities: { tools: {} } }
);

// List available tools
server.setRequestHandler(ListToolsRequestSchema, async () => ({
  tools: [
    // ── Editor tools (port 6030) ──
    {
      name: "godot_ping",
      description: "Check if the Godot editor is connected and responsive",
      inputSchema: { type: "object", properties: {} },
    },
    {
      name: "godot_list_scripts",
      description: "List all C# script files in the project",
      inputSchema: { type: "object", properties: {} },
    },
    {
      name: "godot_list_scenes",
      description: "List all .tscn scene files in the project",
      inputSchema: { type: "object", properties: {} },
    },
    {
      name: "godot_get_project_info",
      description: "Get project name and main scene path",
      inputSchema: { type: "object", properties: {} },
    },
    {
      name: "godot_get_scene_tree",
      description: "Get the node tree of the currently open scene in the editor",
      inputSchema: { type: "object", properties: {} },
    },
    {
      name: "godot_run",
      description: "Run the main scene in the Godot editor",
      inputSchema: { type: "object", properties: {} },
    },
    {
      name: "godot_stop",
      description: "Stop the currently running game in the Godot editor",
      inputSchema: { type: "object", properties: {} },
    },
    {
      name: "godot_get_node_properties",
      description: "Get properties of a node in the currently edited scene",
      inputSchema: {
        type: "object",
        properties: {
          nodePath: { type: "string", description: "Path to node relative to scene root (e.g. 'UILayer/TopBar')" },
        },
        required: ["nodePath"],
      },
    },
    {
      name: "godot_set_node_property",
      description: "Set a property on a node in the currently edited scene",
      inputSchema: {
        type: "object",
        properties: {
          nodePath: { type: "string", description: "Path to node relative to scene root" },
          property: { type: "string", description: "Property name (e.g. 'position', 'visible', 'modulate')" },
          value: { description: "Value to set (number, string, bool, or array for Vector2/Color)" },
        },
        required: ["nodePath", "property", "value"],
      },
    },
    {
      name: "godot_add_node",
      description: "Add a new node to the currently edited scene",
      inputSchema: {
        type: "object",
        properties: {
          parentPath: { type: "string", description: "Path to parent node (e.g. 'UILayer' or '.' for scene root)" },
          type: { type: "string", description: "Godot node type (e.g. 'Node2D', 'Sprite2D', 'Control')" },
          name: { type: "string", description: "Name for the new node" },
        },
        required: ["parentPath", "type", "name"],
      },
    },
    {
      name: "godot_remove_node",
      description: "Remove a node from the currently edited scene",
      inputSchema: {
        type: "object",
        properties: {
          nodePath: { type: "string", description: "Path to the node to remove" },
        },
        required: ["nodePath"],
      },
    },
    {
      name: "godot_get_editor_log",
      description: "Get recent lines from the Godot editor log",
      inputSchema: {
        type: "object",
        properties: {
          lines: { type: "number", description: "Number of lines to return (default 100)" },
        },
      },
    },

    // ── File I/O tools (local filesystem) ──
    {
      name: "read_game_file",
      description: "Read a file from the WARSHIP project directory",
      inputSchema: {
        type: "object",
        properties: {
          path: { type: "string", description: "Relative path from project root (e.g. 'src/Core/EventBus.cs')" },
        },
        required: ["path"],
      },
    },
    {
      name: "write_game_file",
      description: "Write (overwrite) a file in the WARSHIP project directory",
      inputSchema: {
        type: "object",
        properties: {
          path: { type: "string", description: "Relative path from project root" },
          content: { type: "string", description: "File content to write" },
        },
        required: ["path", "content"],
      },
    },
    {
      name: "edit_game_file",
      description: "Apply find-and-replace edits to a file in the WARSHIP project",
      inputSchema: {
        type: "object",
        properties: {
          path: { type: "string", description: "Relative path from project root" },
          edits: {
            type: "array",
            description: "Array of {oldText, newText} replacements applied sequentially",
            items: {
              type: "object",
              properties: {
                oldText: { type: "string" },
                newText: { type: "string" },
              },
              required: ["oldText", "newText"],
            },
          },
        },
        required: ["path", "edits"],
      },
    },
    {
      name: "create_game_file",
      description: "Create a new file in the WARSHIP project (fails if file already exists)",
      inputSchema: {
        type: "object",
        properties: {
          path: { type: "string", description: "Relative path from project root" },
          content: { type: "string", description: "File content" },
        },
        required: ["path", "content"],
      },
    },

    // ── Runtime tools (port 6031, requires game to be running) ──
    {
      name: "game_ping",
      description: "Check if the game process is running and RuntimeBridge is responsive",
      inputSchema: { type: "object", properties: {} },
    },
    {
      name: "game_get_state",
      description: "Get full world state snapshot (nations, cities, units, characters, turn info)",
      inputSchema: { type: "object", properties: {} },
    },
    {
      name: "game_get_nations",
      description: "List all nations with their stats",
      inputSchema: { type: "object", properties: {} },
    },
    {
      name: "game_get_nation",
      description: "Get detailed info for a specific nation including its cities and units",
      inputSchema: {
        type: "object",
        properties: {
          nationId: { type: "string", description: "Nation ID" },
        },
        required: ["nationId"],
      },
    },
    {
      name: "game_get_units",
      description: "List units, optionally filtered by nation",
      inputSchema: {
        type: "object",
        properties: {
          nationId: { type: "string", description: "Optional nation ID to filter by" },
        },
      },
    },
    {
      name: "game_get_characters",
      description: "List characters with authority meters, optionally filtered by nation",
      inputSchema: {
        type: "object",
        properties: {
          nationId: { type: "string", description: "Optional nation ID to filter by" },
        },
      },
    },
    {
      name: "game_get_turn",
      description: "Get current turn number, year, month, and player nation ID",
      inputSchema: { type: "object", properties: {} },
    },
    {
      name: "game_get_tile",
      description: "Inspect a specific map tile (terrain, owner, units)",
      inputSchema: {
        type: "object",
        properties: {
          x: { type: "number", description: "Tile X coordinate" },
          y: { type: "number", description: "Tile Y coordinate" },
        },
        required: ["x", "y"],
      },
    },
    {
      name: "game_get_region",
      description: "Get terrain and ownership for a rectangular map region (max 20x20)",
      inputSchema: {
        type: "object",
        properties: {
          x: { type: "number", description: "Top-left X" },
          y: { type: "number", description: "Top-left Y" },
          w: { type: "number", description: "Width (max 20)" },
          h: { type: "number", description: "Height (max 20)" },
        },
        required: ["x", "y", "w", "h"],
      },
    },
  ],
}));

// Handle tool calls
server.setRequestHandler(CallToolRequestSchema, async (request) => {
  const { name, arguments: args } = request.params;

  try {
    switch (name) {
      // ── Editor tools ──
      case "godot_ping": {
        const result = await sendToGodot("ping");
        return { content: [{ type: "text", text: JSON.stringify(result, null, 2) }] };
      }
      case "godot_list_scripts": {
        const result = await sendToGodot("list_scripts");
        return { content: [{ type: "text", text: JSON.stringify(result, null, 2) }] };
      }
      case "godot_list_scenes": {
        const result = await sendToGodot("list_scenes");
        return { content: [{ type: "text", text: JSON.stringify(result, null, 2) }] };
      }
      case "godot_get_project_info": {
        const result = await sendToGodot("get_project_info");
        return { content: [{ type: "text", text: JSON.stringify(result, null, 2) }] };
      }
      case "godot_get_scene_tree": {
        const result = await sendToGodot("get_scene_tree");
        return { content: [{ type: "text", text: JSON.stringify(result, null, 2) }] };
      }
      case "godot_run": {
        const result = await sendToGodot("run_project");
        return { content: [{ type: "text", text: JSON.stringify(result, null, 2) }] };
      }
      case "godot_stop": {
        const result = await sendToGodot("stop_project");
        return { content: [{ type: "text", text: JSON.stringify(result, null, 2) }] };
      }
      case "godot_get_node_properties": {
        const result = await sendToGodot("get_node_properties", { nodePath: args.nodePath });
        return { content: [{ type: "text", text: JSON.stringify(result, null, 2) }] };
      }
      case "godot_set_node_property": {
        const result = await sendToGodot("set_node_property", {
          nodePath: args.nodePath,
          property: args.property,
          value: args.value,
        });
        return { content: [{ type: "text", text: JSON.stringify(result, null, 2) }] };
      }
      case "godot_add_node": {
        const result = await sendToGodot("add_node", {
          parentPath: args.parentPath,
          nodeType: args.type,
          nodeName: args.name,
        });
        return { content: [{ type: "text", text: JSON.stringify(result, null, 2) }] };
      }
      case "godot_remove_node": {
        const result = await sendToGodot("remove_node", { nodePath: args.nodePath });
        return { content: [{ type: "text", text: JSON.stringify(result, null, 2) }] };
      }
      case "godot_get_editor_log": {
        const result = await sendToGodot("get_editor_log", { lines: args?.lines ?? 100 });
        return { content: [{ type: "text", text: JSON.stringify(result, null, 2) }] };
      }

      // ── File I/O tools ──
      case "read_game_file": {
        const v = validatePath(args.path);
        if (!v.ok) return { content: [{ type: "text", text: `Error: ${v.error}` }] };
        if (!fs.existsSync(v.resolved)) {
          return { content: [{ type: "text", text: `File not found: ${args.path}` }] };
        }
        const content = fs.readFileSync(v.resolved, "utf-8");
        return { content: [{ type: "text", text: content }] };
      }
      case "write_game_file": {
        const v = validatePath(args.path);
        if (!v.ok) return { content: [{ type: "text", text: `Error: ${v.error}` }] };
        fs.writeFileSync(v.resolved, args.content, "utf-8");
        return {
          content: [{ type: "text", text: JSON.stringify({
            status: "written", path: v.normalized, bytes: args.content.length,
          }, null, 2) }],
        };
      }
      case "edit_game_file": {
        const v = validatePath(args.path);
        if (!v.ok) return { content: [{ type: "text", text: `Error: ${v.error}` }] };
        if (!fs.existsSync(v.resolved)) {
          return { content: [{ type: "text", text: `File not found: ${args.path}` }] };
        }
        let content = fs.readFileSync(v.resolved, "utf-8");
        let applied = 0;
        const failed = [];
        for (const edit of args.edits) {
          if (content.includes(edit.oldText)) {
            content = content.replace(edit.oldText, edit.newText);
            applied++;
          } else {
            failed.push(edit.oldText.substring(0, 60));
          }
        }
        fs.writeFileSync(v.resolved, content, "utf-8");
        return {
          content: [{ type: "text", text: JSON.stringify({
            status: "edited", path: v.normalized, appliedCount: applied, failedEdits: failed,
          }, null, 2) }],
        };
      }
      case "create_game_file": {
        const v = validatePath(args.path);
        if (!v.ok) return { content: [{ type: "text", text: `Error: ${v.error}` }] };
        if (fs.existsSync(v.resolved)) {
          return { content: [{ type: "text", text: `Error: File already exists: ${args.path}. Use write_game_file to overwrite.` }] };
        }
        const dir = path.dirname(v.resolved);
        fs.mkdirSync(dir, { recursive: true });
        fs.writeFileSync(v.resolved, args.content, "utf-8");
        return {
          content: [{ type: "text", text: JSON.stringify({
            status: "created", path: v.normalized,
          }, null, 2) }],
        };
      }

      // ── Runtime tools (game must be running) ──
      case "game_ping": {
        const result = await sendToRuntime("runtime_ping");
        return { content: [{ type: "text", text: JSON.stringify(result, null, 2) }] };
      }
      case "game_get_state": {
        const result = await sendToRuntime("get_world_state");
        return { content: [{ type: "text", text: JSON.stringify(result, null, 2) }] };
      }
      case "game_get_nations": {
        const result = await sendToRuntime("get_nations");
        return { content: [{ type: "text", text: JSON.stringify(result, null, 2) }] };
      }
      case "game_get_nation": {
        const result = await sendToRuntime("get_nation", { nationId: args.nationId });
        return { content: [{ type: "text", text: JSON.stringify(result, null, 2) }] };
      }
      case "game_get_units": {
        const result = await sendToRuntime("get_units", { nationId: args?.nationId });
        return { content: [{ type: "text", text: JSON.stringify(result, null, 2) }] };
      }
      case "game_get_characters": {
        const result = await sendToRuntime("get_characters", { nationId: args?.nationId });
        return { content: [{ type: "text", text: JSON.stringify(result, null, 2) }] };
      }
      case "game_get_turn": {
        const result = await sendToRuntime("get_turn_info");
        return { content: [{ type: "text", text: JSON.stringify(result, null, 2) }] };
      }
      case "game_get_tile": {
        const result = await sendToRuntime("get_map_tile", { x: args.x, y: args.y });
        return { content: [{ type: "text", text: JSON.stringify(result, null, 2) }] };
      }
      case "game_get_region": {
        const result = await sendToRuntime("get_map_region", {
          x: args.x, y: args.y, w: args.w, h: args.h,
        });
        return { content: [{ type: "text", text: JSON.stringify(result, null, 2) }] };
      }

      default:
        return { content: [{ type: "text", text: `Unknown tool: ${name}` }] };
    }
  } catch (error) {
    return { content: [{ type: "text", text: `Error: ${error.message}` }] };
  }
});

// Start the server
const transport = new StdioServerTransport();
await server.connect(transport);
