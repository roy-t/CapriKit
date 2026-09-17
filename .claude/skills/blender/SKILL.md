---
name: blender
description: Start Blender with the MCP add-on so the `blender` MCP server tools can reach it. Use when the user wants to inspect or edit a .blend file through MCP, or when a `mcp__blender__*` tool fails with "Cannot connect to Blender".
argument-hint: "[path/to/file.blend]"
allowed-tools: PowerShell(*start-blender.ps1*)
---

# Blender MCP
Two processes are involved:

```
Claude Code ⇐ stdio ⇒ blender-mcp (uv, from .mcp.json) ⇐ TCP :9876 ⇒ MCP add-on inside Blender
```

Claude Code launches `blender-mcp` itself from `.mcp.json`. This skill only makes sure Blender runs with the add-on listening. Each tool call opens a new socket, so tools work as soon as Blender is up; no Claude restart is needed.

## Steps

1. Start Blender (optionally with the `.blend` file from `$ARGUMENTS`) and wait for the bridge:

   ```powershell
   & .claude/skills/blender/start-blender.ps1 -BlendFile "$ARGUMENTS"
   ```

   Leave out `-BlendFile` when no argument was given. If port 9876 is already held by Blender the script exits 0 without starting a second instance; tell the user which file to open themselves if they asked for a different one.

2. Confirm the MCP tools are available (`mcp__blender__*`). If they are not, see Troubleshooting.

## Troubleshooting

- **Blender runs but port 9876 never listens**: in Blender, *Edit > Preferences > Add-ons > MCP* must be enabled, with *Auto Start* on, and *Edit > Preferences > System > Network > Allow Online Access* must be enabled. The add-on preferences also have a *Start MCP Bridge Server* button and show startup errors.
- **No `mcp__blender__*` tools**: the `blender` server in `.mcp.json` needs approval (`/mcp`), `uv` must be on `PATH` when Claude Code starts, and `external/blender_mcp` must be initialized (`git submodule update --init`). Tool changes require a Claude Code restart.
- **Blender installed elsewhere**: set the `BLENDER_PATH` environment variable; both the script and `.mcp.json` use it.
- For more information see: https://projects.blender.org/lab/blender_mcp