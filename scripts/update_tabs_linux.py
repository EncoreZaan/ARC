# Update docs/index.html
with open('docs/index.html', 'r', encoding='utf-8') as f:
    html = f.read()

# 1. Update config tabs
old_tabs = '''            <div class="config-tabs">
              <button class="config-tab active" data-target="claude">Claude Desktop (Windows)</button>
              <button class="config-tab" data-target="macos">macOS (Apple Silicon & Intel)</button>
              <button class="config-tab" data-target="cursor">Cursor / VS Code MCP</button>
              <button class="config-tab" data-target="cli">CLI Daemon</button>
            </div>'''

new_tabs = '''            <div class="config-tabs">
              <button class="config-tab active" data-target="claude">Windows (Revit & BIMcraft)</button>
              <button class="config-tab" data-target="macos">macOS (Apple Silicon & Intel)</button>
              <button class="config-tab" data-target="linux">Linux (Vulkan & Headless)</button>
              <button class="config-tab" data-target="cursor">Cursor / VS Code MCP</button>
              <button class="config-tab" data-target="cli">CLI Daemon</button>
            </div>'''

html = html.replace(old_tabs, new_tabs)

# 2. Update download section subtitle and buttons
old_dl_sub = '''          <p class="download-subtitle">
            Téléchargez l'installateur autonome ARC pour Windows. Compatible avec Autodesk Revit 2024, 2025, 2026 et 2027.
          </p>'''

new_dl_sub = '''          <p class="download-subtitle">
            Disponible universellement pour <strong>Windows</strong> (Revit 2024–2027 & BIMcraft), <strong>macOS</strong> (Apple Silicon M1–M4 & Intel avec BIMcraft Metal) et <strong>Linux</strong> (Vulkan, Headless & CI/CD).
          </p>'''

html = html.replace(old_dl_sub, new_dl_sub)

old_dl_actions = '''            <a href="./revit_architecture_guide.md" class="btn-download-hero" style="background: linear-gradient(135deg, rgba(215, 185, 140, 0.25), rgba(39, 76, 143, 0.35)); border-color: var(--border-champagne);" title="Guide complet d'installation et d'utilisation sur macOS">
              <span style="font-size: 1.1rem; margin-right: 0.35rem;"></span>
              macOS (Apple Silicon & Intel)
            </a>'''

new_dl_actions = '''            <a href="./revit_architecture_guide.md" class="btn-download-hero" style="background: linear-gradient(135deg, rgba(215, 185, 140, 0.25), rgba(39, 76, 143, 0.35)); border-color: var(--border-champagne);" title="Guide complet d'installation et d'utilisation sur macOS">
              <span style="font-size: 1.1rem; margin-right: 0.35rem;"></span>
              macOS (Apple Silicon & Intel)
            </a>
            <a href="./revit_architecture_guide.md#installation-express-sous-linux" class="btn-download-hero" style="background: linear-gradient(135deg, rgba(85, 181, 127, 0.25), rgba(39, 76, 143, 0.35)); border-color: rgba(85, 181, 127, 0.5);" title="Installation et configuration sous Linux">
              <span style="font-size: 1.1rem; margin-right: 0.35rem;">🐧</span>
              Linux (Vulkan & Headless)
            </a>'''

html = html.replace(old_dl_actions, new_dl_actions)

with open('docs/index.html', 'w', encoding='utf-8') as f:
    f.write(html)

print("docs/index.html updated successfully")

# Update docs/app.js
with open('docs/app.js', 'r', encoding='utf-8') as f:
    js = f.read()

# Add linux snippet to configSnippets
old_macos_block = '''    macos: `// ~/Library/Application Support/Claude/claude_desktop_config.json
{
  "mcpServers": {
    "revit": {
      "command": "/Users/%USERNAME%/.cargo/bin/revit-mcp-server",
      "env": {
        "REVIT_HOST": "http://localhost:6543",
        "REVIT_ALLOW_REMOTE": "1"
      }
    },
    "bimcraft": {
      "command": "/Users/%USERNAME%/.cargo/bin/bimcraft-cli",
      "args": ["mcp", "--connect"]
    },
    "cad-mcp": {
      "command": "python3",
      "args": ["/Users/%USERNAME%/ARC/cad-mcp/server.py"]
    }
  }
}`,
    cursor:'''

new_macos_block = '''    macos: `// ~/Library/Application Support/Claude/claude_desktop_config.json
{
  "mcpServers": {
    "bimcraft": {
      "command": "/Users/%USERNAME%/.cargo/bin/bimcraft-cli",
      "args": ["mcp", "--connect"]
    },
    "revit-core": {
      "command": "/Users/%USERNAME%/.cargo/bin/revit-mcp-server",
      "env": {
        "REVIT_HOST": "http://localhost:6543",
        "REVIT_ALLOW_REMOTE": "1"
      }
    },
    "cad-mcp": {
      "command": "python3",
      "args": ["/Users/%USERNAME%/ARC/cad-mcp/server.py"]
    }
  }
}`,
    linux: `// ~/.config/Claude/claude_desktop_config.json
{
  "mcpServers": {
    "bimcraft": {
      "command": "/usr/local/bin/bimcraft-cli",
      "args": ["mcp", "--connect"]
    },
    "revit-core": {
      "command": "/usr/local/bin/revit-mcp-server",
      "args": ["--pipe", "/tmp/revit_mcp_ipc.sock"]
    },
    "cad-mcp": {
      "command": "python3",
      "args": ["/opt/ARC/cad-mcp/server.py"]
    }
  }
}`,
    cursor:'''

js = js.replace(old_macos_block, new_macos_block)

with open('docs/app.js', 'w', encoding='utf-8') as f:
    f.write(js)

print("docs/app.js updated successfully")
