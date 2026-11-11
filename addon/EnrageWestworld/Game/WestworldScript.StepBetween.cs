// Step between them, the game side (DEPTH item 7; Core/StepBetween.cs; [Scenes] StepBetween). A look every 500 ms under the frame
// pass's scheduler (WestworldScript.Looks.cs): the Director's scene is a quarrel between two men with a hostile tie, no blow yet and
// nothing of yours in it, you on foot within 10 m of either and J free: J offers to step between them, once a scene. Chosen, your
// word is said, and each heeds you by his nature and by what he thinks of you (Brawl.Heeds with BrawlVerbs.Standing, the break-it-up
// shout's own judgement): both heed and the rest of the scene is rewritten (Director.Rewrite) to their backing off, the tie put off
// (SceneTie.PutOffAt: the reasons pass the pair over six game hours); the second time both heed, they make it up in front of you (a
// "makeup" tie). One won't: he says so and the scene goes on as the Director had it (his fight with you, if any, the game's).
// Runners: the J menu and the Director's street scenes (both seen).
using System;
using System.Linq;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private string _sbOfferedFor, _sbLast;
        private Ped _sbA, _sbB;
        private Action _aStepBetween;
        private int _sbOffered, _sbPutOff, _sbMadeUp, _sbShoved, _sbNextLook;

        /// Every 500 ms (the scheduler's look with LookScheduler on; Tick04's own call with it off), in its own guard.
        private void StepBetweenStep(int now)
        {
            if (!StepBetween.On || now < _sbNextLook)
                return;
            _sbNextLook = now + 500;
            Safe("step between", _aStepBetween ?? (_aStepBetween = () => StepBetweenLook(_now)));   // a kept delegate: no closure a frame (review 1)
        }

        /// The look: a quarrel about to start near you between two with bad blood.
        private void StepBetweenLook(int now)
        {
            if (!StepBetween.On || _cfg == null || !_cfg.MindLive || _store == null || !_director.Busy || _director.CurrentIntruded)
                return;
            if (_eventMenu != null || (_eventPrompt != null && Game.GameTime <= _eventPromptUntil))
                return;   // something else is on J just now
            string kind = _director.CurrentKind, seed = _director.CurrentSeed;
            if (!StepBetween.Quarrel(kind) || seed == null || seed == _sbOfferedFor || _director.FightOnFor(now) > 0)
                return;
            Ped a = _director.CastAt(0), b = _director.CastAt(1), me = Game.Player.Character;
            if (a == null || b == null || !a.Exists() || !b.Exists() || a.IsDead || b.IsDead || !PED.IS_PED_MALE(a.Handle) || !PED.IS_PED_MALE(b.Handle))
                return;
            if (PED.IS_PED_ON_MOUNT(me.Handle) || PED.IS_PED_IN_ANY_VEHICLE(me.Handle, false) || PED.IS_PED_IN_MELEE_COMBAT(me.Handle) ||
                Math.Min(me.Position.DistanceTo(a.Position), me.Position.DistanceTo(b.Position)) > StepBetween.NearMetres)
                return;
            SceneTie t = StepBetweenTie(a, b, now);
            if (t == null || t.Warmth >= 0)
                return;   // bad blood between them, or it's not a feud to step into
            _sbOfferedFor = seed;
            _sbA = a;
            _sbB = b;
            _sbOffered++;
            PromptSet(StepBetween.Kind, new TownEvent { Kind = StepBetween.Kind, Town = _placeTown, AName = _director.NameAt(0), Extra = _director.NameAt(1) }, StepBetween.PromptMs);
        }

        private SceneTie StepBetweenTie(Ped a, Ped b, int now)
        {
            MindWho wa = MindKnown(a.Handle, now), wb = MindKnown(b.Handle, now);
            if (wa == null || wb == null || wa.Id == null || wb.Id == null)
                return null;
            return _store.Ties.FirstOrDefault(x => x != null && x.Has(wa.Id) && x.Has(wb.Id));
        }

        private void StepBetweenChoose(string id)
        {
            if (id != "stepbetween:step")
                return;
            Safe("step between", () => StepBetweenDo(Game.GameTime));
        }

        private void StepBetweenDo(int now)
        {
            Ped a = _sbA, b = _sbB, me = Game.Player.Character;
            _sbA = _sbB = null;
            if (a == null || b == null || !a.Exists() || !b.Exists() || !_director.Busy || _director.CurrentSeed != _sbOfferedFor || _director.FightOnFor(now) > 0)
            {
                Log.Write("STEP BETWEEN: too late, it's over or come to blows");
                return;
            }
            string seed = "sb|" + _sbOfferedFor;
            SpeechCaption("You", StepBetween.YourWord(seed), me.Handle, 2500, false, false);
            SceneTie t = StepBetweenTie(a, b, now);
            bool heedA, heedB;
            int ca, cb;
            string ta, tb, ra, rb;
            heedA = StepHeeds(a, seed + "|a", now, out ca, out ta, out ra);
            heedB = StepHeeds(b, seed + "|b", now, out cb, out tb, out rb);
            string na = Person.FirstOf(_director.NameAt(0)), nb = Person.FirstOf(_director.NameAt(1));
            string how = na + " " + (heedA ? "heeds" : "won't") + " (" + ca + "%" + (ra != null ? ", " + ra : "") + "), " + nb + " " + (heedB ? "heeds" : "won't") + " (" + cb + "%" + (rb != null ? ", " + rb : "") + ")";
            long g = Bridge.Now();
            if (heedA && heedB)
            {
                bool second = t != null && t.PutOffs >= 1;
                Scene s = second ? StepBetween.MadeUpScene(seed) : StepBetween.PutOffScene(ta, tb, seed);
                if (!_director.Rewrite(s, second ? "you stepped between them again, and they made it up" : "you stepped between them, and both let it be"))
                {
                    Log.Write("STEP BETWEEN: the scene couldn't take it (" + how + ")");
                    return;
                }
                if (t != null)
                {
                    if (second)
                    {
                        MindWho wa = MindKnown(a.Handle, now), wb = MindKnown(b.Handle, now);
                        if (wa != null && wb != null)
                            Ties.Note(_store.Ties, wa.Id, wa.Name, wb.Id, wb.Name, _placeTown, "makeup", g);
                        t.PutOffs = 0;
                        t.PutOffAt = 0;
                        _sbMadeUp++;
                    }
                    else
                    {
                        t.PutOffAt = g;
                        t.PutOffs++;
                        _sbPutOff++;
                    }
                }
                _sbLast = (second ? "made it up: " : "put off: ") + how;
                Log.Write("STEP BETWEEN " + na + " and " + nb + ": " + _sbLast);
                return;
            }
            // one won't: he says so, and the scene goes on as it was
            Ped past = !heedA ? a : b;
            SpeechCaption(!heedA ? na : nb, StepBetween.ShovesPast(seed), past.Handle, 2500, false, false);
            _sbShoved++;
            _sbLast = "shoved past: " + how;
            Log.Write("STEP BETWEEN " + na + " and " + nb + ": " + _sbLast + "; the scene goes on");
        }

        /// One man's heeding of your word: his traits, drink, what he thinks of you (afraid, fond or hostile).
        private bool StepHeeds(Ped p, string seed, int now, out int chance, out string temper, out string regard)
        {
            MindWho w = MindKnown(p.Handle, now);
            Traits t = w != null && w.Src != null ? (w.Traits ?? (w.Traits = _engine.Shadow.TraitsOf(w.Src))) : null;
            bool drunk = w != null && InHisCups(p, w.P);
            regard = SightRegard(p, w != null ? w.Id : null);
            temper = w != null && w.P != null ? w.P.Personality : null;
            return Brawl.Heeds(t, drunk, false, seed, out chance, 1, BrawlVerbs.Standing(regard));
        }

        private string StepBetweenStatus()
        {
            if (!StepBetween.On)
                return "off ([Scenes] StepBetween)";
            return "offered " + _sbOffered + ", put off " + _sbPutOff + ", made up " + _sbMadeUp + ", shoved past " + _sbShoved + (_sbLast != null ? "; the last: " + _sbLast : "");
        }
    }
}
