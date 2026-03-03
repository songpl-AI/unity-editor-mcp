# Unity 2020.3 兼容性修复记录

## 问题概述

Unity 2020.3 使用 .NET Standard 2.0 和 C# 7.3，不支持以下 C# 8.0+ 特性和 Unity 2021+ API：

1. `Dictionary<TKey, TValue>.GetValueOrDefault()` 方法（.NET Standard 2.1+）
2. Switch 表达式语法（C# 8.0+）
3. Null 合并赋值运算符 `??=`（C# 8.0+）
4. Record 类型（C# 9.0+）
5. `init` 关键字（C# 9.0+）
6. `PackageInfo.FindForPackageName()` 静态方法（Unity 2021.2+）

## 已修复的文件

### 1. `Editor/Handlers/GameObjectHandler.cs`

**问题：** 使用了 `GetValueOrDefault()` 和 switch 表达式

**修复：**
- 将所有 `ctx.PathParams.GetValueOrDefault("key", "default")` 替换为：
  ```csharp
  ctx.PathParams.TryGetValue("key", out var value) ? value : "default"
  ```
- 将 `SerializedPropertyToObject()` 中的 switch 表达式改为传统 switch 语句

**影响行数：** 130, 145, 146, 169, 184, 201, 202, 254-265

### 2. `Editor/Core/ConsoleLogger.cs`

**问题：** 使用了 switch 表达式

**修复：**
- 将 `MapLogType()` 中的 switch 表达式改为传统 switch 语句

**影响行数：** 69-75

### 3. `Editor/Setup/DependencyInstaller.cs`

**问题：** 使用了 `PackageInfo.FindForPackageName()` 静态方法（Unity 2021.2+ 才引入）

**修复：**
- 改为使用 Unity 2020.3 兼容的 `Client.List()` 异步查询方式
- 添加 `WaitForList()` 回调处理查询结果
- 在查询结果中遍历查找目标包

**修复前：**
```csharp
var info = UnityEditor.PackageManager.PackageInfo.FindForPackageName(NewtonsoftPackageId);
if (info != null) return; // 已安装
```

**修复后：**
```csharp
s_listRequest = Client.List();
EditorApplication.update += WaitForList;

private static void WaitForList() {
    if (!s_listRequest.IsCompleted) return;
    foreach (var package in s_listRequest.Result) {
        if (package.name == NewtonsoftPackageId) return; // 已安装
    }
    // 未安装，开始安装...
}
```

**影响行数：** 20-37

## 兼容性检查清单

✅ 无 `GetValueOrDefault()` 调用  
✅ 无 switch 表达式语法  
✅ 无 `??=` 运算符  
✅ 无 `record` 类型  
✅ 无 `init` 关键字  
✅ 无 `PackageInfo.FindForPackageName()` 调用  
✅ 所有代码使用 C# 7.3 兼容语法  
✅ 所有 Unity API 使用 2020.3 兼容版本  

## 测试建议

1. 在 Unity 2020.3 LTS 中导入插件
2. 检查 Console 无编译错误
3. 测试 GameObject 相关 API（创建、查询、组件操作）
4. 验证日志输出功能正常
5. 验证 Newtonsoft.Json 自动安装功能（删除包后重新导入插件）

## Unity Package Manager API 版本差异

| API | Unity 2020.3 | Unity 2021.2+ |
|-----|--------------|---------------|
| 查询单个包 | `Client.List()` + 遍历 | `PackageInfo.FindForPackageName()` |
| 查询所有包 | `Client.List()` | `Client.List()` 或 `PackageInfo.GetAllRegisteredPackages()` |
| 添加包 | `Client.Add()` | `Client.Add()` |
| 移除包 | `Client.Remove()` | `Client.Remove()` |

**最佳实践：** 使用 `Client.List()` + 遍历的方式在所有 Unity 版本中都兼容。

## 未来开发注意事项

- 保持代码使用 C# 7.3 语法
- 避免使用 .NET Standard 2.1+ 独有的 API
- 避免使用 Unity 2021+ 独有的 API
- 在添加新功能时检查 Unity 2020.3 兼容性
- 使用条件编译符号处理版本差异（如 WebSocket 实现）
- 查阅 Unity 官方文档确认 API 的最低支持版本
