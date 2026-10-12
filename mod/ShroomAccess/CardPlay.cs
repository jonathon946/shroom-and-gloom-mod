using System;
using Il2Cpp;
using Il2CppControllerNav;
using UnityEngine;

namespace ShroomAccess
{
    // Says what pressing Enter on a card in hand did: played, or why not. The game shows a failed
    // play with a shake and a sound only.
    internal static class CardPlay
    {
        internal static Action<string> Log = _ => { };

        private static IntPtr _card;
        private static string _name;
        private static float _checkAt;
        private static float _castAt;
        private static string _failureBefore;

        internal static void Tick(S_ControllerFocus focus)
        {
            try
            {
                Il2CppBackend.M_Player p = focus.PlayerModel;
                if (p == null) return;

                if (_castAt > 0f && Time.realtimeSinceStartup >= _castAt)
                {
                    _castAt = 0f;
                    if (focus.Mode != FocusMode.Target && !string.IsNullOrWhiteSpace(_name))
                        FocusProbe.Announce("Played " + _name + ".", interrupt: false);
                }
                if (_checkAt > 0f && Time.realtimeSinceStartup >= _checkAt)
                {
                    _checkAt = 0f;
                    Resolve(focus, p);
                }

                if (!InputBridge.Key(InputActionId.Confirm) || InputBridge.CtrlHeld) return;
                // Enter on a target: the card being cast is the one to report on.
                if (focus.Mode == FocusMode.Target && focus.Scope == null && focus.TargetResolver != null)
                {
                    _name = focus.TargetResolver.CastingCardName;
                    _castAt = Time.realtimeSinceStartup + 0.9f;
                    return;
                }
                if (focus.Mode.ToString() != "Hand" || focus.Scope != null) return;
                Il2CppSystem.Object current = focus.CurrentFocus;
                IV_CardInstance view = current != null ? current.TryCast<IV_CardInstance>() : null;
                IM_CardInstance model = view != null ? view.IModel : null;
                if (model == null) return;

                GameplayCardData data = model.IData != null ? model.IData.TryCast<GameplayCardData>() : null;
                if (data != null && data.EnergyUse > p.Energy)
                {
                    FocusProbe.Speak("Not enough energy for " + model.CardName + ": it costs " + data.EnergyUse + ", you have " + p.Energy + ". Space ends the turn.");
                    return;
                }
                _card = model.Pointer;
                _name = model.CardName;
                _failureBefore = Failure(focus);
                _checkAt = Time.realtimeSinceStartup + 0.7f;
            }
            catch (Exception ex) { Log("[cardplay] " + ex.GetType().Name + ": " + ex.Message); }
        }

        private static string Failure(S_ControllerFocus focus)
        {
            try { return focus.SpellCaster != null ? focus.SpellCaster.LastCastFailureReason : null; } catch { return null; }
        }

        private static void Resolve(S_ControllerFocus focus, Il2CppBackend.M_Player p)
        {
            // Choosing a target: nothing to report yet.
            if (focus.Mode == FocusMode.Target) return;

            bool stillHeld = false;
            IM_Hand hand = p.GetActiveHand();
            var held = hand != null ? hand.HeldCards : null;
            for (int i = 0; held != null && i < held.Count; i++)
                if (held[i] != null && held[i].Pointer == _card) { stillHeld = true; break; }

            if (!stillHeld) { FocusProbe.Announce("Played " + _name + ".", interrupt: false); return; }

            // A card with no target is lifted by the first Enter and played by the second.
            bool pending = false;
            try { pending = focus.HandResolver != null && focus.HandResolver.PendingCast; } catch { }
            if (pending || focus.PadOwnsCast)
            {
                FocusProbe.Speak(_name + " is ready. Enter again to play it, Backspace to put it back.");
                return;
            }

            string failure = Failure(focus);
            Log("[cardplay] " + _name + " not played; failure reason: " + failure);
            string why = !string.IsNullOrWhiteSpace(failure) && failure != _failureBefore ? " " + FocusProbe.Clean(failure) + "." : " Nothing here it can be used on.";
            FocusProbe.Speak(_name + " can't be played right now." + why);
        }
    }
}
