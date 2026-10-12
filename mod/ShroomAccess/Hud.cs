using System;
using System.Collections.Generic;
using Il2Cpp;
using Il2CppControllerNav;
using UnityEngine;

namespace ShroomAccess
{
    // Reads the corner displays a sighted player glances at (energy, health, warmth and threat) and
    // the game's own hover text for them, and notices when they change.
    internal static class Hud
    {
        internal static Action<string> Log = _ => { };

        private static S_PlayerValuesDisplay _display;

        private static S_PlayerValuesDisplay Display()
        {
            if (_display == null) _display = UnityEngine.Object.FindObjectOfType<S_PlayerValuesDisplay>();
            return _display;
        }

        // The warmth and threat readout, as one spoken line; empty when the game is not showing it.
        internal static string Threat()
        {
            try
            {
                S_PlayerValuesDisplay d = Display();
                if (d == null || d._hazardThreatRoot == null || !d._hazardThreatRoot.gameObject.activeInHierarchy) return "";
                TooltipUI tip = d._hazardThreatTooltip;
                return tip != null ? string.Join(". ", Lines(tip.GetTooltip())) : "";
            }
            catch { return ""; }
        }

        // The game's hover text for one of the corner displays, by the display's object name.
        internal static List<string> Tip(string objectName)
        {
            try
            {
                foreach (TooltipUI tip in UnityEngine.Object.FindObjectsOfType<TooltipUI>())
                    if (tip != null && tip.gameObject.name == objectName) return Lines(tip.GetTooltip());
            }
            catch { }
            return new List<string>();
        }

        private static List<string> Lines(string richText)
        {
            var lines = new List<string>();
            if (string.IsNullOrWhiteSpace(richText)) return lines;
            foreach (string raw in richText.Replace("\\n", "\n").Split('\n'))
            {
                string line = FocusProbe.Clean(raw).Replace(": - 0", ": 0").TrimEnd('.');
                if (line.Length > 0) lines.Add(line);
            }
            return lines;
        }

        // ---- Changes worth saying without being asked ----

        private static int _hp = int.MinValue, _turns = int.MinValue, _energy = int.MinValue;
        private static string _threat = "";
        private static string _threatSeen = "";
        private static float _threatDue;

        internal static void AnnounceChanges(S_ControllerFocus focus)
        {
            try
            {
                Il2CppBackend.M_Player p = focus.PlayerModel;
                if (p == null) { _hp = int.MinValue; _turns = int.MinValue; _energy = int.MinValue; _threat = ""; return; }

                int hp = p.HealthData.CurrentHealth;
                if (_hp != int.MinValue && hp != _hp)
                    Say((hp < _hp ? "Lost " + (_hp - hp) : "Gained " + (hp - _hp)) + " health. Health " + hp + " of " + p.HealthData.MaxHealth + ".");
                _hp = hp;

                // Energy refills some seconds into a new turn, so it is announced when it actually
                // rises, not when the turn count changes.
                int turns = p.ExploreTurnCount + p.CombatTurnCount;
                if (_turns != int.MinValue && turns > _turns) Say("New turn.");
                _turns = turns;

                int energy = p.Energy;
                if (_energy != int.MinValue && energy > _energy) Say("Energy " + energy + " of " + p.EnergySoftCap + ".");
                _energy = energy;

                bool exploring = p.IsInExploreEncounter;
                if (exploring != _exploring)
                {
                    if (exploring && !focus.InCombat) Say("You have stopped at a camp. Cards are dealt; Space ends the turn.");
                    _exploring = exploring;
                }

                bool combat = focus.InCombat;
                if (combat != _combat)
                {
                    Say(combat ? "Combat." : "Combat over.");
                    _combat = combat;
                    _phase = "";
                }
                if (combat)
                {
                    string phase = p.CombatTurn.ToString();
                    if (phase != _phase)
                    {
                        if (phase == "PlayerTurn") Say("Your turn.");
                        else if (phase == "EnemyTurn") Say("Enemy turn.");
                        _phase = phase;
                    }
                    WatchEnemies();
                }
                else _enemyHp.Clear();

                string threat = Threat();
                if (threat != _threatSeen) { _threatSeen = threat; _threatDue = Time.realtimeSinceStartup + 1.2f; }
                if (_threatDue > 0f && Time.realtimeSinceStartup >= _threatDue)
                {
                    _threatDue = 0f;
                    if (threat != _threat && threat.Length > 0) Say(threat + ".");
                    _threat = threat;
                }
            }
            catch (Exception ex) { Log("[hud] " + ex.GetType().Name + ": " + ex.Message); }
        }

        // Enemy health as it was a moment ago, to say what each hit did.
        private static readonly Dictionary<IntPtr, int> _enemyHp = new Dictionary<IntPtr, int>();
        private static readonly Dictionary<IntPtr, string> _enemyNames = new Dictionary<IntPtr, string>();
        private static bool _combat;
        private static bool _exploring;
        private static string _phase = "";

        private static void WatchEnemies()
        {
            var seen = new HashSet<IntPtr>();
            foreach (V_Enemy view in UnityEngine.Object.FindObjectsOfType<V_Enemy>())
            {
                if (view == null) continue;
                M_Enemy m = null;
                try { m = view.Model; } catch { }
                if (m == null) continue;
                IntPtr key = view.Pointer;
                seen.Add(key);
                string name = FocusProbe.EnemyName(view, m);
                _enemyNames[key] = name;
                int hp = m.HealthData.CurrentHealth;
                if (_enemyHp.TryGetValue(key, out int before) && hp != before)
                {
                    if (hp <= 0 || !m.IsAlive) Say(name + " dies.");
                    else if (hp < before) Say(name + " takes " + (before - hp) + ", " + hp + " left.");
                    else Say(name + " heals " + (hp - before) + ", " + hp + " of " + m.HealthData.MaxHealth + ".");
                }
                _enemyHp[key] = hp;
            }
            // Gone without its health being seen to reach zero: it died, fled or was removed.
            var gone = new List<IntPtr>();
            foreach (var pair in _enemyHp) if (!seen.Contains(pair.Key)) gone.Add(pair.Key);
            foreach (IntPtr key in gone)
            {
                if (_enemyHp[key] > 0 && _enemyNames.TryGetValue(key, out string name)) Say(name + " is gone.");
                _enemyHp.Remove(key);
                _enemyNames.Remove(key);
            }
        }

        private static void Say(string text) => FocusProbe.Announce(text, interrupt: false, holdWhileWalking: true);
    }
}
