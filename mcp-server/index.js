#!/usr/bin/env node

/**
 * WARSHIP MCP Server
 * Bridges Claude AI (via stdio MCP protocol) to the Godot editor (via TCP on port 6030).
 * 
 * Usage:
 *   node index.js
 * 
 * The server exposes tools that Claude can call:
 *   - godot_ping: Check if Godot editor is connected
 *   - godot_list_scripts: List all C# scripts in the project
 *   - godot_list_scenes: List all .tscn scene files
 *   - godot_get_project_info: Get project name, main scene
 *   - godot_get_scene_tree: Get the node tree of the open scene
 *   - godot_run: Run the game in the editor
 *   - godot_stop: Stop the running game
 *   - read_game_file: Read any file from the project directory
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
const GODOT_HOST = "127.0.0.1";

// ─── Godot TCP Communication ────────────────────────────

function sendToGodot(method, params = {}) {
  return new Promise((resolve, reject) => {
    const client = new net.Socket();
    const timeout = setTimeout(() => {
      client.destroy();
      reject(new Error("Godot connection timeout (is the editor running with MCP plugin enabled?)"));
    }, 5000);

    client.connect(GODOT_PORT, GODOT_HOST, () => {
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
          reject(new Error(`Invalid JSON from Godot: ${data}`));
        }
      }
    });

    client.on("error", (err) => {
      clearTimeout(timeout);
      reject(new Error(`Cannot connect to Godot editor on port ${GODOT_PORT}. Is it running?`));
    });
  });
}

// ─── MCP Server Setup ───────────────────────────────────

const server = new Server(
  { name: "warship-godot-mcp", version: "1.0.0" },
  { capabilities: { tools: {} } }
);

// List available tools
server.setRequestHandler(ListToolsRequestSchema, async () => ({
  tools: [
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
      name: "read_game_file",
      description: "Read a file from the WARSHIP project directory",
      inputSchema: {
        type: "object",
        properties: {
          path: {
            type: "string",
            description: "Relative path from project root (e.g. 'src/Core/EventBus.cs')",
          },
        },
        required: ["path"],
      },
    },
  ],
}));

// Handle tool calls
server.setRequestHandler(CallToolRequestSchema, async (request) => {
  const { name, arguments: args } = request.params;

  try {
    switch (name) {
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
      case "read_game_file": {
        const filePath = path.resolve(PROJECT_ROOT, args.path);
        // Safety: don't allow reading outside project
        if (!filePath.startsWith(PROJECT_ROOT)) {
          return { content: [{ type: "text", text: "Error: path outside project directory" }] };
        }
        if (!fs.existsSync(filePath)) {
          return { content: [{ type: "text", text: `File not found: ${args.path}` }] };
        }
        const content = fs.readFileSync(filePath, "utf-8");
        return { content: [{ type: "text", text: content }] };
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
