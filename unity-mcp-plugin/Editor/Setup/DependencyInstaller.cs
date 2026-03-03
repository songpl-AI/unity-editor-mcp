using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace OpenMCP.UnityPlugin.Setup
{
    /// <summary>
    /// Automatically installs Newtonsoft.Json (com.unity.nuget.newtonsoft-json) on first import.
    ///
    /// This script lives in a separate Assembly Definition (OpenMCPUnityPlugin.Setup) with NO
    /// reference to Newtonsoft.Json, so it compiles and runs even when the main plugin assembly
    /// fails to compile because Newtonsoft.Json is missing.
    /// </summary>
    [InitializeOnLoad]
    internal static class DependencyInstaller
    {
        private const string NewtonsoftPackageId = "com.unity.nuget.newtonsoft-json";

        private static ListRequest s_listRequest;
        private static AddRequest s_addRequest;

        static DependencyInstaller()
        {
            // delayCall: 等 PackageManager 数据库在本次域重载中完全加载后再检查
            EditorApplication.delayCall += CheckDependencies;
        }

        private static void CheckDependencies()
        {
            // Unity 2020.3 兼容：使用 Client.List() 而非 PackageInfo.FindForPackageName()
            s_listRequest = Client.List();
            EditorApplication.update += WaitForList;
        }

        private static void WaitForList()
        {
            if (!s_listRequest.IsCompleted) return;
            EditorApplication.update -= WaitForList;

            if (s_listRequest.Status == StatusCode.Success)
            {
                // 检查包是否已安装
                foreach (var package in s_listRequest.Result)
                {
                    if (package.name == NewtonsoftPackageId)
                    {
                        // 已安装，无需操作
                        return;
                    }
                }

                // 未安装，开始安装
                Debug.Log($"[OpenMCP Unity Plugin] Required package '{NewtonsoftPackageId}' not found. Installing automatically...");
                s_addRequest = Client.Add(NewtonsoftPackageId);
                EditorApplication.update += WaitForInstall;
            }
            else
            {
                Debug.LogError($"[OpenMCP Unity Plugin] Failed to list packages: {s_listRequest.Error?.message}");
            }
        }

        private static void WaitForInstall()
        {
            if (!s_addRequest.IsCompleted) return;
            EditorApplication.update -= WaitForInstall;

            if (s_addRequest.Status == StatusCode.Success)
            {
                Debug.Log($"[OpenMCP Unity Plugin] ✅ '{NewtonsoftPackageId}' installed successfully. Unity will recompile now.");
            }
            else
            {
                Debug.LogError(
                    $"[OpenMCP Unity Plugin] ❌ Auto-install failed: {s_addRequest.Error?.message}\n" +
                    $"Please install it manually:\n" +
                    $"  Window → Package Manager → '+' → Add package by name → {NewtonsoftPackageId}"
                );
            }
        }
    }
}
