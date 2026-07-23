using UnityEngine;

namespace OCC.Combat.Presentation
{
    // Debug controls are deliberately opt-in so the player-facing HUD stays free of test affordances.
    public sealed class DeveloperConsolePanel : MonoBehaviour
    {
        private CombatPrototypeBootstrap bootstrap;
        private bool open;

        public void Initialize(CombatPrototypeBootstrap source) { bootstrap = source; }
        public void Toggle() { open = !open; }

        private void OnGUI()
        {
            if (bootstrap == null || !Application.isPlaying) return;
            if (Event.current != null && Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.F1)
            {
                Toggle();
                Event.current.Use();
                if (!open) return;
            }
            if (!open) return;
            float scale = Mathf.Min(Screen.width / 1920f, Screen.height / 1080f);
            Matrix4x4 previous = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(new Vector2((Screen.width - 1920f * scale) * .5f, (Screen.height - 1080f * scale) * .5f), Quaternion.identity, Vector3.one * scale);
            Rect panel = new Rect(1280, 130, 560, 420);
            GUI.color = new Color(.018f, .025f, .03f, .97f); GUI.Box(panel, "");
            GUI.color = new Color(.95f, .72f, .24f); GUI.DrawTexture(new Rect(panel.x, panel.y, panel.width, 3), Texture2D.whiteTexture);
            GUI.color = new Color(.92f, .95f, .96f); GUI.Label(new Rect(panel.x + 24, panel.y + 22, 470, 28), "开发控制台 // F1 关闭");
            GUI.color = new Color(.58f, .65f, .68f); GUI.Label(new Rect(panel.x + 24, panel.y + 58, 490, 42), "调试功能不属于正式战斗 HUD；关闭后不遮挡战场和指令区。");
            GUI.color = Color.white;
            if (GUI.Button(new Rect(panel.x + 24, panel.y + 124, 240, 54), "战术重开")) bootstrap.TacticalRestartDeveloperCombat();
            if (GUI.Button(new Rect(panel.x + 286, panel.y + 124, 240, 54), "返回入口")) bootstrap.ReturnToDeveloperMenu();
            GUI.enabled = bootstrap.IsDeveloperCombatActive;
            if (GUI.Button(new Rect(panel.x + 24, panel.y + 196, 240, 54), "测试胜利")) bootstrap.ForceCurrentOutcome(true);
            if (GUI.Button(new Rect(panel.x + 286, panel.y + 196, 240, 54), "测试失败")) bootstrap.ForceCurrentOutcome(false);
            GUI.enabled = true;
            if (GUI.Button(new Rect(panel.x + 24, panel.y + 300, 502, 50), "关闭控制台")) open = false;
            GUI.matrix = previous;
        }
    }
}
