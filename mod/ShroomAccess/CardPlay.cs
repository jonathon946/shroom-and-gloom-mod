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
        private static int _playedBefore;

        private static string _placedAt = "";

        internal static void Tick(S_ControllerFocus focus)
        {
            try
            {
                Il2CppBackend.M_Player p = focus.PlayerModel;
                if (p == null) return;

                if (_castAt > 0f && Time.realtimeSinceStartup >= _castAt)
                {
                    _castAt = 0f;
                    // A play is heard in the game's own sound; only where a camp card went is said.
                    if (focus.Mode != FocusMode.Target && _placedAt.Length > 0)
                        FocusProbe.Announce(_name + " placed, " + _placedAt + ".", interrupt: false);
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
                    // The check started by the first Enter (on the card) no longer applies.
                    _checkAt = 0f;
                    // A camp card set down on an empty plot: say where it went.
                    _placedAt = "";
                    try
                    {
                        string target = focus.CurrentFocus != null ? FocusProbe.Describe(focus.CurrentFocus, out _) : "";
                        if (target.StartsWith("Empty plot, ")) _placedAt = target.Substring(12).Split(new[] { ", marked" }, StringSplitOptions.None)[0].TrimEnd('.', ' ');
                    }
                    catch { }
                    FocusProbe.NotePlay(_name);
                    FocusProbe.Silent(2.5f);
                    return;
                }
                if (focus.Mode.ToString() != "Hand" || focus.Scope != null) return;
                Il2CppSystem.Object current = focus.CurrentFocus;
                IV_CardInstance view = current != null ? current.TryCast<IV_CardInstance>() : null;
                IM_CardInstance model = view != null ? view.IModel : null;
                if (model == null) return;

                GameplayCardData data = model.IData != null ? model.IData.TryCast<GameplayCardData>() : null;
                bool unplayable = false;
                try { unplayable = data != null && data.IsUnplayable; } catch { }
                if (unplayable)
                {
                    FocusProbe.Speak(model.CardName + " is never played: it works by itself while it is in your hand.");
                    return;
                }
                if (data != null && data.EnergyUse > p.Energy)
                {
                    FocusProbe.Speak("Not enough energy for " + model.CardName + ": it costs " + data.EnergyUse + ", you have " + p.Energy + ". Space ends the turn.");
                    return;
                }
                // A card that costs health ("Take 5 damage") when that is all the health left: a
                // sighted player sees the health bar beside the card.
                try
                {
                    var hurt = System.Text.RegularExpressions.Regex.Match(FocusProbe.Clean(data != null ? data.SingleDescription : ""), @"Take (\d+) damage", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    int cost = hurt.Success ? int.Parse(hurt.Groups[1].Value) : 0;
                    int hp = p.HealthData.CurrentHealth;
                    if (cost > 0 && cost >= hp)
                        FocusProbe.Announce("Careful: " + model.CardName + " costs " + cost + " health and you have " + hp + ".", interrupt: false);
                }
                catch { }
                _card = model.Pointer;
                _name = model.CardName;
                _failureBefore = Failure(focus);
                _playedBefore = Played(p);
                // A second Enter on a lifted card plays it; the card the focus falls back to is not read.
                bool lifted = false;
                try { lifted = focus.HandResolver != null && focus.HandResolver.PendingCast; } catch { }
                if (lifted || focus.PadOwnsCast) FocusProbe.Silent(2.5f);
                _checkAt = Time.realtimeSinceStartup + 0.7f;
            }
            catch (Exception ex) { Log("[cardplay] " + ex.GetType().Name + ": " + ex.Message); }
        }

        private static string Failure(S_ControllerFocus focus)
        {
            try { return focus.SpellCaster != null ? focus.SpellCaster.LastCastFailureReason : null; } catch { return null; }
        }

        // The game's own count of cards played: a card that stays in hand after playing (Persistent)
        // is played all the same.
        private static int Played(Il2CppBackend.M_Player p)
        {
            try { return p.CardsPlayedThisEncounter * 1000 + p.CardsPlayedThisTurn; } catch { return -1; }
        }

        private static void Resolve(S_ControllerFocus focus, Il2CppBackend.M_Player p)
        {
            // Choosing a target: nothing to report yet.
            if (focus.Mode == FocusMode.Target) return;

            int played = Played(p);
            if (played >= 0 && _playedBefore >= 0 && played != _playedBefore)
            {
                FocusProbe.NotePlay(_name);
                Log("[cardplay] played " + _name);
                return;
            }

            bool stillHeld = false;
            IM_Hand hand = p.GetActiveHand();
            var held = hand != null ? hand.HeldCards : null;
            for (int i = 0; held != null && i < held.Count; i++)
                if (held[i] != null && held[i].Pointer == _card) { stillHeld = true; break; }

            if (!stillHeld) { FocusProbe.NotePlay(_name); Log("[cardplay] played " + _name); return; }

            // A card with no target is lifted by the first Enter and played by the second.
            bool pending = false;
            try { pending = focus.HandResolver != null && focus.HandResolver.PendingCast; } catch { }
            if (pending || focus.PadOwnsCast)
            {
                FocusProbe.Speak(_name + " is ready. Enter again to play it, Backspace to put it back.");
                return;
            }

            string failure = Failure(focus);
            // What the game's own check says, for the log and for a reason the game does not put in words.
            bool notTurn = false, canCast = true;
            string target = "";
            try
            {
                var obj = new Il2CppSystem.Object(_card);
                M_GameplayCardInstance card = obj.TryCast<M_GameplayCardInstance>();
                if (card != null && focus.SpellCaster != null) canCast = focus.SpellCaster.CanCast(card, out notTurn);
                IM_CardInstance im = obj.TryCast<IM_CardInstance>();
                GameplayCardData d = im != null && im.IData != null ? im.IData.TryCast<GameplayCardData>() : null;
                if (d != null) target = d.Target.ToString();
            }
            catch (Exception ex) { target = ex.GetType().Name; }
            Log("[cardplay] " + _name + " not played; failure reason: " + failure + "; canCast=" + canCast + " notTurn=" + notTurn + " target=" + target);
            // With no reason given and the game's own check passing, the card had nothing to act on
            // (seen with Shovel and Tool when nothing nearby could be dug or fixed).
            string why = !string.IsNullOrWhiteSpace(failure) && failure != _failureBefore ? " " + FocusProbe.Clean(failure) + "."
                : notTurn ? " It is not your turn yet."
                : " Nothing here it can be used on.";
            FocusProbe.Speak(_name + " was not played." + why);
        }
    }
}
