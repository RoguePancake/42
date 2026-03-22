# MCP Setup Guide for WARSHIP

## What Is This?

The **MCP (Model Context Protocol) bridge** lets Claude AI (via Claude Code or Claude Desktop) control your Godot editor — list scripts, run the game, inspect the scene tree, etc.

It has two parts:
1. **Godot Plugin** (`addons/godot_mcp/`) — TCP server inside the editor (port 6030)
2. **Node.js Server** (`mcp-server/`) — bridges Claude's stdio protocol to Godot's TCP

---

## Setup Steps

### 1. Enable the Godot Plugin

1. Open the project in Godot 4
2. Go to **Project → Project Settings → Plugins**
3. Find "Godot MCP Bridge" → toggle **Enable**
4. Check the Output panel — you should see: `[MCP] Bridge listening on tcp://127.0.0.1:6030`

### 2. Install MCP Server Dependencies

```bash
cd ~/Desktop/42/mcp-server
npm install
```

### 3. Configure Claude Desktop or Claude Code

Add this to your Claude config file:

**Claude Desktop** (`~/Library/Application Support/Claude/claude_desktop_config.json`):
```json
{
  "mcpServers": {
    "warship-godot": {
      "command": "node",
      "args": ["/Users/tylerbaptiste/Desktop/42/mcp-server/index.js"]
    }
  }
}
```

**Claude Code** (`.claude/mcp.json` in project root):
```json
{
  "mcpServers": {
    "warship-godot": {
      "command": "node",
      "args": ["./mcp-server/index.js"]
    }
  }
}
```

### 4. Test It

1. Open the project in Godot with the plugin enabled
2. Start a Claude conversation
3. Ask Claude to "ping the Godot editor" — it should get a response

---

## Available Tools (for Claude)

| Tool | What It Does |
|------|-------------|
| `godot_ping` | Check if editor is connected |
| `godot_list_scripts` | List all .cs files |
| `godot_list_scenes` | List all .tscn files |
| `godot_get_project_info` | Project name, main scene |
| `godot_get_scene_tree` | Node tree of open scene |
| `godot_run` | Run the game (F5) |
| `godot_stop` | Stop the game |
| `read_game_file` | Read any project file |

---

## GitHub CLI Setup

To push this repo to GitHub, install the GitHub CLI:

```bash
brew install gh
gh auth login
cd ~/Desktop/42
gh repo create 42 --private --source=. --push
```
