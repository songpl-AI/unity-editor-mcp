using System;
using Newtonsoft.Json;
using UnityEngine;

namespace OpenMCP.UnityPlugin
{
    /// <summary>
    /// 向所有已连接的 WebSocket 客户端广播 Unity 编辑器事件。
    /// 可从任意线程安全调用，内部通过 IWebSocketServer.Broadcast() 发送。
    /// OnBroadcast 为 C# 端事件，供 Dashboard 窗口等内部订阅者使用。
    /// </summary>
    public static class EventBroadcaster
    {
        private static IWebSocketServer _wsServer;

        /// <summary>每次 Broadcast 调用时触发，参数为 eventName。始终触发，即使 WebSocket 未就绪。</summary>
        public static event Action<string> OnBroadcast;

        public static void Initialize(IWebSocketServer wsServer)
        {
            _wsServer = wsServer;
        }

        public static void Shutdown()
        {
            _wsServer = null;
        }

        /// <summary>
        /// 广播事件。eventName 对应技术分析 §2.4 中定义的事件类型。
        /// </summary>
        public static void Broadcast(string eventName, object data = null)
        {
            // 始终通知 C# 端订阅者（如 Dashboard 窗口），即使 WS 未就绪
            OnBroadcast?.Invoke(eventName);

            if (_wsServer == null) return;
            try
            {
                var message = JsonConvert.SerializeObject(new
                {
                    @event    = eventName,
                    timestamp = DateTime.UtcNow.ToString("o"),
                    data      = data ?? new object()
                });
                _wsServer.Broadcast(message);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[OpenClaw] EventBroadcaster failed to send '{eventName}': {ex.Message}");
            }
        }
    }
}
