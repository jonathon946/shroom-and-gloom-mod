using System;
using Il2Cpp;
using Il2CppControllerNav;
using UnityEngine;

namespace ShroomAccess
{
    // The game lets a gamepad focus some world objects only after the player has looked straight at
    // them with the camera stick for a moment. A blind player cannot aim a camera, so anything of
    // that kind that is within the game's own interaction range is marked as seen for them.
    internal static class WorldAssist
    {
        internal static Action<string> Log = _ => { };
        private static float _next;
        // The game's own list of focus targets and their gates. A search of the whole scene for the
        // gates cost more with every tunnel streamed in, until camp menus lagged late in a run.
        private static bool _registryFailed;
        private static string _logged = "";

        internal static void Tick()
        {
            if (Time.realtimeSinceStartup < _next) return;
            _next = Time.realtimeSinceStartup + 0.25f;
            try
            {
                Vector3? player = null;
                if (S_ControllerFocus.TryGetFocus(out S_ControllerFocus focus) && focus != null && focus.Player != null)
                    player = focus.Player.transform.position;
                // Nothing is approached in a fight.
                if (focus != null && focus.InCombat) return;

                if (!_registryFailed)
                {
                    try { FromRegistry(player); return; }
                    catch (Exception ex)
                    {
                        _registryFailed = true;
                        Log("[assist] focus registry unreadable, searching the scene every 2 s instead: " + ex.GetType().Name + ": " + ex.Message);
                    }
                }
                _next = Time.realtimeSinceStartup + 2f;
                foreach (ProximityFocusGate gate in UnityEngine.Object.FindObjectsOfType<ProximityFocusGate>()) Consider(gate, player);
            }
            catch (Exception ex)
            {
                Log("[assist] " + ex.GetType().Name + ": " + ex.Message);
            }
        }

        private static void FromRegistry(Vector3? player)
        {
            var targets = ControllerFocusRegistry._targets;
            int count = targets != null ? targets.Count : 0;
            int gated = 0, unseen = 0;
            for (int i = 0; i < count; i++)
            {
                IControllerFocusTarget target = targets[i];
                if (target == null) continue;
                var gates = ControllerFocusRegistry.GatesFor(target);
                for (int g = 0; gates != null && g < gates.Length; g++)
                {
                    ControllerFocusGate gate = gates[g];
                    ProximityFocusGate proximity = gate != null ? gate.TryCast<ProximityFocusGate>() : null;
                    if (proximity == null) continue;
                    gated++;
                    if (!proximity._seen) unseen++;
                    Consider(proximity, player);
                }
            }
            string summary = count + " targets, " + gated + " gated, " + unseen + " unseen";
            if (summary != _logged) { _logged = summary; Log("[assist] focus registry: " + summary); }
        }

        private static void Consider(ProximityFocusGate gate, Vector3? player)
        {
            if (gate == null || gate._seen) return;
            EncounterStartWorldObject obj = gate._focusOnSeenBehaviour;
            if (obj == null) obj = gate.GetComponent<EncounterStartWorldObject>();
            if (obj != null)
            {
                if (!obj.PlayerInRange || !obj.CanInteract) return;
            }
            else
            {
                // Other gated things (a chest that rises from the ground) have no range of
                // their own to ask; use distance from the player.
                if (player == null || (gate.transform.position - player.Value).sqrMagnitude > 10f * 10f) return;
            }
            gate._seen = true;
            Log("[assist] marked seen: " + gate.gameObject.name);
        }
    }
}
