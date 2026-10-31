// A man beaten down in a brawl: killed or knocked out (Lex, 2026-10-31: "Ideally the fistfights should eventually be fatal
// or knockout depending on the NPC innermind. For now we can have fist fights go maybe 50% fatal, 50% knockout or sort of
// random generation that makes sense for exciting gameplay from a game designer perspective"). The Director drops him from
// the sides the frame his health falls under 30% of what he came in with (or he's found dead), and the brawl goes on
// without him; here his end is rolled (Core/Brawl.Fate: [Mind] BrawlFistsFatal, more fatal with several on him, a drunk or a
// hothead at him, bad blood, an old man; less with the law close, a friend of his in it, a fair fight). Knocked out: down
// 20-40 s, nobody can finish him, then up and on his way. Killed: the town's own death handling (LedgerNote: minds, word
// of it, the awake who see it). Before this every fist down lay there for good while the ledger called it "out cold".
using System;
using System.Collections.Generic;
using System.Linq;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Math;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private sealed class BrawlKo
        {
            public int Until;
            public uint Model;
            public string Name;
        }

        private sealed class FatalDue
        {
            public int Handle, At, Until;
            public string Id, Name, Kind;
        }

        private readonly Dictionary<int, BrawlKo> _brawlOut = new Dictionary<int, BrawlKo>();   // knocked out in a brawl: held down, unhurtable, until they come to
        private readonly HashSet<int> _brawlFatal = new HashSet<int>();   // killed in a brawl by our roll (or before it): a death, never "out cold"
        private readonly List<FatalDue> _brawlFatalDue = new List<FatalDue>();
        private int _brawlKnockedOut, _brawlCameTo, _brawlKilled, _brawlGameFirst;
        private string _brawlDownLast;

        /// From the Director: a fighter is down (h), the one on him (by), how many were on him, and whether the game had
        /// already killed him.
        private void OnFighterDown(int h, int by, int on, bool dead)
        {
            if (!ENTITY.DOES_ENTITY_EXIST(h))
                return;
            int now = Game.GameTime;
            Ped p = new Ped(h);
            Known k = KnownAt(p, now);
            string name = k != null && k.Name != null ? k.Name : MindNameOf(h) ?? Director.Kind(p, !PED.IS_PED_MALE(h));
            MindWho v = MindKnown(h, now), a = by != 0 && ENTITY.DOES_ENTITY_EXIST(by) ? MindKnown(by, now) : null;
            Traits at = a != null && a.Src != null ? (a.Traits ?? (a.Traits = _engine.Shadow.TraitsOf(a.Src))) : null;
            bool drunk = a != null && InHisCups(new Ped(by), a.P);
            int warmth = v != null && a != null && v.Id != null && a.Id != null ? LadderWarmth(a.Id, v.Id) : 0;
            bool old = Director.OldModel(p);
            Vector3 pos = p.Position;
            bool lawNear = BrawlFindLaw(pos, 30f, false, 0) != null;
            bool friendNear = false, fair = false;
            List<int> sa, sb;
            if (_director.FightSides(out sa, out sb))
            {
                if (v != null && v.Id != null)
                    foreach (int f in sa.Concat(sb))
                    {
                        MindWho fw = f != h ? MindKnown(f, now) : null;
                        if (fw != null && fw.Id != null && LadderWarmth(v.Id, fw.Id) > 0 && new Ped(f).Position.DistanceTo(pos) < 15f)
                        {
                            friendNear = true;
                            break;
                        }
                    }
                fair = on <= 1 && Math.Abs(sa.Count - sb.Count) <= 1;
            }
            int pct;
            string why, fate;
            string seed = "fight" + _brawlFightFor + "|" + (v != null && v.Id != null ? v.Id : h.ToString()) + "|" + (a != null && a.Id != null ? a.Id : by.ToString());
            fate = Brawl.Fate(at, drunk, on, warmth, old, lawNear, friendNear, fair, _cfg.BrawlFistsFatal, seed, out pct, out why);
            bool record = IsRecord(h);
            if (dead)
            {
                // the game's own blow got there before the roll: a death, booked as one (it lay there for good as "out cold" before)
                _brawlGameFirst++;
                fate = "fatal";
                why = "the game's blow killed him before the roll (" + why + ")";
            }
            else if (record)
            {
                fate = "knockout";   // one of enRAGE's people: harm to a record goes only through a record-safe path (CLAUDE.md), so never killed by us
                why = "one of enRAGE's people, so only knocked out (" + why + ")";
            }
            string byName = by != 0 ? (a != null && a.Name != null ? a.Name : MindNameOf(by) ?? "a man") : "nobody in particular";
            _brawlDownLast = name + " " + (fate == "fatal" ? "killed" : "knocked out") + " by " + byName;
            if (fate == "fatal")
            {
                _brawlKilled++;
                if (record && dead)
                {
                    Log.Write("BRAWL down: " + name + " lies dead, by " + byName + " (" + why + "; one of enRAGE's people, left to enRAGE)");
                    return;
                }
                _brawlFatal.Add(h);
                if (!dead)
                {
                    TASK.CLEAR_PED_TASKS(h, true, false);
                    PED.APPLY_DAMAGE_TO_PED(h, Math.Max(50, ENTITY.GET_ENTITY_HEALTH(h) + 50), 0, 0, by != 0 ? by : h);   // the last blow, his: the source of the death is the man on him
                }
                if (k != null && k.Id != null)
                    _brawlFatalDue.Add(new FatalDue { Handle = h, Id = k.Id, Name = k.Name, Kind = k.Kind, At = now + 600, Until = now + 6000 });
                Log.Write("BRAWL down: " + name + " is beaten to death by " + byName + " (" + why + ", " + on + " on him)");
                return;
            }
            _brawlKnockedOut++;
            ENTITY.SET_ENTITY_INVINCIBLE(h, true);   // nobody finishes a knocked-out man; undone when he comes to, and on a reload
            TASK.CLEAR_PED_TASKS(h, true, false);
            PED.SET_PED_TO_RAGDOLL(h, 20000, 30000, 0, false, false, "");
            int ms = Brawl.KnockoutMs(seed);
            _brawlOut[h] = new BrawlKo { Until = now + ms, Model = ENTITY.GET_ENTITY_MODEL(h), Name = name };
            Log.Write("BRAWL down: " + name + " is knocked out cold by " + byName + " (" + why + ", " + on + " on him); down for " + ms / 1000 + " s");
        }

        /// Every 1.5 s: the knocked out kept down until they come to, then up and on their way; the killed booked as deaths.
        private void BrawlDownsStep(int now)
        {
            if (_brawlOut.Count > 0)
                foreach (int h in _brawlOut.Keys.ToList())
                {
                    BrawlKo ko = _brawlOut[h];
                    if (!ENTITY.DOES_ENTITY_EXIST(h) || ENTITY.GET_ENTITY_MODEL(h) != ko.Model)
                    {
                        _brawlOut.Remove(h);   // gone, or the handle is someone else's now
                        continue;
                    }
                    if (ENTITY.IS_ENTITY_DEAD(h))
                    {
                        ENTITY.SET_ENTITY_INVINCIBLE(h, false);
                        _brawlOut.Remove(h);
                        continue;
                    }
                    if (now < ko.Until)
                    {
                        if (!PED.IS_PED_RAGDOLL(h))
                            PED.SET_PED_TO_RAGDOLL(h, 6000, 9000, 0, false, false, "");   // still out: down again
                        continue;
                    }
                    _brawlOut.Remove(h);
                    ENTITY.SET_ENTITY_INVINCIBLE(h, false);
                    TASK.CLEAR_PED_TASKS(h, true, false);
                    WanderLater(h, now + 4000);
                    _brawlCameTo++;
                    Log.Write("BRAWL comes to: " + ko.Name + " gets up, dazed, and goes on his way");
                }
            for (int i = _brawlFatalDue.Count - 1; i >= 0; i--)
            {
                FatalDue d = _brawlFatalDue[i];
                if (now < d.At)
                    continue;
                bool there = ENTITY.DOES_ENTITY_EXIST(d.Handle);
                if (there && !ENTITY.IS_ENTITY_DEAD(d.Handle) && now < d.Until)
                {
                    PED.APPLY_DAMAGE_TO_PED(d.Handle, Math.Max(50, ENTITY.GET_ENTITY_HEALTH(d.Handle) + 50), 0, 0, d.Handle);   // the blow didn't take: again
                    d.At = now + 1500;
                    continue;
                }
                _brawlFatalDue.RemoveAt(i);
                if (!there || !ENTITY.IS_ENTITY_DEAD(d.Handle))
                {
                    Log.Write("BRAWL down: " + d.Name + " didn't die of it (" + (there ? "still alive after 6 s" : "gone from the world") + ")");
                    continue;
                }
                if (_deadNoted.Contains(d.Handle))
                    continue;   // the ledger's own pass booked it first
                _deadNoted.Add(d.Handle);
                LedgerNote(d.Handle, d.Id, d.Name, d.Kind, "a beating", null);
            }
            if (_brawlFatal.Count > 200)
                _brawlFatal.RemoveWhere(x => !ENTITY.DOES_ENTITY_EXIST(x));
        }

        /// A reload or the script stopping: nobody stays unhurtable.
        private void BrawlDownsRelease()
        {
            foreach (int h in _brawlOut.Keys)
                if (ENTITY.DOES_ENTITY_EXIST(h))
                    ENTITY.SET_ENTITY_INVINCIBLE(h, false);
            _brawlOut.Clear();
        }

        private string BrawlDownStatus()
        {
            return "beaten down in brawls " + (_brawlKnockedOut + _brawlKilled) + ": " + _brawlKnockedOut + " knocked out (" + _brawlCameTo + " came to, " + _brawlOut.Count + " out now), " + _brawlKilled + " killed" +
                   (_brawlGameFirst > 0 ? " (" + _brawlGameFirst + " by the game's blow before the roll)" : "") + "; fatal share " + _cfg.BrawlFistsFatal + "%" + (_brawlDownLast != null ? "; last: " + _brawlDownLast : "");
        }
    }
}
