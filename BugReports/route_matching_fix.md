# OpenMCP Unity Plugin Bug Report: Route Matching Failure with Parameters

**状态：✅ 已修复（2026-03-03）**

## Issue Description
Requests to routes containing parameters (e.g., `:path`, `:type`) fail with `404 NOT FOUND`, even when the route is correctly registered.

**Example:**
- **Registered Route:** `POST /api/v1/gameobject/:path/component/add`
- **Request URL:** `POST /api/v1/gameobject/Cube/component/add`
- **Result:** `404 NOT FOUND` "No route for POST ..."

## Affected File
`Editor/Core/RequestRouter.cs`

## Root Cause Analysis
The issue lies in the `RequestRouter.RouteEntry` constructor, specifically in how it converts route patterns (like `/api/:id`) into regular expressions.

1. The code uses `Regex.Escape(pattern)` to escape the route string.
2. It then attempts to find parameter placeholders (e.g., `:id`) and replace them with regex named groups.
3. The original regex pattern `@"\\:(\w+)"` expects the colon `:` to always be escaped as `\:` by `Regex.Escape`.
4. However, if `Regex.Escape` does not escape the colon (which can happen depending on the implementation or context), or if the logic is strictly looking for the escaped backslash, the parameter placeholder is not detected.
5. Consequently, the route remains a literal string (e.g., matching literal `.../:path/...`) instead of a regex pattern matching dynamic content, causing the match to fail for actual values.

## Fix
Update the regex replacement pattern to match both escaped (`\:`) and unescaped (`:`) colons preceding the parameter name.

### Code Diff
```csharp
// Editor/Core/RequestRouter.cs

// ... inside RouteEntry constructor ...

// OLD CODE:
// Only matches escaped colons (e.g., \:param)
var regexStr = "^" + Regex.Replace(
    escaped,
    @"\\:(\w+)", 
    m => { paramNames.Add(m.Groups[1].Value); return $"(?<{m.Groups[1].Value}>[^/]+)"; }
) + "$";

// NEW CODE:
// Matches both escaped (\:param) and unescaped (:param) colons
var regexStr = "^" + Regex.Replace(
    escaped,
    @"(?:\\:|:)(\w+)", 
    m => { paramNames.Add(m.Groups[1].Value); return $"(?<{m.Groups[1].Value}>[^/]+)"; }
) + "$";
```

## Verification
After applying this fix, the `Regex` correctly compiles with named groups for parameters, and requests like `POST /api/v1/gameobject/Cube/component/add` are correctly routed to `HandleAddComponent` with `path="Cube"`.

## Fix Applied
- **Date:** 2026-03-03
- **File Modified:** `unity-mcp-plugin/Editor/Core/RequestRouter.cs`
- **Lines Changed:** 71-79
- **Impact:** All 5 parameterized routes now work correctly:
  - `GET /api/v1/gameobject/:path/components`
  - `GET /api/v1/gameobject/:path/component/:type/values`
  - `POST /api/v1/gameobject/:path/component/:type/values`
  - `POST /api/v1/gameobject/:path/component/add`
  - `POST /api/v1/gameobject/:path/component/remove`
