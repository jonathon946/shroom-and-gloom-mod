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

        internal static void Tick()
        {
            if (Time.realtimeSinceStartup < _next) return;
            _next = Time.realtimeSinceStartup + 0.25f;
            try
            {
                Vector3? player = null;
                if (S_ControllerFocus.TryGetFocus(out S_ControllerFocus focus) && focus != null && focus.Player != null)
                    player = focus.Player.transform.position;
                // Nothing is approached in a fight, and the search below is slow enough to be felt.
                if (focus != null && focus.InCombat) return;

                foreach (ProximityFocusGate gate in UnityEngine.Object.FindObjectsOfType<ProximityFocusGate>())
                {
                    if (gate == null || gate._seen) continue;
                    EncounterStartWorldObject obj = gate._focusOnSeenBehaviour;
                    if (obj == null) obj = gate.GetComponent<EncounterStartWorldObject>();
                    if (obj != null)
                    {
                        if (!obj.PlayerInRange || !obj.CanInteract) continue;
                    }
                    else
                    {
                        // Other gated things (a chest that rises from the ground) have no range of
                        // their own to ask; use distance from the player.
                        if (player == null || (gate.transform.position - player.Value).sqrMagnitude > 10f * 10f) continue;
                    }
                    gate._seen = true;
                    Log("[assist] marked seen: " + gate.gameObject.name);
                }
            }
            catch (Exception ex)
            {
                Log("[assist] " + ex.GetType().Name + ": " + ex.Message);
            }
        }
    }
}
