# Changelog

All notable changes to Unity Editor MCP Plugin will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.1.0] - 2026-03-03

### Added

- 新增 Asset 删除功能（`/api/v1/asset/delete`）
  - MCP 工具：`unity_delete_asset`
  - 支持删除 Assets/ 目录下的任何资产文件或文件夹
  - 自动路径安全校验（限制在 Assets/ 目录内）
  - 自动资产存在性检查
  - 使用 `AssetDatabase.DeleteAsset()` 实现，确保 Unity 元数据同步
  - 感谢用户反馈（Bug Report: `BugReports/bug_report_openmcp_asset_delete.md`）

### Changed

- 工具总数从 31 个增加到 32 个

## [1.0.0] - 2026-03-03

### Added

- 初始发布版本
- HTTP REST API 服务器（31 个端点）
- WebSocket 服务器（事件推送）
- Dashboard 窗口（Window > Open MCP）
- 自动安装 Newtonsoft.Json 依赖（v3.0.2，兼容 Unity 2019.4+）
- Unity 2020.3+ 兼容性支持
- Domain Reload 自动清理机制
- 主线程调度器（MainThreadDispatcher）
- Undo 系统集成

### Fixed

- **Critical Bug:** 修复参数化路由无法匹配的问题（`RequestRouter.cs`）
  - 影响所有包含 `:param` 的路由（5 个 GameObject 组件相关端点）
  - 根本原因：`Regex.Escape()` 不会转义冒号 `:`，导致路由模式无法匹配实际请求
  - 解决方案：修改正则表达式从 `@"\\:(\w+)"` 为 `@"(?:\\:|:)(\w+)"`，同时支持转义和未转义的冒号

### Supported Operations

- 场景管理（获取信息、层级结构、保存场景）
- GameObject 操作（创建、删除、查找、变换）
- 组件管理（获取、添加、设置属性）
- 文件 I/O（读写 Assets/ 目录）
- 编译控制（触发编译、获取错误）
- Console 日志获取
- 资产搜索与删除
- Tag 管理
- 输入系统检测
- Player Settings 查询
- 渲染管线检测
- 材质属性操作
- 包管理（列出、安装、卸载）

### Security

- 服务器仅绑定 127.0.0.1（不暴露外网）
- 文件操作限制在 Assets/ 目录
- Host 头严格校验

## [Unreleased]

### Planned

- 预制体实例化支持
- Animator 参数设置
- Physics 模拟控制
- 更多组件属性编辑器
