using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace OpenMCP.UnityPlugin
{
    /// <summary>
    /// Unity Editor 窗口，显示 MCP 服务器状态，支持手动启停。
    /// 通过 Window > Open MCP 打开。
    /// </summary>
    public class McpDashboardWindow : EditorWindow
    {
        private Vector2 _logScroll;

        [MenuItem("Window/Open MCP")]
        public static void ShowWindow()
        {
            var w = GetWindow<McpDashboardWindow>("Open MCP");
            w.minSize = new Vector2(340, 320);
        }

        private void OnEnable()
        {
            EventBroadcaster.OnBroadcast += OnEventReceived;
        }

        private void OnDisable()
        {
            EventBroadcaster.OnBroadcast -= OnEventReceived;
        }

        // 收到 WS 事件时强制刷新（无论 WS 是否连接，OnBroadcast 始终触发）
        private void OnEventReceived(string _) => Repaint();

        // 每秒约 10 次刷新，保持实时状态
        private void OnInspectorUpdate() => Repaint();

        private void OnGUI()
        {
            bool running  = UnityEditorServer.IsRunning;
            int  httpPort = UnityEditorServer.HttpPort;
            int  wsPort   = UnityEditorServer.WsPort;

            // ────────────────────────────────────────────────────────
            // 状态栏
            // ────────────────────────────────────────────────────────
            EditorGUILayout.Space(6);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(8);

                // 状态指示灯
                var prevColor = GUI.color;
                GUI.color = running
                    ? new Color(0.18f, 0.83f, 0.28f)   // 绿
                    : new Color(0.85f, 0.25f, 0.25f);   // 红
                GUILayout.Label("●", GUILayout.Width(16));
                GUI.color = prevColor;

                GUILayout.Label(
                    running ? "Running" : "Stopped",
                    EditorStyles.boldLabel,
                    GUILayout.Width(68));

                GUILayout.FlexibleSpace();

                if (running)
                {
                    if (GUILayout.Button("Stop", GUILayout.Width(64)))
                        UnityEditorServer.StopServer();
                }
                else
                {
                    if (GUILayout.Button("Start", GUILayout.Width(64)))
                        UnityEditorServer.StartServer();
                }
                GUILayout.Space(8);
            }

            // URL 行（仅运行时显示）
            if (running)
            {
                EditorGUILayout.Space(4);
                DrawUrlRow("HTTP", $"http://127.0.0.1:{httpPort}");
                DrawUrlRow("WS",   $"ws://127.0.0.1:{wsPort}/ws");
            }

            // ────────────────────────────────────────────────────────
            // Unity 状态信息
            // ────────────────────────────────────────────────────────
            DrawDivider();

            string sceneName = EditorSceneManager.GetActiveScene().name;
            if (string.IsNullOrEmpty(sceneName)) sceneName = "(no scene)";

            string playState = EditorApplication.isPlaying ? "Play Mode" : "Edit Mode";

            string compileLabel;
            if (EditorApplication.isCompiling)
            {
                compileLabel = "Compiling...";
            }
            else
            {
                switch (CompilationListener.Status)
                {
                    case CompileStatus.Success:
                        var warnCount = CompilationListener.LastErrors.Count;
                        compileLabel = warnCount > 0
                            ? $"OK ({warnCount} warn)"
                            : "OK";
                        break;
                    case CompileStatus.Failed:
                        int errCount = 0;
                        foreach (var e in CompilationListener.LastErrors)
                            if (e.Type == "error") errCount++;
                        compileLabel = $"FAILED ({errCount} err)";
                        break;
                    case CompileStatus.Compiling:
                        compileLabel = "Compiling...";
                        break;
                    default:
                        compileLabel = "Idle";
                        break;
                }
            }

            EditorGUILayout.Space(3);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(8);
                GUILayout.Label(
                    $"{Application.unityVersion}  ·  {Application.productName}",
                    EditorStyles.miniLabel);
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(8);
                GUILayout.Label(
                    $"Scene: {sceneName}  ·  {playState}  ·  Compile: {compileLabel}",
                    EditorStyles.miniLabel);
            }
            EditorGUILayout.Space(3);

            // ────────────────────────────────────────────────────────
            // 活动日志
            // ────────────────────────────────────────────────────────
            DrawDivider();

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(8);
                GUILayout.Label("Activity Log", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Clear", EditorStyles.miniButton, GUILayout.Width(44)))
                    UnityEditorServer.ClearDashboardLog();
                GUILayout.Space(8);
            }

            _logScroll = EditorGUILayout.BeginScrollView(_logScroll, GUILayout.ExpandHeight(true));

            List<string> logs = UnityEditorServer.GetDashboardLogSnapshot();
            if (logs.Count == 0)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Space(8);
                    GUILayout.Label("No activity yet.", EditorStyles.miniLabel);
                }
            }
            else
            {
                foreach (var entry in logs)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Space(8);
                        GUILayout.Label(entry, EditorStyles.miniLabel);
                    }
                }
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.Space(4);
        }

        // ────────────────────────────────────────────────────────────
        // 辅助方法
        // ────────────────────────────────────────────────────────────

        private static void DrawUrlRow(string label, string url)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(8);
                GUILayout.Label(label, EditorStyles.miniLabel, GUILayout.Width(28));
                EditorGUILayout.SelectableLabel(
                    url,
                    EditorStyles.miniLabel,
                    GUILayout.Height(EditorGUIUtility.singleLineHeight));
                if (GUILayout.Button("Copy", EditorStyles.miniButton, GUILayout.Width(38)))
                    GUIUtility.systemCopyBuffer = url;
                GUILayout.Space(8);
            }
        }

        private static void DrawDivider()
        {
            EditorGUILayout.Space(2);
            var rect = EditorGUILayout.GetControlRect(false, 1f);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.35f));
            EditorGUILayout.Space(2);
        }
    }
}
