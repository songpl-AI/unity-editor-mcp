using System;
using System.Reflection;
using UnityEngine;
using UnityEditor;

namespace OpenMCP.UnityPlugin
{
    public class ScreenshotHandler
    {
        public void HandleCapture(HttpContext ctx)
        {
            string view = ctx.Query("view", "game");
            int width = 1920;
            int height = 1080;
            var wStr = ctx.Query("width");
            if (wStr != null && int.TryParse(wStr, out var wi)) width = wi;
            var hStr = ctx.Query("height");
            if (hStr != null && int.TryParse(hStr, out var hi)) height = hi;

            string base64Image = null;
            try
            {
                base64Image = MainThreadDispatcher.Dispatch(() =>
                {
                    switch (view)
                    {
                        case "scene":       return CaptureSceneView(width, height);
                        case "game_window": return CaptureGameViewWindow();
                        default:            return CaptureGameCamera(width, height);
                    }
                });
            }
            catch (Exception ex)
            {
                ResponseHelper.WriteError(ctx.Response, ErrorCode.ExecutionFailed, ex.Message);
                return;
            }

            ResponseHelper.WriteSuccess(ctx.Response, new { base64 = base64Image });
        }

        // Method A: 渲染场景摄像机 — Edit 和 Play 模式均可用
        private string CaptureGameCamera(int width, int height)
        {
            // Play 模式优先使用 ScreenCapture，可包含后处理效果
            if (EditorApplication.isPlaying)
            {
                var texture = ScreenCapture.CaptureScreenshotAsTexture();
                if (texture != null)
                {
                    try   { return Convert.ToBase64String(texture.EncodeToPNG()); }
                    finally { UnityEngine.Object.DestroyImmediate(texture); }
                }
            }

            // Edit 模式或 ScreenCapture 失败：查找最佳可用摄像机
            Camera cam = Camera.main;
            if (cam == null)
            {
                // 遍历所有激活摄像机，取 depth 最高的
                foreach (var c in Camera.allCameras)
                    if (cam == null || c.depth > cam.depth) cam = c;
            }

            if (cam == null)
                throw new Exception("No active camera found in the scene. Add a Camera component or switch to Play mode.");

            return CaptureCamera(cam, width, height);
        }

        // Method B: 反射读取 Game View 面板实际像素 — 需要 Game View 已打开
        private string CaptureGameViewWindow()
        {
            var gameViewType = Type.GetType("UnityEditor.GameView,UnityEditor");
            if (gameViewType == null)
                throw new Exception("Cannot access GameView type.");

            // 查找已打开的 Game View 窗口，不创建新窗口
            var windows = Resources.FindObjectsOfTypeAll(gameViewType);
            if (windows == null || windows.Length == 0)
                throw new Exception("Game View panel is not open. Open it via Window > General > Game.");

            var gameView = windows[0] as EditorWindow;
            gameView.Repaint();

            // 依次尝试不同 Unity 版本中的内部字段名
            string[] candidateFields = { "m_RenderTexture", "m_TargetTexture", "targetTexture" };
            RenderTexture rt = null;
            foreach (var fieldName in candidateFields)
            {
                var field = gameViewType.GetField(fieldName,
                    BindingFlags.NonPublic | BindingFlags.Instance);
                if (field == null) continue;
                rt = field.GetValue(gameView) as RenderTexture;
                if (rt != null && rt.IsCreated()) break;
                rt = null;
            }

            if (rt == null)
                throw new Exception("Could not access Game View render texture. Your Unity version may not expose it via reflection. Use view='game' instead.");

            RenderTexture prevActive = RenderTexture.active;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            try
            {
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                tex.Apply();
                return Convert.ToBase64String(tex.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = prevActive;
                UnityEngine.Object.DestroyImmediate(tex);
            }
        }

        private string CaptureSceneView(int width, int height)
        {
            var view = SceneView.lastActiveSceneView;
            if (view == null && SceneView.sceneViews.Count > 0)
                view = (SceneView)SceneView.sceneViews[0];

            if (view == null)
                throw new Exception("No open Scene View found.");

            Camera cam = view.camera;
            if (cam == null)
                throw new Exception("Scene View camera not found.");

            return CaptureCamera(cam, width, height);
        }

        private string CaptureCamera(Camera cam, int width, int height)
        {
            RenderTexture rt = new RenderTexture(width, height, 24);
            RenderTexture prev = cam.targetTexture;
            Texture2D screenShot = null;
            try
            {
                cam.targetTexture = rt;
                cam.Render();
                cam.targetTexture = prev;

                RenderTexture.active = rt;
                screenShot = new Texture2D(width, height, TextureFormat.RGB24, false);
                screenShot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                screenShot.Apply();
                RenderTexture.active = null;

                return Convert.ToBase64String(screenShot.EncodeToPNG());
            }
            finally
            {
                if (screenShot != null) UnityEngine.Object.DestroyImmediate(screenShot);
                rt.Release();
                UnityEngine.Object.DestroyImmediate(rt);
            }
        }
    }
}
