# Unity Editor MCP — Windows Install Script (PowerShell)
# Usage:
#   .\install.ps1
#
# If you see "running scripts is disabled", run this first (once):
#   Set-ExecutionPolicy -Scope CurrentUser RemoteSigned
# Or bypass for a single run:
#   powershell -ExecutionPolicy Bypass -File .\install.ps1

$ErrorActionPreference = "Stop"

$ScriptDir    = Split-Path -Parent $MyInvocation.MyCommand.Definition
$McpServerDir = Join-Path $ScriptDir "mcp-server"

Write-Host "=== Unity Editor MCP — Install ===" -ForegroundColor Cyan
Write-Host ""

# ── Check dependencies ───────────────────────────────────────────────────────

if (-not (Get-Command node -ErrorAction SilentlyContinue)) {
    Write-Host "ERROR: Node.js 18+ is required.  https://nodejs.org" -ForegroundColor Red
    exit 1
}
if (-not (Get-Command npm -ErrorAction SilentlyContinue)) {
    Write-Host "ERROR: npm is required." -ForegroundColor Red
    exit 1
}

$nodeVer = [int](node -e "console.log(process.versions.node.split('.')[0])")
if ($nodeVer -lt 18) {
    Write-Host "ERROR: Node.js 18+ required (current: $nodeVer)" -ForegroundColor Red
    exit 1
}

# ── Build MCP Server ─────────────────────────────────────────────────────────

Write-Host "[1/2] Installing npm dependencies..." -ForegroundColor Yellow
Set-Location "$McpServerDir"
npm install

Write-Host "[2/2] Building TypeScript..." -ForegroundColor Yellow
npm run build

$distIndex = Join-Path $McpServerDir "dist\index.js"
if (-not (Test-Path $distIndex)) {
    Write-Host "ERROR: Build failed — dist\index.js not found" -ForegroundColor Red
    exit 1
}

Write-Host "Build complete: $distIndex" -ForegroundColor Green
Write-Host ""

# ── Output MCP config block ──────────────────────────────────────────────────
# node.js on Windows accepts forward slashes in paths
$distIndexFwd = $distIndex -replace '\\', '/'

$configBlock = @"
{
  "mcpServers": {
    "unity-editor": {
      "command": "node",
      "args": ["$distIndexFwd"],
      "env": {
        "UNITY_PORT": "23456",
        "UNITY_WS_PORT": "23457"
      }
    }
  }
}
"@

Write-Host "=== MCP Configuration ===" -ForegroundColor Cyan
Write-Host $configBlock
Write-Host 'Add the "unity-editor" block to your tool''s MCP config file:'
Write-Host ""
Write-Host "  Claude Code    : $env:USERPROFILE\.claude\settings.json"
Write-Host "  Claude Desktop : $env:APPDATA\Claude\claude_desktop_config.json"
Write-Host "  Cursor(global) : $env:USERPROFILE\.cursor\mcp.json"
Write-Host "  Cursor(project): .cursor\mcp.json"
Write-Host "  Continue       : $env:USERPROFILE\.continue\config.json  -> mcpServers"
Write-Host ""

# ── Unity Plugin instructions ────────────────────────────────────────────────

Write-Host "=== Unity Plugin ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Option A — UPM Git URL (recommended):"
Write-Host "  Window > Package Manager > '+' > Add package from git URL"
Write-Host "  https://github.com/yourname/unity-editor-mcp.git?path=unity-plugin"
Write-Host ""
Write-Host "Option B — Manual copy (PowerShell):"
$unityPluginSrc = Join-Path $ScriptDir "unity-plugin"
Write-Host "  Copy-Item -Recurse `"$unityPluginSrc`" `"C:\path\to\YourProject\Assets\`""
Write-Host ""
Write-Host "  Then add to Packages/manifest.json:"
Write-Host '  "com.unity.nuget.newtonsoft-json": "3.2.1"'
Write-Host ""
Write-Host "After import, Unity Console should show:"
Write-Host "  [OpenClaw] Plugin ready. HTTP: http://127.0.0.1:23456/api/v1"
Write-Host ""

# ── Verify ───────────────────────────────────────────────────────────────────

Write-Host "=== Verify Connection ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "  curl http://127.0.0.1:23456/api/v1/status"
Write-Host ""
Write-Host "Expected response: { `"ok`": true, `"data`": { `"status`": `"ready`", ... } }"
Write-Host ""
