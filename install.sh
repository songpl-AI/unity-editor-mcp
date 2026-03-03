#!/bin/bash
# Unity Editor MCP — Install Script (macOS / Linux)
# Usage:   bash install.sh
# Windows: use install.ps1 instead
# Effect:  builds the MCP Server and prints configuration snippets for each tool

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
MCP_SERVER_DIR="$SCRIPT_DIR/mcp-server"

echo "=== Unity Editor MCP — Install ==="
echo ""

# ── 检查依赖 ────────────────────────────────────────────────────────────────

command -v node >/dev/null 2>&1 || { echo "❌ Node.js 18+ is required. https://nodejs.org"; exit 1; }
command -v npm  >/dev/null 2>&1 || { echo "❌ npm is required."; exit 1; }

NODE_VER=$(node -e "console.log(process.versions.node.split('.')[0])")
if [ "$NODE_VER" -lt 18 ]; then
  echo "❌ Node.js 18+ required (current: $NODE_VER)"
  exit 1
fi

# ── 构建 MCP Server ─────────────────────────────────────────────────────────

echo "📦 Installing dependencies..."
cd "$MCP_SERVER_DIR"
npm install

echo "🔨 Building..."
npm run build

if [ ! -f "$MCP_SERVER_DIR/dist/index.js" ]; then
  echo "❌ Build failed: dist/index.js not found"
  exit 1
fi

echo "✅ Build complete: $MCP_SERVER_DIR/dist/index.js"
echo ""

# ── 生成配置片段 ─────────────────────────────────────────────────────────────

CONFIG_BLOCK=$(cat <<EOF
{
  "mcpServers": {
    "unity-editor": {
      "command": "node",
      "args": ["$MCP_SERVER_DIR/dist/index.js"],
      "env": {
        "UNITY_PORT": "23456",
        "UNITY_WS_PORT": "23457"
      }
    }
  }
}
EOF
)

echo "=== MCP Configuration ==="
echo "$CONFIG_BLOCK"
echo ""
echo "Add the \"unity-editor\" block to your tool's MCP config file:"
echo ""
echo "  ┌─ Claude Code ──────────────────────────────────────────────────┐"
echo "  │  ~/.claude/settings.json                                        │"
echo "  └────────────────────────────────────────────────────────────────┘"
echo ""
echo "  ┌─ Claude Desktop ───────────────────────────────────────────────┐"
echo "  │  macOS: ~/Library/Application Support/Claude/claude_desktop_config.json │"
echo "  │  Linux: ~/.config/Claude/claude_desktop_config.json                      │"
echo "  └──────────────────────────────────────────────────────────────────────────┘"
echo ""
echo "  ┌─ Cursor ───────────────────────────────────────────────────────┐"
echo "  │  ~/.cursor/mcp.json  (global)                                   │"
echo "  │  .cursor/mcp.json    (per-project)                              │"
echo "  └────────────────────────────────────────────────────────────────┘"
echo ""
echo "  ┌─ Continue ─────────────────────────────────────────────────────┐"
echo "  │  ~/.continue/config.json  → mcpServers 字段                     │"
echo "  └────────────────────────────────────────────────────────────────┘"
echo ""

# ── Unity Plugin 安装提示 ────────────────────────────────────────────────────

echo "=== Unity Plugin ==="
echo ""
echo "Option A — UPM (Git URL, recommended):"
echo "  Window > Package Manager > '+' > Add package from git URL"
echo "  Enter the git URL of this repo + '?path=unity-mcp-plugin'"
echo "  e.g.  https://github.com/yourname/unity-editor-mcp.git?path=unity-mcp-plugin"
echo ""
echo "Option B — Manual copy:"
echo "  cp -r $SCRIPT_DIR/unity-mcp-plugin  /path/to/YourUnityProject/Assets/"
echo ""
echo "  Then add to Packages/manifest.json:"
echo '  "com.unity.nuget.newtonsoft-json": "3.2.1"'
echo ""
echo "After import, verify Unity Console shows:"
echo '  [OpenClaw] Plugin ready. HTTP: http://127.0.0.1:23456/api/v1'
echo ""

# ── 验证命令 ─────────────────────────────────────────────────────────────────

echo "=== Verify Connection ==="
echo ""
echo "  # Check Unity is running and plugin is active:"
echo "  curl http://127.0.0.1:23456/api/v1/status"
echo ""
echo "  # Test via Claude Code:"
echo "  claude -p 'Check Unity status using unity_check_status' \\"
echo "    --mcp-config <your-config-file> \\"
echo "    --allowedTools 'mcp__unity-editor__unity_check_status' \\"
echo "    --max-turns 3"
echo ""
