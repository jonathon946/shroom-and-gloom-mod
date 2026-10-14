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
        private static string _threatOutcome = "";
        private static string _threatSeen = "";
        private static float _threatDue;
        internal static float DeathSaidAt = -100f;
        private static string _energyHeld = "";
        private static int _held = -1;

        internal static void AnnounceChanges(S_ControllerFocus focus)
        {
            try
            {
                Il2CppBackend.M_Player p = focus.PlayerModel;
                if (p == null) { _hp = int.MinValue; _turns = int.MinValue; _energy = int.MinValue; _threat = ""; return; }

                int hp = p.HealthData.CurrentHealth;
                if (_hp != int.MinValue && hp != _hp)
                    Say((hp < _hp ? "Lost " + (_hp - hp) : "Gained " + (hp - _hp)) + " health. Health " + hp + " of " + p.HealthData.MaxHealth + ".");
                // At zero the run is over: what the game does next (a new turn, enemies leaving) is not news.
                if (hp <= 0 && _hp > 0)
                {
                    DeathSaidAt = Time.realtimeSinceStartup;
                    Say("You have died.");
                }
                _hp = hp;
                if (hp <= 0) { _combat = false; _phase = ""; _enemyHp.Clear(); _enemyArmour.Clear(); return; }

                // Energy refills some seconds into a new turn, so it is announced when it actually
                // rises, not when the turn count changes.
                int turns = p.ExploreTurnCount + p.CombatTurnCount;
                if (_turns != int.MinValue && turns > _turns && !_combat) { Say("New turn."); FocusProbe.Silent(4f); }
                _turns = turns;

                int energy = p.Energy;
                if (_energy != int.MinValue && energy > _energy)
                {
                    string line = "Energy " + energy + " of " + p.EnergySoftCap + ".";
                    // In a fight the refill comes just before the game's turn changes and the new hand is
                    // dealt: keep it for the "Your turn" line, and keep the hand quiet while it is dealt.
                    if (_combat && _phase == "EnemyTurn")
                    {
                        _energyHeld = line;
                        FocusProbe.Silent(3f);
                    }
                    else
                    {
                        Say(line);
                        // Outside a fight the refill starts a new turn's deal.
                        if (!_combat) FocusProbe.Silent(3f);
                    }
                }
                _energy = energy;

                bool exploring = p.IsInExploreEncounter;
                if (exploring != _exploring)
                {
                    if (exploring && !focus.InCombat) { FocusProbe.CampReached(); _threatOutcome = ""; }
                    _exploring = exploring;
                }

                // A shop sets both the focus system's and the player's combat flags while food is paid.
                bool combat = InFight(focus);
                if (combat != _combat)
                {
                    if (combat) FocusProbe.CombatStarted(); else FocusProbe.CombatEnded();
                    _combat = combat;
                    _phase = "";
                }
                if (combat)
                {
                    string phase = p.CombatTurn.ToString();
                    if (phase != _phase)
                    {
                        if (phase == "PlayerTurn") Say("Your turn." + (_energyHeld.Length > 0 ? " " + _energyHeld : ""));
                        else if (phase == "EnemyTurn") Say("Enemy turn.");
                        if (phase == "PlayerTurn") _energyHeld = "";
                        _phase = phase;
                    }
                    WatchEnemies();
                    int held = -1;
                    try { held = p.GetActiveHand() != null && p.GetActiveHand().HeldCards != null ? p.GetActiveHand().HeldCards.Count : -1; } catch { }
                    if (held == 0 && _held > 0 && phase == "PlayerTurn") Say("Your hand is empty. Space ends the turn.");
                    _held = held;
                }
                else { _enemyHp.Clear(); _enemyArmour.Clear(); }

                string threat = Threat();
                if (threat != _threatSeen) { _threatSeen = threat; _threatDue = Time.realtimeSinceStartup + 1.2f; }
                if (_threatDue > 0f && Time.realtimeSinceStartup >= _threatDue)
                {
                    _threatDue = 0f;
                    // Said when what will happen changes (safe, balanced, or taking damage), and once on
                    // reaching a camp. A different number with the same outcome stays on the Status row.
                    string outcome = threat.Contains("Take ") ? "damage" : threat.Contains("Safe") ? "safe" : threat.Length > 0 ? "other" : "";
                    if (threat != _threat && threat.Length > 0 && outcome != _threatOutcome) Say(threat + ".");
                    if (threat.Length > 0) _threatOutcome = outcome;
                    _threat = threat;
                }
            }
            catch (Exception ex) { Log("[hud] " + ex.GetType().Name + ": " + ex.Message); }
        }

        private static float _shopUntil = -100f;
        private static bool _shopUnderMenu;

        // The enemies are acting; the player's hand is being cleared and the next one dealt.
        internal static bool EnemyTurnNow => _combat && _phase == "EnemyTurn";

        // A real fight: the player model says combat and no shop is open. Paying at a shop uses the
        // fight's hand and sets the same flags, and they stay set for a moment after the shop closes.
        internal static bool InFight(S_ControllerFocus focus)
        {
            try
            {
                string scope = focus.Scope != null ? FocusProbe.TypeName(focus.Scope) : "";
                // The pause menu over a shop is still the shop.
                if (!scope.EndsWith("MenuFocusScope")) _shopUnderMenu = scope.EndsWith("BowlShopEncounterUI");
                if (scope.EndsWith("BowlShopEncounterUI") || (scope.EndsWith("MenuFocusScope") && _shopUnderMenu)) _shopUntil = Time.realtimeSinceStartup + 2f;
                if (Time.realtimeSinceStartup < _shopUntil) return false;
                Il2CppBackend.M_Player p = focus.PlayerModel;
                return p != null ? p.IsInCombat : focus.InCombat;
            }
            catch { return focus.InCombat; }
        }

        // Enemy health as it was a moment ago, to say what each hit did.
        private static readonly Dictionary<IntPtr, int> _enemyHp = new Dictionary<IntPtr, int>();
        private static readonly Dictionary<IntPtr, string> _enemyNames = new Dictionary<IntPtr, string>();
        private static readonly Dictionary<IntPtr, int> _enemyArmour = new Dictionary<IntPtr, int>();
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
                int armour = 0;
                try { armour = m.HealthData.Armour; } catch { }
                bool known = _enemyHp.TryGetValue(key, out int before);
                _enemyArmour.TryGetValue(key, out int armourBefore);
                // A hit that armour soaks up changes no health; say what the armour took.
                string blocked = known && armour < armourBefore && hp > 0 && _phase == "PlayerTurn" ? (armourBefore - armour) + " blocked by armour" : "";
                if (known && hp != before)
                {
                    if (hp <= 0 || !m.IsAlive) Say(name + " dies.");
                    else if (hp < before) Say(name + " takes " + (before - hp) + (blocked.Length > 0 ? ", " + blocked : "") + ", " + hp + " left.");
                    else Say(name + " heals " + (hp - before) + ", " + hp + " of " + m.HealthData.MaxHealth + ".");
                }
                else if (blocked.Length > 0) Say(name + ": " + blocked + (armour > 0 ? ", " + armour + " armour left." : ", no armour left."));
                else if (known && armour > armourBefore && _phase == "EnemyTurn") Say(name + " gains " + (armour - armourBefore) + " armour.");
                _enemyHp[key] = hp;
                _enemyArmour[key] = armour;
            }
            // Gone without its health being seen to reach zero: it died, fled or was removed.
            var gone = new List<IntPtr>();
            foreach (var pair in _enemyHp) if (!seen.Contains(pair.Key)) gone.Add(pair.Key);
            foreach (IntPtr key in gone)
            {
                if (_enemyHp[key] > 0 && _enemyNames.TryGetValue(key, out string name)) Say(name + " is gone.");
                _enemyHp.Remove(key);
                _enemyNames.Remove(key);
                _enemyArmour.Remove(key);
            }
        }

        private static void Say(string text) => FocusProbe.Announce(text, interrupt: false, holdWhileWalking: true);
    }
}
