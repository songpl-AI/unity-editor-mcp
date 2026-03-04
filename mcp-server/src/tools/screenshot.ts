import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { z } from "zod";
import { UnityClient } from "../unity-client.js";

export function registerScreenshotTools(server: McpServer, client: UnityClient): void {
  server.registerTool("unity_take_screenshot", {
    description:
      "Take a screenshot of the Unity Editor. Three modes are available:\n" +
      "  'game' (default) — renders the scene camera to a texture; works in both Edit and Play mode.\n" +
      "  'game_window' — reads the actual Game View panel pixels via reflection; Edit mode, requires the Game View panel to be open. Width/height are ignored (uses the Game View's own resolution).\n" +
      "  'scene' — renders the Scene View camera to a texture; works in both Edit and Play mode.",
    inputSchema: {
      view: z
        .enum(["game", "game_window", "scene"])
        .optional()
        .describe(
          "Which view to capture:\n" +
          "  'game' (default) — camera render, Edit + Play mode, respects width/height\n" +
          "  'game_window' — Game View panel pixels via reflection, Edit mode only, ignores width/height\n" +
          "  'scene' — Scene View camera render, Edit + Play mode, respects width/height"
        ),
      width: z
        .number()
        .optional()
        .describe("Width in pixels for 'game' and 'scene' modes (default 1920). Ignored for 'game_window'."),
      height: z
        .number()
        .optional()
        .describe("Height in pixels for 'game' and 'scene' modes (default 1080). Ignored for 'game_window'."),
    },
  }, async ({ view, width, height }) => {
    await client.ensureConnected();
    const res = await client.get<{ base64: string }>("/screenshot", {
      view: view ?? "game",
      width: width ?? 1920,
      height: height ?? 1080,
    });

    if (!res.ok) {
      throw new Error(`Unity API Error [${res.error?.code}]: ${res.error?.message}`);
    }

    return {
      content: [
        {
          type: "image" as const,
          data: res.data.base64,
          mimeType: "image/png",
        },
      ],
    };
  });
}
