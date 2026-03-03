# AGENTS.md — Unity Editor MCP

本文件供 AI 助手在开发本项目时自动读取，包含编码规范、架构约束和关键决策。

---

## 项目概况

**Unity Editor MCP** 是一个独立发布包，让 AI 编程工具（Claude Code、Cursor、Claude Desktop、Continue）通过 [MCP 协议](https://modelcontextprotocol.io)操作 Unity Editor。

**项目结构：**

```
unity-editor-mcp/
├── AGENTS.md              ← 本文件
├── README.md              ← 用户文档
├── COMPATIBILITY_FIXES.md ← Unity 2020.3 兼容性修复记录
├── install.sh             ← macOS/Linux 构建脚本
├── install.ps1            ← Windows 构建脚本
├── mcp-server/            ← Node.js MCP Server
│   ├── package.json
│   ├── tsconfig.json
│   └── src/
│       ├── index.ts       ← MCP Server 入口
│       ├── tools/         ← 工具实现（按功能分类）
│       │   ├── scene.ts
│       │   ├── gameobject.ts
│       │   ├── asset.ts
│       │   └── ...
│       └── utils/
│           ├── format.ts  ← 输出格式化
│           └── client.ts  ← HTTP/WebSocket 客户端
└── unity-mcp-plugin/      ← Unity C# 插件（UPM 格式）
    ├── package.json       ← UPM 包定义
    └── Editor/            ← Editor-only 代码
        ├── UnityEditorServer.cs
        ├── Core/
        ├── Handlers/
        └── UI/
```

---

## Unity C# 插件规范

### 强制规则

#### 1. 主线程安全

所有 Unity Editor API 调用**必须**通过 `MainThreadDispatcher.Dispatch()` 提交，绝不在 HTTP 后台线程直接调用。

```csharp
// ❌ 禁止 — 会导致 "can only be called from the main thread" 错误
public void HandleFind(HttpContext ctx) {
    var go = GameObject.Find("Player");  // 在 HTTP 线程执行，报错！
}

// ✅ 必须 — 正确模式
public void HandleFind(HttpContext ctx) {
    var result = MainThreadDispatcher.Dispatch(() => {
        return GameObject.Find("Player");  // 在主线程执行
    });
}
```

#### 2. Editor-only 目录

所有代码**必须**位于 `Editor/` 目录下，不得放在 Runtime 目录，否则会被打包进 Player Build。

```
✅ unity-mcp-plugin/Editor/Handlers/GameObjectHandler.cs
❌ unity-mcp-plugin/Runtime/Handlers/GameObjectHandler.cs
❌ unity-mcp-plugin/Scripts/GameObjectHandler.cs
```

#### 3. Domain Reload 清理

任何持有资源的静态类**必须**注册 `AssemblyReloadEvents.beforeAssemblyReload` 进行清理，防止端口占用和内存泄漏。

```csharp
// ✅ HttpServer.cs、UnityEditorServer.cs 都已正确实现
[InitializeOnLoad]
public class UnityEditorServer
{
    static UnityEditorServer() {
        AssemblyReloadEvents.beforeAssemblyReload += Cleanup;
    }
    
    private static void Cleanup() {
        _httpServer?.Stop();
        _wsServer?.Stop();
    }
}
```

#### 4. Undo 支持

所有修改场景的操作**必须**注册 Undo，不得绕过。

```csharp
// ✅ 创建对象
var go = new GameObject("Cube");
Undo.RegisterCreatedObjectUndo(go, "Create Cube");

// ✅ 修改属性
Undo.RecordObject(go.transform, "Set Transform");
go.transform.position = new Vector3(1, 2, 3);

// ✅ 添加组件
Undo.AddComponent<Rigidbody>(go);

// ✅ 删除对象
Undo.DestroyObjectImmediate(go);
```

#### 5. 不直接序列化 Unity 类型

`Vector3`、`Transform`、`GameObject` 等 Unity 类型不得直接传给 `JsonConvert.SerializeObject`，必须转为 DTO。

```csharp
// ❌ 禁止
ResponseHelper.WriteSuccess(ctx.Response, go.transform.position);

// ✅ 必须
ResponseHelper.WriteSuccess(ctx.Response, new {
    x = go.transform.position.x,
    y = go.transform.position.y,
    z = go.transform.position.z
});
```

#### 6. Unity 2020.3 兼容性（C# 7.3）

**禁止使用** C# 8.0+ 特性和 Unity 2021+ 独有 API，保持与 Unity 2020.3 LTS 兼容：

```csharp
// ❌ 禁止 — GetValueOrDefault() 需要 .NET Standard 2.1+
var path = dict.GetValueOrDefault("path", "");

// ✅ 使用 TryGetValue
var path = dict.TryGetValue("path", out var value) ? value : "";

// ❌ 禁止 — Switch 表达式（C# 8.0+）
var result = type switch {
    LogType.Error => "error",
    _ => "log"
};

// ✅ 使用传统 switch 语句
switch (type) {
    case LogType.Error: return "error";
    default: return "log";
}

// ❌ 禁止 — Null 合并赋值（C# 8.0+）
_server ??= new HttpServer();

// ✅ 使用传统 null 检查
if (_server == null) _server = new HttpServer();

// ❌ 禁止 — PackageInfo.FindForPackageName()（Unity 2021.2+）
var info = PackageInfo.FindForPackageName("com.unity.package");

// ✅ 使用 Client.List()（Unity 2020.3+）
var listRequest = Client.List();
EditorApplication.update += () => {
    if (!listRequest.IsCompleted) return;
    foreach (var pkg in listRequest.Result) {
        if (pkg.name == "com.unity.package") { /* 已安装 */ }
    }
};
```

**兼容性检查清单：**
- ❌ `GetValueOrDefault()`
- ❌ Switch 表达式 `=> ... switch { ... }`
- ❌ `??=` 运算符
- ❌ `record` 类型
- ❌ `init` 关键字
- ❌ Index (`^`) 和 Range (`..`) 运算符
- ❌ `PackageInfo.FindForPackageName()` 等 Unity 2021+ API
- ✅ 所有代码使用 C# 7.3 语法和 Unity 2020.3 API

详细修复记录见 `COMPATIBILITY_FIXES.md`。

### 代码风格

- **命名规范**
  - PascalCase：类名、方法名、公开字段（`GameObjectHandler`、`HandleCreate`）
  - camelCase：私有字段加 `_` 前缀（`_httpServer`、`_listener`）
  - UPPER_SNAKE_CASE：常量（`DEFAULT_PORT`）

- **错误处理**
  - 统一使用 `ApiResponse.Error(code, message)` 返回错误
  - 不得在 Handler 中直接 `throw` 到 HTTP 层（会导致 500 响应）
  - Handler 内部的 `MainThreadDispatcher.Dispatch(() => { ... })` 中可以 `throw`，会被自动捕获并转为错误响应

```csharp
// ✅ 正确模式
public void HandleCreate(HttpContext ctx) {
    var req = ctx.ParseBody<CreateRequest>();
    if (string.IsNullOrEmpty(req.Name)) {
        ResponseHelper.WriteError(ctx.Response, ErrorCode.InvalidParams, "'name' is required");
        return;
    }
    
    var result = MainThreadDispatcher.Dispatch(() => {
        // 这里可以 throw，会被自动捕获
        if (GameObject.Find(req.Name) != null)
            throw new Exception($"GameObject '{req.Name}' already exists");
        return new GameObject(req.Name);
    });
    
    ResponseHelper.WriteSuccess(ctx.Response, result);
}
```

- **日志规范**
  - 使用 `Debug.Log("[OpenClaw] ...")` 前缀，便于 Console 过滤
  - 启动/停止服务器：`[OpenClaw] Starting...` / `[OpenClaw] Shutting down...`
  - 错误：`[OpenClaw] Error: ...`

### API 响应格式（不得改变）

所有 HTTP API 必须返回以下格式之一：

```json
// 成功
{ "ok": true, "data": { ... }, "error": null }

// 失败
{ "ok": false, "data": null, "error": { "code": "ERROR_CODE", "message": "..." } }
```

**错误码定义：**

| 错误码 | 含义 | HTTP 状态码 |
|--------|------|-------------|
| `INVALID_PARAMS` | 参数缺失或无效 | 400 |
| `NOT_FOUND` | 资源不存在 | 404 |
| `COMPILE_ERROR` | 编译失败 | 400 |
| `TIMEOUT` | 操作超时 | 408 |
| `INTERNAL_ERROR` | Unity 内部错误 | 500 |

---

## MCP Server (TypeScript) 规范

### 强制规则

#### 1. 工具命名

所有工具名**必须**以 `unity_` 前缀开头，使用 `snake_case`。

```typescript
// ✅ 正确
server.tool("unity_get_hierarchy", ...)
server.tool("unity_create_gameobject", ...)

// ❌ 禁止
server.tool("getHierarchy", ...)
server.tool("create-gameobject", ...)
server.tool("CreateGameObject", ...)
```

#### 2. 错误抛出而非返回

MCP handler 内部用 `throw new Error(...)` 报错，SDK 自动将其转为 MCP 错误响应。**不得** `return` 错误字符串。

```typescript
// ✅ 正确模式
server.tool("unity_get_hierarchy", {
  description: "获取场景层级结构",
  inputSchema: { type: "object", properties: {} }
}, async () => {
  const res = await client.get("/api/v1/scene/hierarchy");
  if (!res.ok) throw new Error(res.error?.message ?? "Unknown error");
  return { content: [{ type: "text", text: formatHierarchy(res.data) }] };
});

// ❌ 禁止 — 不要返回错误字符串
async () => {
  const res = await client.get("/api/v1/scene/hierarchy");
  if (!res.ok) return { content: [{ type: "text", text: `Error: ${res.error?.message}` }] };
}
```

#### 3. 输出格式化

工具返回的文本**必须**经过 `utils/format.ts` 格式化，不得直接返回原始 JSON 字符串。

```typescript
// ❌ 禁止 — Agent 难以阅读原始 JSON
return { content: [{ type: "text", text: JSON.stringify(res.data) }] };

// ✅ 必须 — 使用格式化函数
return { content: [{ type: "text", text: formatHierarchy(res.data) }] };
```

**格式化示例：**

```typescript
// utils/format.ts
export function formatHierarchy(data: any): string {
  return `Scene: ${data.sceneName}

Hierarchy:
${data.objects.map((obj: any) => 
  `  ${obj.path} ${obj.active ? '✓' : '✗'}`
).join('\n')}

Total: ${data.objects.length} objects`;
}
```

#### 4. HTTP 客户端配置

所有请求**必须**使用 `127.0.0.1`，不得使用 `localhost`（Unity Plugin 对 Host 头做严格校验）。

```typescript
// ✅ 正确 — utils/client.ts 已配置
const baseURL = `http://127.0.0.1:${process.env.UNITY_PORT || 23456}`;

// ❌ 禁止
const baseURL = `http://localhost:${process.env.UNITY_PORT || 23456}`;
```

### 代码风格

- **TypeScript 严格模式**
  - 不使用 `any`（临时类型声明除外，需加 `// TODO: replace with SDK types` 注释）
  - 启用 `strict: true` 和 `noImplicitAny: true`

- **文件组织**
  - 每个工具独立文件，放在 `src/tools/` 下，按功能分类
  - `src/tools/scene.ts` — 场景相关（`unity_get_scene_info`、`unity_save_scene`）
  - `src/tools/gameobject.ts` — GameObject 相关（`unity_create_gameobject`、`unity_find_gameobjects`）
  - `src/tools/asset.ts` — 资产相关（`unity_find_assets`、`unity_read_file`）
  - `src/tools/compile.ts` — 编译相关（`unity_compile`、`unity_get_compile_errors`）

- **异步函数**
  - 统一使用 `async/await`，不使用 `.then()` 链式调用

```typescript
// ✅ 正确
async () => {
  const res = await client.get("/api/v1/status");
  const data = await processData(res.data);
  return { content: [{ type: "text", text: data }] };
}

// ❌ 禁止
() => {
  return client.get("/api/v1/status")
    .then(res => processData(res.data))
    .then(data => ({ content: [{ type: "text", text: data }] }));
}
```

- **工具描述**
  - 简洁明确，说明功能而非实现细节
  - 用中文或英文保持一致（建议中文，面向中国开发者）

```typescript
// ✅ 正确
description: "获取当前场景的层级结构，包括所有 GameObject 的路径和激活状态"

// ❌ 过于简略
description: "获取层级"

// ❌ 包含实现细节
description: "调用 /api/v1/scene/hierarchy 获取层级结构并格式化返回"
```

---

## 架构决策（不得推翻）

| 决策 | 结论 | 原因 |
|------|------|------|
| 通信协议 | HTTP REST（主动操作）+ WebSocket（事件推送） | REST 简单可靠，WS 用于编译/Domain Reload 等长时异步事件 |
| WebSocket 兼容 | Unity 2022.3+：.NET 内置<br>Unity 2020.3 / 2021.x：websocket-sharp（MIT）<br>条件编译隔离（`#if UNITY_2022_3_OR_NEWER`） | 避免强制依赖第三方库 |
| GameObject 标识 | 使用场景层级路径（如 `"Player/Head"`） | InstanceID 在场景重载后失效，路径更稳定 |
| 资产路径限制 | 所有路径必须在 `Assets/` 目录下 | 防止访问系统文件，安全隔离 |
| 服务器绑定 | 仅 `127.0.0.1`，不暴露外网 | 避免远程攻击风险 |
| Unity 兼容版本 | 2020.3 LTS 及以上 | 覆盖 90% 活跃项目，C# 7.3 语法足够 |
| 端口默认值 | HTTP: 23456<br>WebSocket: 23457 | 避开常用端口，被占用时自动递增 |
| 依赖包管理 | Unity Plugin 自动安装 `com.unity.nuget.newtonsoft-json` | 减少用户手动配置 |

---

## 开发工作流

### 添加新工具

1. **定义 Unity Plugin API**
   - 在 `unity-mcp-plugin/Editor/Handlers/` 创建或修改 Handler
   - 在 `UnityEditorServer.cs` 的 `RegisterRoutes()` 中注册路由
   - 测试 API：`curl http://127.0.0.1:23456/api/v1/...`

2. **实现 MCP Server 工具**
   - 在 `mcp-server/src/tools/` 创建或修改工具文件
   - 在 `index.ts` 中导入并注册
   - 测试工具：`npx @modelcontextprotocol/inspector dist/index.js`

3. **更新文档**
   - 在 `README.md` 的"可用工具"表格中添加条目
   - 如有复杂参数，在注释中说明

### 构建和发布

```bash
# 构建 MCP Server
cd mcp-server
npm install
npm run build

# 测试 Unity Plugin
# 1. 复制 unity-mcp-plugin/ 到测试 Unity 项目
# 2. 打开 Unity Editor
# 3. 检查 Console 是否有 "[OpenClaw] Plugin ready" 日志

# 打包发布（仅在 Git 仓库中）
git tag v1.0.0
git push origin v1.0.0
```

### 兼容性测试矩阵

在以下环境中验证：

| Unity 版本 | C# 版本 | .NET 版本 | WebSocket 实现 |
|-----------|---------|-----------|---------------|
| 2020.3 LTS | C# 7.3 | .NET Standard 2.0 | websocket-sharp |
| 2021.3 LTS | C# 8.0 | .NET Standard 2.1 | websocket-sharp |
| 2022.3 LTS | C# 9.0 | .NET Standard 2.1 | 内置 |
| 2023.2+ | C# 9.0 | .NET Standard 2.1 | 内置 |

---

## 常见陷阱

### 1. 主线程死锁

```csharp
// ❌ 死锁 — 在主线程中等待主线程任务
public void HandleBadExample(HttpContext ctx) {
    var result = MainThreadDispatcher.Dispatch(() => {
        // 如果这里又调用 Dispatch() 并 Wait()，会死锁！
        var nested = MainThreadDispatcher.Dispatch(() => ...).Result;
        return nested;
    });
}

// ✅ 正确 — 在 Dispatch 内部直接调用 Unity API
public void HandleGoodExample(HttpContext ctx) {
    var result = MainThreadDispatcher.Dispatch(() => {
        var go = GameObject.Find("Player");
        return go.transform.position;  // 直接返回，不嵌套 Dispatch
    });
}
```

### 2. GameObject 路径变更

```csharp
// ⚠️ 注意 — 如果父对象重命名，路径会变
var path = "Player/Head";  // Player 重命名为 MainPlayer 后，路径失效

// ✅ 最佳实践 — 在每次操作时重新查找
var go = GameObject.Find(path);
if (go == null) throw new Exception($"GameObject '{path}' not found");
```

### 3. 异步编译等待

```typescript
// ❌ 编译是异步的，立即查询会得到旧结果
await client.post("/api/v1/compile/trigger");
const errors = await client.get("/api/v1/compile/errors");  // 可能是旧的错误

// ✅ 使用 unity_compile 工具，自动等待编译完成（通过 WebSocket）
const result = await server.callTool("unity_compile", { timeoutSeconds: 60 });
```

### 4. Domain Reload 清理不完整

```csharp
// ❌ 只清理 HTTP Server，忘记清理 WebSocket Server
private static void Cleanup() {
    _httpServer?.Stop();
    // _wsServer?.Stop();  ← 忘记清理，导致端口占用
}

// ✅ 清理所有资源
private static void Cleanup() {
    _httpServer?.Stop();
    _wsServer?.Stop();
    _listener?.Stop();
}
```

---

## 调试技巧

### Unity Plugin 调试

```csharp
// 1. 在 Handler 中添加日志
Debug.Log($"[OpenClaw] HandleCreate: name={req.Name}");

// 2. 在 Unity Console 中过滤日志
// Filter: "[OpenClaw]"

// 3. 查看请求详情
Debug.Log($"[OpenClaw] Request body: {ctx.RequestBody}");
```

### MCP Server 调试

```bash
# 1. 使用 MCP Inspector 测试单个工具
npx @modelcontextprotocol/inspector dist/index.js

# 2. 查看环境变量
echo $UNITY_PORT
echo $UNITY_WS_PORT

# 3. 测试 HTTP 连接
curl -v http://127.0.0.1:23456/api/v1/status

# 4. 查看 MCP Server 日志（在 AI 工具的开发者工具中）
# Claude Desktop: Help > Developer Tools > Console
# Cursor: View > Output > MCP
```

---

## 遇到问题先查

- **Unity 2020.3 兼容性问题** → 查看 `COMPATIBILITY_FIXES.md`
- **连接失败** → 检查 Unity Editor 是否打开、插件是否成功导入
- **编译超时** → 增加 `timeoutSeconds` 参数或等待 Domain Reload 完成
- **WebSocket 不可用** → 检查 Unity 版本和 websocket-sharp 配置

---

## License

MIT
