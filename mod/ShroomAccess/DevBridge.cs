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
        internal static bool Enabled => _enabled;
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
            if (_enabled) TickPending();
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
                        case "click":
                        {
                            // click <GameObject name>: press a UI button as a mouse click would.
                            GameObject target = GameObject.Find(arg);
                            UnityEngine.UI.Button button = target != null ? target.GetComponent<UnityEngine.UI.Button>() : null;
                            if (button != null) button.onClick.Invoke();
                            Log("[dev] click " + arg + (button != null ? "" : " (no such button)"));
                            break;
                        }
                        // ---- Setting up a situation to test, using the game's own debug entry points ----
                        case "addcard":
                            Player().CMDAddCardByName(arg);
                            Log("[dev] addcard " + arg);
                            break;
                        case "polled":
                            Log("[dev] polled " + string.Join(" ", InputBridge.Polled));
                            InputBridge.Polled.Clear();
                            break;
                        case "roundup":
                        {
                            Il2Cpp.RoundUpUIManager roundUp = UnityEngine.Object.FindObjectOfType<Il2Cpp.RoundUpUIManager>(true);
                            if (roundUp != null) roundUp.ShowRoundUp();
                            Log("[dev] roundup " + (roundUp != null));
                            break;
                        }
                        case "die":
                            Player().CMDDie();
                            Log("[dev] die");
                            break;
                        case "pack":
                        {
                            Il2Cpp.CardUnlockPack pack = UnityEngine.Object.FindObjectOfType<Il2Cpp.CardUnlockPack>();
                            if (pack != null) pack.OnPointerClick(new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current));
                            Log("[dev] pack " + (pack != null ? "clicked enabled=" + pack.EnabledForClick : "none"));
                            break;
                        }
                        case "energy":
                            Player().AddEnergy(int.Parse(arg));
                            Log("[dev] energy +" + arg);
                            break;
                        case "heal":
                            Player().CMDFullHeal();
                            Log("[dev] heal");
                            break;
                        case "killall":
                            Player().DebugKillAll();
                            Log("[dev] killall");
                            break;
                        case "list":
                            Log("[dev] " + Listing());
                            break;
                        case "focus":
                            Log("[dev] focus " + arg + " -> " + FocusOn(arg));
                            break;
                        case "state":
                            File.WriteAllText(arg, FocusProbe.StateDump());
                            Log("[dev] state " + arg);
                            break;
                        case "press":
                        {
                            // press <action>       a key press as the player would make it
                            // press ctrl+<action>  the same with Ctrl held
                            bool ctrl = arg.StartsWith("ctrl+", StringComparison.OrdinalIgnoreCase);
                            string name = ctrl ? arg.Substring(5) : arg;
                            InputBridge.Simulate((Il2Cpp.InputActionId)Enum.Parse(typeof(Il2Cpp.InputActionId), name, true), ctrl);
                            Log("[dev] press " + arg);
                            break;
                        }
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

        private static Il2CppControllerNav.S_ControllerFocus Focus()
        {
            Il2CppControllerNav.S_ControllerFocus.TryGetFocus(out Il2CppControllerNav.S_ControllerFocus focus);
            if (focus == null) throw new InvalidOperationException("no focus service");
            return focus;
        }

        private static Il2CppBackend.M_Player Player()
        {
            Il2CppBackend.M_Player p = Focus().PlayerModel;
            if (p == null) throw new InvalidOperationException("no player yet");
            return p;
        }

        // What can be focused right now, by name: the hand and what is nearby.
        private static string Listing()
        {
            var focus = Focus();
            var hand = new System.Collections.Generic.List<string>();
            Il2Cpp.IM_Hand active = focus.PlayerModel != null ? focus.PlayerModel.GetActiveHand() : null;
            for (int i = 0; active != null && active.HeldCards != null && i < active.HeldCards.Count; i++) hand.Add(active.HeldCards[i].CardName);
            var near = new System.Collections.Generic.List<string>();
            foreach (var t in Navigator.NearbyInOrder(focus)) near.Add(FocusProbe.Describe(t, out _).Split('.')[0]);
            return "mode=" + focus.Mode + " hand=[" + string.Join(", ", hand) + "] nearby=[" + string.Join(", ", near) + "]";
        }

        // Put the game's focus on a card in hand or a thing nearby whose name contains the text,
        // so a test can say "the Grimoire" and not "Right three times".
        private static Il2CppControllerNav.IControllerFocusTarget _pendingTarget;
        private static int _pendingFrames;

        private static string FocusOn(string text)
        {
            var focus = Focus();
            Il2Cpp.IM_Hand active = focus.PlayerModel != null ? focus.PlayerModel.GetActiveHand() : null;
            for (int i = 0; active != null && active.HeldCards != null && i < active.HeldCards.Count; i++)
            {
                if (active.HeldCards[i].CardName.IndexOf(text, StringComparison.OrdinalIgnoreCase) < 0) continue;
                if (focus.Mode.ToString() != "Hand" && focus.Mode != Il2CppControllerNav.FocusMode.Target) focus.TryPushHand();
                if (focus.Mode == Il2CppControllerNav.FocusMode.Target) focus.TargetResolver.SetCardFocus(i, "dev");
                else focus.HandResolver.SetFocus(i);
                return "hand card " + i + " " + active.HeldCards[i].CardName;
            }
            foreach (var t in Navigator.NearbyInOrder(focus))
            {
                string name = FocusProbe.Describe(t, out _);
                if (name.IndexOf(text, StringComparison.OrdinalIgnoreCase) < 0) continue;
                if (focus.Mode.ToString() == "World" || focus.Mode == Il2CppControllerNav.FocusMode.Target) focus.WorldResolver.SetFocus(t, "dev");
                else
                {
                    // Step into the world row the way the player would, then land on the thing.
                    InputBridge.Forward(Il2Cpp.InputActionId.FocusUp);
                    _pendingTarget = t;
                    _pendingFrames = 6;
                }
                return "nearby " + name.Split('.')[0];
            }
            return "nothing matches";
        }

        private static void TickPending()
        {
            if (_pendingTarget == null || --_pendingFrames > 0) return;
            try { Focus().WorldResolver.SetFocus(_pendingTarget, "dev"); } catch { }
            _pendingTarget = null;
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
                    if (type == "TooltipUI")
                    {
                        string tip = null;
                        try { tip = comp.TryCast<Il2Cpp.TooltipUI>().GetTooltip(); } catch { }
                        if (!string.IsNullOrWhiteSpace(tip)) sb.Append(" tip=\"").Append(tip.Replace("\n", "\\n")).Append('"');
                    }
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
