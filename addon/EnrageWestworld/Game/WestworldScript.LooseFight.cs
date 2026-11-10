// A fight left loose (Lex, 2026-11-02, Valentine: Ike Quarles and Thornton Hollowell came to blows, the scene stopped the
// same second, "someone in the scene is gone or threatened", and the two fought on with no lawman coming). The scene's
// director lets a fight scene go the moment one of it is in combat with you or you aim at one; the game may keep the two
// at each other, and the law's ear (WestworldScript.Brawl.cs) only follows the director's own fight. Here the two are
// followed after the stop: while they're still swinging at each other (and neither at you), after BrawlHeardMs from the
// first blows any free lawman within BrawlHeardMetres comes, sight or not, and breaks it up with a word: both stop, face
// him and go their ways. With [Mind] BrawlLaw, as the director's own.
using System;
using System.Linq;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Math;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private sealed class LooseFight
        {
            public int A, B, Since, Law, LawSince;
            public string Seed;
            public bool Owned;
        }

        private LooseFight _loose;
        private int _looseHeard, _looseBroken;

        /// Whose body a handle was, once it's gone (FOURTEENHOUR item 1d; the Director's line when one of a scene stops existing,
        /// as Lonnie did at his fight's first blow in Valentine, 2026-11-09): one we made or borrowed, the test cast's, one enRAGE
        /// keeps as a record, or the game's own, with the name the mind knew it by. Reads only what we hold: no native on the handle.
        private string GoneWhose(int h)
        {
            MindWho w;
            string mind = _mindWho.TryGetValue(h, out w) && w != null && !string.IsNullOrEmpty(w.Name) ? " (the mind knew it as " + w.Name + ")" : "";
            if (Spawner.IsOurs(h))
                return "a body we made or borrowed" + mind;
            if (Spawner.IsCast(h))
                return "a body of the test cast" + mind;
            if (IsRecord(h))
                return "a body enRAGE keeps as one of its records" + mind;
            return "a body the game made, nobody's that we know of" + mind;
        }

        private void OnFightLeftLoose(int a, int b, int blowsAt)
        {
            Ped me = Game.Player.Character;
            if (a == 0 || b == 0 || a == me.Handle || b == me.Handle)
                return;
            int now = Game.GameTime;
            _loose = new LooseFight { A = a, B = b, Since = blowsAt > 0 && blowsAt <= now ? blowsAt : now, Seed = "loose|" + a + "|" + now / 1000 };
            Log.Write("LAW loose fight: " + (MindNameOf(a) ?? "a man") + " and " + (MindNameOf(b) ?? "a man") + " fight on without the scene; the law may still hear it");
        }

        private static bool Swinging(int h)
        {
            return ENTITY.DOES_ENTITY_EXIST(h) && !ENTITY.IS_ENTITY_DEAD(h) && (PED.IS_PED_IN_MELEE_COMBAT(h) || PED.IS_PED_IN_COMBAT(h, 0));
        }

        /// From BrawlStep while the director has no fight of its own.
        private void LooseFightStep(Ped me, int now)
        {
            LooseFight f = _loose;
            if (f == null)
                return;
            if (_director.FightSides(out _, out _))
            {
                _loose = null;   // the director has a fight again: its own law follows that
                return;
            }
            bool on = Swinging(f.A) && Swinging(f.B) && !PED.IS_PED_IN_COMBAT(f.A, me.Handle) && !PED.IS_PED_IN_COMBAT(f.B, me.Handle);
            if (f.Law == 0)
            {
                if (!on || now - f.Since > 90000)
                {
                    _loose = null;
                    Log.Detail("LAW loose fight: over before anyone came (" + (now - f.Since) / 1000 + " s)");
                    return;
                }
                if (!_cfg.BrawlLaw || MISC.GET_MISSION_FLAG() || now - f.Since < BrawlHeardMs)
                    return;
                Vector3 mid = ENTITY.GET_ENTITY_COORDS(f.A, true, true);
                Ped law = BrawlFindLaw(mid, BrawlHeardMetres, false, f.A);
                if (law == null)
                    return;   // nobody free yet: looked for again next step
                f.Law = law.Handle;
                f.LawSince = now;
                f.Owned = LawTake(f.Law);
                TASK.CLEAR_PED_TASKS(f.Law, true, false);
                TASK.TASK_GO_TO_ENTITY(f.Law, f.A, -1, 4f, 2f, 0f, 0);   // at a run
                _looseHeard++;
                Log.Write("LAW loose fight: a lawman " + (int)law.Position.DistanceTo(mid) + " m off hears it and comes over (" + LawTakeWords(f.Owned) + ")");
                return;
            }
            Ped lw = new Ped(f.Law);
            if (!lw.Exists() || lw.IsDead || now - f.LawSince > 30000)
            {
                if (lw.Exists() && !lw.IsDead)
                    LawRoundsAgain(f.Law, now + 2000);
                _loose = null;
                return;
            }
            if (!on)
            {
                Log.Write("LAW loose fight: over as he got there (" + (now - f.LawSince) / 1000 + " s after he set off)");
                LawRoundsAgain(f.Law, now + 4000);
                _loose = null;
                return;
            }
            if (lw.Position.DistanceTo(ENTITY.GET_ENTITY_COORDS(f.A, true, true)) > 9f && now - f.LawSince < 12000)
                return;
            // there: the word, and both stop
            LawSay(lw, "The lawman", Brawl.LawBreaksUp(f.Seed), "SHOUT_STOP");
            foreach (int h in new[] { f.A, f.B })
            {
                if (!ENTITY.DOES_ENTITY_EXIST(h) || ENTITY.IS_ENTITY_DEAD(h))
                    continue;
                TASK.CLEAR_PED_TASKS(h, true, false);
                TASK.TASK_TURN_PED_TO_FACE_ENTITY(h, f.Law, 2500, 0f, 0f, 0f);
                WanderLater(h, now + 7000);
            }
            _looseBroken++;
            _brawlBreakups++;
            _brawlLawAfterUntil = now + 60000;   // the aftermath's own word stands aside: this was the word
            LawRoundsAgain(f.Law, now + 9000);
            Log.Write("LAW loose fight: the lawman breaks it up (" + (MindNameOf(f.A) ?? "a man") + " and " + (MindNameOf(f.B) ?? "a man") + ")");
            _loose = null;
        }
    }
}
