# Bug 报告：OpenMCP 缺少 Asset 删除接口及 ErrorCode 定义缺失

## 问题描述
OpenMCP 目前缺少删除 Asset 的 API (`/api/v1/asset/delete`)。在尝试扩展此功能时，发现 `ErrorCode` 类中缺少通用的 `InternalError` 定义，导致扩展代码编译失败。此外，`AssetHandler.cs` 中也未实现 `HandleDelete` 方法。

## 环境信息
- **操作系统**：macos
- **Unity 版本**：6000.3.10f1
- **插件版本**：com.openmcp.unity-editor-mcp@68c40812a804

## 复现步骤
1.  尝试在 `AssetHandler.cs` 中添加 `HandleDelete` 方法。
2.  使用 `ErrorCode.InternalError` 作为操作失败的错误码。
3.  编译项目，出现报错：`ErrorCode` does not contain a definition for `InternalError`.
4.  检查 `RequestRouter.cs`，发现未注册 `/api/v1/asset/delete` 路由。

## 预期行为
1.  `AssetHandler` 应包含 `HandleDelete` 方法以支持删除资源。
2.  `RequestRouter` 应注册 `/api/v1/asset/delete` 路由。
3.  `ErrorCode` 应包含 `InternalError` 或其他通用错误码定义，以便在操作失败时使用。

## 实际行为
1.  缺少 `HandleDelete` 方法。
2.  缺少路由注册。
3.  `ErrorCode` 类中未定义 `InternalError`，导致编译失败。

## 根因分析
- **文件**：`Editor/Handlers/AssetHandler.cs`, `Editor/Core/RequestRouter.cs`, `Editor/Models/ApiResponse.cs`
- **原因**：OpenMCP 尚未实现资源删除功能的 API 及其路由，且基础错误码定义不完整，导致开发者扩展时遇到编译错误。

## 修复方案
建议在 `AssetHandler.cs` 中添加 `HandleDelete` 方法，并在 `RequestRouter.cs` 中注册对应路由。同时，在 `ApiResponse.cs` 的 `ErrorCode` 类中添加 `InternalError` 定义，或者统一使用现有的 `ExecutionFailed`。

### 代码对比

#### 1. `Editor/Handlers/AssetHandler.cs`

```csharp
// 新增 HandleDelete 方法
public void HandleDelete(HttpContext ctx)
{
    var req = ctx.ParseBody<DeleteRequest>();
    if (string.IsNullOrEmpty(req.Path))
    {
        ResponseHelper.WriteError(ctx.Response, ErrorCode.InvalidParams, "'path' is required");
        return;
    }
    var success = MainThreadDispatcher.Dispatch(() => AssetDatabase.DeleteAsset(req.Path));
    if (success) ResponseHelper.WriteSuccess(ctx.Response, new { deleted = req.Path });
    else ResponseHelper.WriteError(ctx.Response, ErrorCode.ExecutionFailed, "Failed to delete asset");
}

// ...

// 新增 Request DTO
private class DeleteRequest { [JsonProperty("path")] public string Path { get; set; } }
```

#### 2. `Editor/UnityEditorServer.cs` (或 `RequestRouter.cs` 初始化处)

```csharp
// 注册路由
router.Register("POST", "/api/v1/asset/delete", assetHandler.HandleDelete);
```

#### 3. `Editor/Models/ApiResponse.cs`

```csharp
// (可选) 如果需要更具体的错误码
public static class ErrorCode
{
    // ...
    public const string InternalError = "INTERNAL_ERROR"; // 建议添加
}
```

## 验证
通过 `curl` 命令调用接口验证：
```bash
curl -X POST "http://localhost:23456/api/v1/asset/delete" \
  -H "Content-Type: application/json" \
  -d '{"path": "Assets/Scripts/TestScript.cs"}'
```
预期返回：`{"ok":true,"data":{"deleted":"Assets/Scripts/TestScript.cs"},"error":null}`
