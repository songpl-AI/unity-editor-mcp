# Unity Editor MCP Plugin

让 AI 编程工具通过自然语言直接操作 Unity Editor。基于 [MCP 协议](https://modelcontextprotocol.io)，兼容 Claude Code、Cursor、Claude Desktop、Continue 等所有支持 MCP 的工具。

## 环境要求

| 依赖 | 版本 |
|------|------|
| Unity Editor | 2020.3 LTS+ |
| Newtonsoft.Json | 3.0.2 (自动安装) |

## 快速安装

### 方式 A — UPM Git URL（推荐）

Unity > Window > Package Manager > `+` > Add package from git URL：

```
https://github.com/songpl-AI/unity-editor-mcp.git?path=/unity-mcp-plugin
```

**Git URL 格式说明：**
- 必须以 `.git` 结尾
- 使用 `?path=/unity-mcp-plugin` 指定子目录
- 可选指定版本：`#v1.0.0`（Tag）、`#main`（分支）、`#abc123`（Commit）

插件会自动安装依赖包 `com.unity.nuget.newtonsoft-json`。

### 方式 B — 手动复制

```bash
# macOS / Linux
cp -r unity-mcp-plugin  /path/to/YourUnityProject/Assets/

# Windows（PowerShell）
Copy-Item -Recurse unity-mcp-plugin C:\path\to\YourUnityProject\Assets\
```

然后在 `Packages/manifest.json` 的 `dependencies` 中添加：

```json
"com.unity.nuget.newtonsoft-json": "3.0.2"
```

## 验证安装

### 1. 检查 Console 日志

重新打开 Unity，Console 出现以下日志表示安装成功：

```
[OpenMCP] Plugin ready. HTTP: http://127.0.0.1:23456/api/v1  WS: ws://127.0.0.1:23457/ws
```

### 2. 检查 Package Manager

在 Package Manager 中应该能看到 "Unity Editor MCP" (com.openmcp.unity-editor-mcp)。

### 3. 测试连接

Unity Editor 打开且插件运行后，在终端执行（必须用 `127.0.0.1`，不能用 `localhost`）：

```bash
curl http://127.0.0.1:23456/api/v1/status
```

期望返回：

```json
{ "ok": true, "data": { "status": "ready", "unityVersion": "...", "httpPort": 23456 } }
```

## Dashboard 窗口

插件导入后，Unity 菜单栏会出现：

**Window > Open MCP**

窗口功能：
- 实时显示服务器运行状态（绿色 Running / 红色 Stopped）
- 显示 HTTP / WebSocket 地址并支持一键复制
- 手动启停服务器（Stop / Start 按钮）
- 显示当前场景、编辑模式、编译状态
- 活动日志（服务器启动、编译开始/完成/失败）

## 端口说明

| 服务 | 默认端口 | 环境变量 |
|------|----------|----------|
| HTTP REST | 23456 | `UNITY_PORT` |
| WebSocket | 23457 | `UNITY_WS_PORT` |

端口被占用时 Unity 自动递增（23456 → 23457 → …），实际端口打印在 Console。

## 配置 MCP Server

插件只是服务端，需要配合 MCP Server 使用。完整安装指南请参考：

[Unity Editor MCP 完整文档](https://github.com/songpl-AI/unity-editor-mcp)

## 常见问题

### 安装问题

**`No 'git' executable was found`**
- 安装 Git：https://git-scm.com/downloads
- 重启 Unity Editor
- 验证：在终端执行 `git --version`

**`Error adding package`**
- 确认 URL 格式正确（必须有 `.git` 后缀和 `?path=` 参数）
- 检查网络连接（能否访问 GitHub）
- 尝试手动克隆：`git clone https://github.com/songpl-AI/unity-editor-mcp.git`

**`Assembly has reference to non-existent assembly 'Newtonsoft.Json'`**
- 手动添加到 `Packages/manifest.json`：
  ```json
  "com.unity.nuget.newtonsoft-json": "3.0.2"
  ```

### 运行问题

**`curl` 返回 `Connection refused`**
- Unity Editor 未打开，或插件未成功导入
- 检查 Console 是否有 `[OpenMCP] Plugin ready` 日志
- 检查是否有编译错误

**`curl http://localhost:...` 返回 400**
- 必须使用 `127.0.0.1`，不能用 `localhost`
- HTTP Server 对 Host 头做严格校验（安全原因）

**Unity Console 报 `Newtonsoft.Json` 相关错误**
- `manifest.json` 中未添加 `com.unity.nuget.newtonsoft-json`
- Unity 正在下载包，稍等片刻
- 清理 `Library/` 目录并重启 Unity

**Unity 2020.3 / 2021.x WebSocket 不可用**
- Unity 2022.3+ 内置 WebSocket，无需额外操作
- Unity 2020.3 / 2021.x 需手动配置：
  1. 下载 `websocket-sharp.dll`
  2. 放入 `Assets/unity-mcp-plugin/Plugins/`
  3. 在 Player Settings > Scripting Define Symbols 中添加 `WEBSOCKET_SHARP`

## 卸载

在 Package Manager 中选择 "Unity Editor MCP"，点击 "Remove" 按钮。

或手动编辑 `Packages/manifest.json`，删除：

```json
"com.openmcp.unity-editor-mcp": "https://github.com/songpl-AI/unity-editor-mcp.git?path=/unity-mcp-plugin"
```

## 技术信息

### 包信息
- **包名：** com.openmcp.unity-editor-mcp
- **版本：** 1.0.0
- **Unity 最低版本：** 2020.3 LTS
- **许可证：** MIT

### 依赖包
- com.unity.nuget.newtonsoft-json: 3.0.2（兼容 Unity 2019.4+）

### 截图 API（`GET /api/v1/screenshot`）

| 参数 | 类型 | 说明 |
|------|------|------|
| `view` | string | `game`（默认）/ `game_window` / `scene` |
| `width` | int | 截图宽度，默认 1920（`game_window` 模式忽略）|
| `height` | int | 截图高度，默认 1080（`game_window` 模式忽略）|

返回：`{ "ok": true, "data": { "base64": "<PNG base64>" } }`

| `view` 值 | 原理 | 适用模式 |
|-----------|------|----------|
| `game` | 渲染场景摄像机（Play 模式优先 ScreenCapture）| Edit + Play |
| `game_window` | 反射读取 Game View 面板 RenderTexture | Edit 模式，需 Game View 已打开 |
| `scene` | 渲染 Scene View 摄像机 | Edit + Play |

### 架构
```
AI Tool (Claude Code / Cursor / ...)
    ↓  MCP stdio
MCP Server  (Node.js + TypeScript)
    ↓  HTTP REST + WebSocket
Unity Plugin (C# Editor-only)
    ↓  Unity Editor API
Unity Editor
```

## 参考资料

- [Unity Manual: Git dependencies](https://docs.unity3d.com/Manual/upm-git.html)
- [Unity Manual: Package manifest](https://docs.unity3d.com/Manual/upm-manifestPkg.html)
- [Model Context Protocol](https://modelcontextprotocol.io/)

## License

MIT
