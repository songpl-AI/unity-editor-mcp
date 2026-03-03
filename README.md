# Unity Editor MCP

让 AI 编程工具通过自然语言直接操作 Unity Editor。基于 [MCP 协议](https://modelcontextprotocol.io)，兼容 Claude Code、Cursor、Claude Desktop、Continue 等所有支持 MCP 的工具。

## 环境要求

| 依赖 | 版本 |
|------|------|
| Unity Editor | 2020.3 LTS+ |
| Node.js | 18+ |
| npm | 8+ |

---

## 快速安装

### 第一步：构建 MCP Server

**macOS / Linux**

```bash
bash install.sh
```

**Windows（PowerShell）**

```powershell
.\install.ps1
```

> 首次运行 PowerShell 脚本若提示执行策略限制，先执行：
> ```powershell
> Set-ExecutionPolicy -Scope CurrentUser RemoteSigned
> ```
> 或单次绕过：`powershell -ExecutionPolicy Bypass -File .\install.ps1`

脚本执行完成后会输出如下配置块（路径已填好），直接复制使用：

```json
{
  "mcpServers": {
    "unity-editor": {
      "command": "node",
      "args": ["/absolute/path/to/unity-editor-mcp/mcp-server/dist/index.js"],
      "env": {
        "UNITY_PORT": "23456",
        "UNITY_WS_PORT": "23457"
      }
    }
  }
}
```

### 第二步：添加到 AI 工具配置

将上一步输出的 `unity-editor` 块合并到你的工具配置文件：

| 工具 | macOS / Linux | Windows |
|------|--------------|---------|
| **Claude Code** | `~/.claude/settings.json` | `%USERPROFILE%\.claude\settings.json` |
| **Claude Desktop** | `~/Library/Application Support/Claude/claude_desktop_config.json` | `%APPDATA%\Claude\claude_desktop_config.json` |
| **Cursor**（全局） | `~/.cursor/mcp.json` | `%USERPROFILE%\.cursor\mcp.json` |
| **Cursor**（项目级） | `.cursor/mcp.json` | `.cursor\mcp.json` |
| **Continue** | `~/.continue/config.json` → `mcpServers` | `%USERPROFILE%\.continue\config.json` → `mcpServers` |

### 第三步：安装 Unity 插件

**方式 A — UPM Git URL（推荐，自动处理依赖）**

Unity > Window > Package Manager > `+` > Add package from git URL：

```
https://github.com/songpl-AI/unity-editor-mcp.git?path=/unity-mcp-plugin
```

> 注意：使用 `.git` 结尾并通过 `?path=` 参数指定子目录。Unity 会自动处理依赖。

**方式 B — 手动复制**

```bash
# macOS / Linux
cp -r unity-mcp-plugin  /path/to/YourUnityProject/Assets/
```

```powershell
# Windows（PowerShell）
Copy-Item -Recurse unity-mcp-plugin C:\path\to\YourUnityProject\Assets\
```

然后在 `Packages/manifest.json` 的 `dependencies` 中添加：

```json
"com.unity.nuget.newtonsoft-json": "3.0.2"
```

重新打开 Unity，Console 出现以下日志表示安装成功：

```
[OpenMCP] Plugin ready. HTTP: http://127.0.0.1:23456/api/v1  WS: ws://127.0.0.1:23457/ws
```

---

## 验证连接

Unity Editor 打开且插件运行后，执行（必须用 `127.0.0.1`，不能用 `localhost`）：

```bash
curl http://127.0.0.1:23456/api/v1/status
```

期望返回：

```json
{ "ok": true, "data": { "status": "ready", "unityVersion": "...", "httpPort": 23456 } }
```

---

## Dashboard 窗口

插件导入后，Unity 菜单栏会出现：

**Window > Open MCP**

窗口功能：
- 实时显示服务器运行状态（绿色 Running / 红色 Stopped）
- 显示 HTTP / WebSocket 地址并支持一键复制
- 手动启停服务器（Stop / Start 按钮）
- 显示当前场景、编辑模式、编译状态
- 活动日志（服务器启动、编译开始/完成/失败）

---

## 可用工具（31 个）

| 工具 | 说明 |
|------|------|
| `unity_check_status` | 检查 Unity Editor 连接状态 |
| `unity_get_scene_info` | 获取当前场景基本信息 |
| `unity_get_hierarchy` | 获取场景层级结构 |
| `unity_save_scene` | 保存当前场景 |
| `unity_create_gameobject` | 创建 GameObject |
| `unity_delete_gameobject` | 删除 GameObject |
| `unity_set_transform` | 设置位置/旋转/缩放 |
| `unity_find_gameobjects` | 按名称或 Tag 查找 |
| `unity_get_components` | 获取组件列表 |
| `unity_add_component` | 添加组件 |
| `unity_set_component_property` | 设置组件属性 |
| `unity_read_file` | 读取 Assets/ 内文件 |
| `unity_write_file` | 写入 Assets/ 内文件 |
| `unity_compile` | 触发编译并等待结果（WebSocket 推送）|
| `unity_get_compile_errors` | 获取编译错误列表 |
| `unity_get_console_logs` | 获取 Console 日志 |
| `unity_get_project_info` | 获取项目基本信息 |
| `unity_get_scripts` | 列出所有 C# 脚本及公开 API |
| `unity_find_assets` | 搜索项目资产 |
| `unity_get_tags` | 获取所有 Tag |
| `unity_create_tag` | 创建新 Tag |
| `unity_set_gameobject_tag` | 为 GameObject 设置 Tag |
| `unity_get_input_system_type` | 检测输入系统类型（Legacy/New/Both）|
| `unity_get_player_settings` | 获取 Player Settings 配置 |
| `unity_get_render_pipeline` | 检测渲染管线（Built-in/URP/HDRP）|
| `unity_get_material_properties` | 查看材质所有属性及当前值 |
| `unity_set_material_properties` | 设置材质属性（颜色/Float/贴图等）|
| `unity_assign_material` | 将材质赋给场景中 Renderer |
| `unity_list_packages` | 列出已安装 Unity 包 |
| `unity_install_package` | 安装 Unity 包（含等待 Domain Reload）|
| `unity_remove_package` | 卸载 Unity 包 |

---

## 端口说明

| 服务 | 默认端口 | 环境变量 |
|------|----------|----------|
| HTTP REST | 23456 | `UNITY_PORT` |
| WebSocket | 23457 | `UNITY_WS_PORT` |

端口被占用时 Unity 自动递增（23456 → 23457 → …），实际端口打印在 Console。若端口不是默认值，需同步修改 MCP 配置中的环境变量。

---

## 架构

```
AI Tool (Claude Code / Cursor / ...)
    ↓  MCP stdio
MCP Server  (mcp-server/)  — Node.js + TypeScript
    ↓  HTTP REST + WebSocket
Unity Plugin (unity-mcp-plugin/) — C# Editor-only
    ↓  Unity Editor API
Unity Editor
```

---

## 常见问题

**`curl` 返回 `Connection refused`**
→ Unity Editor 未打开，或插件未成功导入。检查 Console 是否有 `[OpenMCP] Plugin ready` 日志。

**`curl http://localhost:...` 返回 400**
→ 必须使用 `127.0.0.1`，不能用 `localhost`（HTTP Server 对 Host 头做严格校验）。

**Unity Console 报 `Newtonsoft.Json` 相关错误**
→ `manifest.json` 中未添加 `com.unity.nuget.newtonsoft-json`，或 Unity 正在下载包，稍等片刻。

**`unity_compile` 超时（60s）**
→ Unity 正在进行 Domain Reload，等待编译完成后重试。大型项目可在 prompt 中指定 `timeoutSeconds: 120`。

**Unity 2020.3 / 2021.x WebSocket 不可用**
→ 2022.3+ 内置 WebSocket，无需额外操作。2020.3 / 2021.x 需手动下载 `websocket-sharp.dll` 放入 `Assets/unity-mcp-plugin/Plugins/`，并在 Player Settings > Scripting Define Symbols 中添加 `WEBSOCKET_SHARP`。

---

## License

MIT
