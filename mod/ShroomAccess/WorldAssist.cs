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
                foreach (ProximityFocusGate gate in UnityEngine.Object.FindObjectsOfType<ProximityFocusGate>())
                {
                    if (gate == null || gate._seen) continue;
                    EncounterStartWorldObject obj = gate._focusOnSeenBehaviour;
                    if (obj == null) obj = gate.GetComponent<EncounterStartWorldObject>();
                    if (obj == null || !obj.PlayerInRange || !obj.CanInteract) continue;
                    gate._seen = true;
                    Log("[assist] marked seen: " + obj.gameObject.name);
                }
            }
            catch (Exception ex)
            {
                Log("[assist] " + ex.GetType().Name + ": " + ex.Message);
            }
        }
    }
}
