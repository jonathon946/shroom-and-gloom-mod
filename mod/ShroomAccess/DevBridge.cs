using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace ShroomAccess
{
    // Development aid, off unless UserData\ShroomAccess\dev.flag exists.
    // The developer's tools cannot see the screen (and a screen curtain blanks desktop captures), so
    // they drop one command per line into dev-command.txt and the mod carries it out in-game:
    //   shot <path>     save the game's rendered frame as a PNG
    //   tree <path>     write the active UI hierarchy with its text to a file
    //   move <x> <y> <seconds>  hold the walk stick
    //   state <path>    write the focus system's state and candidates to a file
    //   press <action>  simulate one press of a game action (Confirm, FocusNext, ...)
    internal static class DevBridge
    {
        internal static Action<string> Log = _ => { };

        private static string _dir;
        private static bool _enabled;
        private static float _nextCheck;

        internal static void Init(string userDataDir)
        {
            _dir = Path.Combine(userDataDir, "ShroomAccess");
            _enabled = File.Exists(Path.Combine(_dir, "dev.flag"));
            if (_enabled) Log("[dev] bridge on: " + _dir);
        }

        internal static void Tick()
        {
            if (!_enabled || Time.realtimeSinceStartup < _nextCheck) return;
            _nextCheck = Time.realtimeSinceStartup + 0.25f;
            string file = Path.Combine(_dir, "dev-command.txt");
            if (!File.Exists(file)) return;
            string[] lines;
            try
            {
                lines = File.ReadAllLines(file);
                File.Delete(file);
            }
            catch { return; }

            foreach (string raw in lines)
            {
                string line = raw.Trim();
                if (line.Length == 0) continue;
                int space = line.IndexOf(' ');
                string cmd = space < 0 ? line : line.Substring(0, space);
                string arg = space < 0 ? "" : line.Substring(space + 1).Trim();
                try
                {
                    switch (cmd)
                    {
                        case "shot":
                            ScreenCapture.CaptureScreenshot(arg);
                            Log("[dev] shot " + arg);
                            break;
                        case "tree":
                            File.WriteAllText(arg, UiTree());
                            Log("[dev] tree " + arg);
                            break;
                        case "move":
                        {
                            string[] parts = arg.Split(' ');
                            var inv = System.Globalization.CultureInfo.InvariantCulture;
                            InputBridge.InjectMove(float.Parse(parts[0], inv), float.Parse(parts[1], inv), float.Parse(parts[2], inv));
                            Log("[dev] move " + arg);
                            break;
                        }
                        case "obj":
                        {
                            // obj <file>|<GameObject name>: dump one object's subtree with component types.
                            string[] parts = arg.Split('|');
                            GameObject go = GameObject.Find(parts[1]);
                            var sb = new StringBuilder();
                            if (go == null) sb.AppendLine("not found: " + parts[1]);
                            else
                            {
                                sb.AppendLine(go.name + " active=" + go.activeInHierarchy);
                                Walk(go.transform.parent != null ? go.transform.parent : go.transform, 1, sb);
                            }
                            File.WriteAllText(parts[0], sb.ToString());
                            Log("[dev] obj " + parts[1]);
                            break;
                        }
                        case "state":
                            File.WriteAllText(arg, FocusProbe.StateDump());
                            Log("[dev] state " + arg);
                            break;
                        case "press":
                            InputBridge.Inject((Il2Cpp.InputActionId)Enum.Parse(typeof(Il2Cpp.InputActionId), arg, true));
                            Log("[dev] press " + arg);
                            break;
                        default:
                            Log("[dev] unknown command: " + line);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Log("[dev] " + cmd + " failed: " + ex.GetType().Name + ": " + ex.Message);
                }
            }
        }

        // Every active canvas, indented, with component type names and any text.
        private static string UiTree()
        {
            var sb = new StringBuilder();
            foreach (Canvas canvas in UnityEngine.Object.FindObjectsOfType<Canvas>())
            {
                if (canvas == null || !canvas.isRootCanvas || !canvas.gameObject.activeInHierarchy) continue;
                sb.AppendLine("CANVAS " + canvas.gameObject.scene.name + " :: " + canvas.name + " (" + canvas.renderMode + ")");
                Walk(canvas.transform, 1, sb);
            }
            return sb.ToString();
        }

        private static void Walk(Transform t, int depth, StringBuilder sb)
        {
            for (int i = 0; i < t.childCount; i++)
            {
                Transform c = t.GetChild(i);
                if (!c.gameObject.activeInHierarchy) continue;
                sb.Append(' ', depth * 2).Append(c.name);
                foreach (Component comp in c.GetComponents<Component>())
                {
                    if (comp == null) continue;
                    string type = comp.GetIl2CppType().Name;
                    if (type == "RectTransform" || type == "CanvasRenderer" || type == "Transform") continue;
                    sb.Append(" [").Append(type).Append(']');
                    var text = comp.TryCast<Il2CppTMPro.TMP_Text>();
                    if (text != null && !string.IsNullOrWhiteSpace(text.text))
                        sb.Append(" \"").Append(text.text.Replace("\n", "\\n")).Append('"');
                }
                sb.AppendLine();
                if (depth < 14) Walk(c, depth + 1, sb);
            }
        }
    }
}
