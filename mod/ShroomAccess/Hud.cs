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
        private static float _displayLookAt;

        private static S_PlayerValuesDisplay Display()
        {
            if (_display == null && Time.realtimeSinceStartup >= _displayLookAt)
            {
                _displayLookAt = Time.realtimeSinceStartup + 1f;
                _display = UnityEngine.Object.FindObjectOfType<S_PlayerValuesDisplay>();
            }
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
        private static float _threatReadAt;
        internal static float DeathSaidAt = -100f;
        private static bool _turnSaid;
        private static float _emptySince = -1f;
        private static int _held = -1;

        internal static void AnnounceChanges(S_ControllerFocus focus)
        {
            try
            {
                Il2CppBackend.M_Player p = focus.PlayerModel;
                if (p == null) { _hp = int.MinValue; _turns = int.MinValue; _energy = int.MinValue; _threat = ""; return; }

                int hp = p.HealthData.CurrentHealth;
                int armour = 0;
                try { armour = p.HealthData.Armour; } catch { }
                if (_hp != int.MinValue && hp != _hp)
                {
                    string health = " " + Loc.T("hud.health", "Health {0} of {1}.", hp, p.HealthData.MaxHealth);
                    string cause = hp < _hp && _combat ? DamageSource(_hp - hp, Math.Max(0, _armourBefore - armour)) : "";
                    Say(cause.Length > 0 ? cause + health
                        : (hp < _hp ? Loc.T("hud.lost_health", "Lost {0} health.", _hp - hp) : Loc.T("hud.gained_health", "Gained {0} health.", hp - _hp)) + health);
                }
                _armourBefore = armour;
                // At zero the run is over: what the game does next (a new turn, enemies leaving) is not news.
                if (hp <= 0 && _hp > 0)
                {
                    DeathSaidAt = Time.realtimeSinceStartup;
                    Say(Loc.T("hud.died", "You have died."));
                }
                _hp = hp;
                if (hp <= 0) { _combat = false; _phase = ""; _enemyHp.Clear(); _enemyArmour.Clear(); return; }

                // Energy refills some seconds into a new turn, so it is announced when it actually
                // rises, not when the turn count changes.
                int turns = p.ExploreTurnCount + p.CombatTurnCount;
                if (_turns != int.MinValue && turns > _turns && !_combat) { Say(Loc.T("turn.new", "New turn.")); FocusProbe.Silent(4f); }
                _turns = turns;

                int energy = p.Energy;
                if (_energy != int.MinValue && energy > _energy)
                {
                    string line = Loc.T("hud.energy", "Energy {0} of {1}.", energy, p.EnergySoftCap);
                    // In a fight the refill comes just before the game's turn changes and the new hand is
                    // dealt: keep it for the "Your turn" line, and keep the hand quiet while it is dealt.
                    if (_combat && _phase == "EnemyTurn")
                    {
                        // The refill is the first sign of the player's turn, as the new hand starts
                        // to be dealt: say it now rather than when the game's turn flag follows.
                        Say(Loc.T("turn.yours", "Your turn.") + " " + line);
                        _turnSaid = true;
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
                        if (phase == "PlayerTurn" && !_turnSaid) Say(Loc.T("turn.yours", "Your turn."));
                        else if (phase == "EnemyTurn") { Say(Loc.T("turn.enemy", "Enemy turn.")); NoteAttacks(); }
                        _turnSaid = false;
                        _phase = phase;
                    }
                    Perf.Lap("Hud.core");
                    WatchEnemies();
                    WarnIfLethal(phase, turns, hp, armour);
                    Perf.Lap("Hud.enemies");
                    int held = -1;
                    try { held = p.GetActiveHand() != null && p.GetActiveHand().HeldCards != null ? p.GetActiveHand().HeldCards.Count : -1; } catch { }
                    // Said once the hand has stayed empty a moment: a card played can make another
                    // (Roast makes a Toasty) a fraction of a second later.
                    if (held == 0 && _held > 0) _emptySince = Time.realtimeSinceStartup;
                    if (held != 0) _emptySince = -1f;
                    if (_emptySince > 0f && Time.realtimeSinceStartup - _emptySince > 0.6f && phase == "PlayerTurn")
                    {
                        _emptySince = -1f;
                        Say(Loc.T("hud.hand_empty", "Your hand is empty. Space ends the turn."));
                    }
                    _held = held;
                }
                else { _enemyHp.Clear(); _enemyArmour.Clear(); }

                Perf.Lap("Hud.hand");
                // Read a few times a second: the game builds the whole hover text each time.
                string threat = _threatSeen;
                if (Time.realtimeSinceStartup >= _threatReadAt) { _threatReadAt = Time.realtimeSinceStartup + 0.3f; threat = Threat(); }
                Perf.Lap("Hud.threat");
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

        // In the player's turn, the enemies' shown attacks (a Megashroom's explosion among them) add
        // up to the player's health or more: said once per turn and amount, a few times a second at
        // most. A sighted player sees the big number over the enemy; this is that glance.
        private static float _lethalCheckAt;
        private static string _lethalSaid = "";

        private static void WarnIfLethal(string phase, int turn, int hp, int armour)
        {
            if (phase != "PlayerTurn" || Time.realtimeSinceStartup < _lethalCheckAt) return;
            _lethalCheckAt = Time.realtimeSinceStartup + 0.5f;
            int incoming = FocusProbe.IncomingTotal(out _);
            if (incoming <= 0 || incoming < hp + armour) return;
            string key = turn + ":" + incoming;
            if (key == _lethalSaid) return;
            _lethalSaid = key;
            Say(armour > 0
                ? Loc.T("hud.lethal_armour", "Warning: the enemies will deal {0} damage next turn, and you have {1} health and {2} armour.", incoming, hp, armour)
                : Loc.T("hud.lethal", "Warning: the enemies will deal {0} damage next turn, and you have {1} health.", incoming, hp));
        }

        private static float _shopUntil = -100f;
        private static int _armourBefore;

        // The attacks the enemies showed at the start of their turn, in the order they stand.
        private static readonly List<KeyValuePair<string, int>> _attacks = new List<KeyValuePair<string, int>>();

        private static void NoteAttacks()
        {
            _attacks.Clear();
            try
            {
                var views = new List<V_Enemy>();
                foreach (V_Enemy v in EnemyViews()) if (v != null) views.Add(v);
                views.Sort((a, b) => a.transform.position.x.CompareTo(b.transform.position.x));
                foreach (V_Enemy v in views)
                {
                    M_Enemy m = v.Model;
                    if (m == null || !m.IsAlive || m.CurrentTurnAction == null) continue;
                    var pairs = m.CurrentTurnAction.EnemyIntentPairs;
                    for (int i = 0; pairs != null && i < pairs.Count; i++)
                        if (pairs[i] != null && pairs[i].Intent != null && FocusProbe.IsDamageIntent(pairs[i].Intent.name))
                            _attacks.Add(new KeyValuePair<string, int>(FocusProbe.EnemyName(v, m), pairs[i].ModifiedNumber));
                }
            }
            catch (Exception ex) { Log("[hud] attacks: " + ex.Message); }
        }

        // Whose doing a loss of health was, when it can be told: an announced attack of that size in
        // the enemy turn, or thorns on an enemy in the player's turn. Empty when it cannot.
        private static string DamageSource(int lost, int armourLost)
        {
            try
            {
                if (_phase == "EnemyTurn")
                {
                    int hit = lost + armourLost;
                    int i = _attacks.FindIndex(a => a.Value == hit);
                    if (i < 0) i = _attacks.FindIndex(a => a.Value == lost);
                    if (i < 0) return "";
                    string who = _attacks[i].Key;
                    _attacks.RemoveAt(i);
                    return armourLost > 0 ? Loc.T("hud.hit_blocked", "{0} hits you for {1}, {2} blocked by your armour.", who, lost, armourLost) : Loc.T("hud.hit", "{0} hits you for {1}.", who, lost);
                }
                if (_phase == "PlayerTurn")
                {
                    foreach (V_Enemy v in EnemyViews())
                    {
                        M_Enemy m = v != null ? v.Model : null;
                        if (m == null) continue;
                        foreach (string status in FocusProbe.EnemyStatuses(m))
                            if (status == "Thorns " + lost) return Loc.T("hud.thorns", "Thorns on {0}: lost {1} health.", FocusProbe.EnemyName(v, m), lost);
                    }
                }
            }
            catch (Exception ex) { Log("[hud] damage source: " + ex.Message); }
            return "";
        }
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

        // The enemies on screen. Searching the scene for them takes this game tens of milliseconds,
        // long enough to make a fight's frames late, so the list is taken from the game's encounter
        // manager, found once. An enemy stays on the list until its object is destroyed or hidden,
        // as a scene search would have it, so its death is still seen.
        private static readonly List<V_Enemy> _views = new List<V_Enemy>();
        private static float _viewsAt = -100f, _managerLookAt;
        private static S_EnemyEncounterManager _manager;

        internal static List<V_Enemy> EnemyViews()
        {
            float now = Time.realtimeSinceStartup;
            if (_manager == null && now >= _managerLookAt)
            {
                _managerLookAt = now + 2f;
                _manager = UnityEngine.Object.FindObjectOfType<S_EnemyEncounterManager>();
            }
            Il2CppSystem.Collections.Generic.List<V_Enemy> current = null;
            try { current = _manager != null ? _manager._currentEnemies : null; } catch { }
            if (current == null)
            {
                if (now - _viewsAt > 1f)
                {
                    _viewsAt = now;
                    _views.Clear();
                    foreach (V_Enemy v in UnityEngine.Object.FindObjectsOfType<V_Enemy>()) if (v != null) _views.Add(v);
                }
                else _views.RemoveAll(v => v == null);
                return _views;
            }
            _views.RemoveAll(v => v == null || !v.gameObject.activeInHierarchy);
            for (int i = 0; i < current.Count; i++)
            {
                V_Enemy v = current[i];
                if (v == null || !v.gameObject.activeInHierarchy) continue;
                IntPtr ptr = v.Pointer;
                if (!_views.Exists(x => x.Pointer == ptr)) _views.Add(v);
            }
            return _views;
        }

        private static void WatchEnemies()
        {
            var seen = new HashSet<IntPtr>();
            foreach (V_Enemy view in EnemyViews())
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
                string blocked = known && armour < armourBefore && hp > 0 && _phase == "PlayerTurn" ? Loc.T("enemy.blocked", "{0} blocked by armour", armourBefore - armour) : "";
                if (known && hp != before)
                {
                    if (hp <= 0 || !m.IsAlive) Say(Loc.T("enemy.dies", "{0} dies.", name));
                    else if (hp < before) Say(blocked.Length > 0 ? Loc.T("enemy.takes_blocked", "{0} takes {1}, {2}, {3} left.", name, before - hp, blocked, hp) : Loc.T("enemy.takes", "{0} takes {1}, {2} left.", name, before - hp, hp));
                    else Say(Loc.T("enemy.heals", "{0} heals {1}, {2} of {3}.", name, hp - before, hp, m.HealthData.MaxHealth));
                }
                else if (blocked.Length > 0) Say(armour > 0 ? Loc.T("enemy.blocked_left", "{0}: {1}, {2} armour left.", name, blocked, armour) : Loc.T("enemy.blocked_none", "{0}: {1}, no armour left.", name, blocked));
                else if (known && armour > armourBefore && _phase == "EnemyTurn") Say(Loc.T("enemy.gains_armour", "{0} gains {1} armour.", name, armour - armourBefore));
                _enemyHp[key] = hp;
                _enemyArmour[key] = armour;
            }
            // Gone without its health being seen to reach zero: it died, fled or was removed.
            var gone = new List<IntPtr>();
            foreach (var pair in _enemyHp) if (!seen.Contains(pair.Key)) gone.Add(pair.Key);
            foreach (IntPtr key in gone)
            {
                if (_enemyHp[key] > 0 && _enemyNames.TryGetValue(key, out string name)) Say(Loc.T("enemy.gone", "{0} is gone.", name));
                _enemyHp.Remove(key);
                _enemyNames.Remove(key);
                _enemyArmour.Remove(key);
            }
        }

        private static void Say(string text) => FocusProbe.Announce(text, interrupt: false, holdWhileWalking: true, repeatable: true);
    }
}
