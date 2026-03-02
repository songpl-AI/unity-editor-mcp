# Unity Editor MCP

让 AI 编程工具通过自然语言直接操作 Unity Editor。基于 [MCP 协议](https://modelcontextprotocol.io)，兼容 Claude Code、Cursor、Claude Desktop、Continue 等所有支持 MCP 的工具。

## 功能

- 管理 GameObject（创建、删除、查找、设置变换）
- 挂载/设置组件属性
- 读写 Assets 目录下的文件
- 触发编译并获取错误
- 管理材质（创建、设置属性、赋给 Renderer）
- 安装/卸载 Unity 包（Package Manager）
- 获取场景层级、项目信息、Console 日志
- Tag 管理、渲染管线检测、Player Settings 查询

## 环境要求

| 依赖 | 版本 |
|------|------|
| Unity Editor | 2021.3 LTS+ |
| Node.js | 18+ |
| npm | 8+ |

## 安装

### 第一步：运行安装脚本

```bash
bash install.sh
```

脚本会自动构建 MCP Server，并输出各工具的配置内容。

### 第二步：配置你的 AI 工具

将安装脚本输出的配置块添加到对应工具的配置文件：

**Claude Code** — `~/.claude/settings.json`

**Claude Desktop** — `~/Library/Application Support/Claude/claude_desktop_config.json`

**Cursor** — `~/.cursor/mcp.json`（全局）或 `.cursor/mcp.json`（项目级）

**Continue** — `~/.continue/config.json` 的 `mcpServers` 字段

配置格式统一为：

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

### 第三步：安装 Unity 插件

**方式 A — UPM Git URL（推荐）**

Unity > Window > Package Manager > `+` > Add package from git URL：

```
https://github.com/yourname/unity-editor-mcp.git?path=unity-plugin
```

**方式 B — 手动复制**

```bash
cp -r unity-plugin  /path/to/YourUnityProject/Assets/
```

同时在 `Packages/manifest.json` 中添加：

```json
"com.unity.nuget.newtonsoft-json": "3.2.1"
```

打开 Unity，Console 出现以下日志表示安装成功：

```
[OpenClaw] Plugin ready. HTTP: http://127.0.0.1:23456/api/v1
```

## 验证

```bash
# 确认 Unity 插件正在运行（必须使用 127.0.0.1，不能用 localhost）
curl http://127.0.0.1:23456/api/v1/status
```

## 可用工具

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
| `unity_compile` | 触发编译并等待结果 |
| `unity_get_compile_errors` | 获取编译错误 |
| `unity_get_console_logs` | 获取 Console 日志 |
| `unity_get_project_info` | 获取项目基本信息 |
| `unity_get_scripts` | 列出所有 C# 脚本 |
| `unity_find_assets` | 搜索项目资产 |
| `unity_get_tags` | 获取所有 Tag |
| `unity_create_tag` | 创建新 Tag |
| `unity_set_gameobject_tag` | 为 GameObject 设置 Tag |
| `unity_get_input_system_type` | 检测输入系统类型 |
| `unity_get_player_settings` | 获取 Player Settings |
| `unity_get_render_pipeline` | 检测渲染管线（Built-in/URP/HDRP）|
| `unity_get_material_properties` | 查看材质属性 |
| `unity_set_material_properties` | 设置材质属性 |
| `unity_assign_material` | 将材质赋给 Renderer |
| `unity_list_packages` | 列出已安装 Unity 包 |
| `unity_install_package` | 安装 Unity 包 |
| `unity_remove_package` | 卸载 Unity 包 |

## 端口说明

| 服务 | 默认端口 | 环境变量 |
|------|----------|----------|
| HTTP REST | 23456 | `UNITY_PORT` |
| WebSocket | 23457 | `UNITY_WS_PORT` |

端口冲突时 Unity 自动递增。需同步修改 MCP 配置中的环境变量。

## 架构

```
AI Tool (Claude Code / Cursor / ...)
    ↓  MCP stdio
MCP Server  (mcp-server/)  — Node.js
    ↓  HTTP REST + WebSocket
Unity Plugin (unity-plugin/) — C# Editor-only
    ↓  Unity Editor API
Unity Editor
```

## Unity 2021.3 WebSocket 补充

2022.3+ 内置 WebSocket，无需额外操作。2021.3 需手动安装 `websocket-sharp.dll`，放入 `Assets/unity-plugin/Plugins/`。

## License

MIT
