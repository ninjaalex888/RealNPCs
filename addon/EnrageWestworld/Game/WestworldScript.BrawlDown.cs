// A man beaten down in a brawl: killed or knocked out (Lex, 2026-10-31: "Ideally the fistfights should eventually be fatal
// or knockout depending on the NPC innermind. For now we can have fist fights go maybe 50% fatal, 50% knockout or sort of
// random generation that makes sense for exciting gameplay from a game designer perspective"). The Director drops him from
// the sides the frame his health falls under 30% of what he came in with (or he's found dead), and the brawl goes on
// without him; here his end is rolled (Core/Brawl.Fate: [Mind] BrawlFistsFatal, more fatal with several on him, a drunk or a
// hothead at him, bad blood, an old man; less with the law close, a friend of his in it, a fair fight). Knocked out: down
// 20-40 s, nobody can finish him, then up and on his way. Killed: the town's own death handling (LedgerNote: minds, word
// of it, the awake who see it). Before this every fist down lay there for good while the ledger called it "out cold".
// Found dead shot or cut (DiedOfBlows false) he is no beating: nothing is rolled, the ledger books him as any killing.
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
            public int Handle, By, At, Until;
            public string Id, Name, Kind;
        }

        private readonly Dictionary<int, BrawlKo> _brawlOut = new Dictionary<int, BrawlKo>();   // knocked out in a brawl: held down, unhurtable, until they come to
        private readonly HashSet<int> _brawlFatal = new HashSet<int>();   // killed in a brawl by our roll (or by its blows before it, never shot or cut: DiedOfBlows): a death, never "out cold", booked "a beating"; the ledger lets a gone body's handle go
        private readonly List<FatalDue> _brawlFatalDue = new List<FatalDue>();
        private int _brawlKnockedOut, _brawlCameTo, _brawlKilled, _brawlGameFirst;
        private string _brawlDownLast;

        /// From the Director: a fighter is down (h), the one on him (by), how many were on him, and whether the game had
        /// already killed him.
        private void OnFighterDown(int h, int by, int on, bool dead)
        {
            if (!ENTITY.DOES_ENTITY_EXIST(h))
                return;
            if (_afterFor == _director.FightId)
                _afterDowns++;   // for the town's word on it after (FOURTEENHOUR item 12)
            int now = Game.GameTime;
            string shockKey = "down|" + h + "|" + now / 10000;   // the street's event for him, once per fall (a handle is reused: the time keeps it one man's; the review)
            Ped p = new Ped(h);
            _known.Remove(h);   // the ledger may have him as nobody from before the brawl gave him a name (its 10 s cache)
            Known k = KnownAt(p, now);
            string name = k != null && k.Name != null ? k.Name : MindNameOf(h) ?? Director.Kind(p, !PED.IS_PED_MALE(h));
            if (dead && !DiedOfBlows(h))
            {
                // shot or cut while still in the fight (a lawman, a stray bullet, a drawn gun, Arthur's): a killing like any
                // other, the ledger's own with the game's cause, so it hushes the street; never "a beating" (the review,
                // 2026-10-31: _brawlFatal made the ledger book it so, and the death ledger said "a beating, by your hand")
                uint cause = PED.GET_PED_CAUSE_OF_DEATH(h);
                _brawlDownLast = name + " shot or cut down in the fight";
                Log.Write("BRAWL down: " + name + " lies dead in the fight, by " + (Weapons.Phrase(cause) ?? "a weapon") + ": not a beating (the ledger books it as the game has it)");
                StreetShock("killed", h, "brawl", shockKey);   // the street reacts as the game's own crowd would (WestworldScript.Street.cs)
                return;
            }
            // you put him down: the man he was beating is saved, and yours for good (WestworldScript.Saved.cs; Lex, 2026-11-03)
            int me = Game.Player.Character.Handle;
            if (by == me)
            {
                int beaten = _director.TargetOf(h);
                if (beaten != 0 && beaten != me && ENTITY.DOES_ENTITY_EXIST(beaten) && !ENTITY.IS_ENTITY_DEAD(beaten))
                    SavedByYou(beaten, "you put down the man beating them");
            }
            MindWho v = MindKnown(h, now), a = by != 0 && ENTITY.DOES_ENTITY_EXIST(by) ? MindKnown(by, now) : null;
            Traits at = a != null && a.Src != null ? (a.Traits ?? (a.Traits = _engine.Shadow.TraitsOf(a.Src))) : null;
            bool drunk = a != null && InHisCups(new Ped(by), a.P);
            int warmth = v != null && a != null && v.Id != null && a.Id != null ? LadderWarmth(a.Id, v.Id) : 0;
            bool old = Director.OldModel(p);
            Vector3 pos = p.Position;
            bool lawNear = BrawlFindLaw(pos, 30f, false, 0) != null;
            bool friendNear = false, fair = false;
            // the sides as they stood when he went down (the Director's own snapshot: after the drop the last down of a brawl
            // leaves a side empty, and the fair fight and the friend in it were never counted for him; the review, 2026-10-31)
            List<int> sa = _director.DownSideA, sb = _director.DownSideB;
            if (sa.Count > 0 && sb.Count > 0)
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
            StreetShock(fate == "fatal" ? "killed" : "knockedout", h, "brawl", shockKey);   // after the fate, not before it (the review): a man we beat to death is a killing to the street, the body after
            if (fate == "fatal")
            {
                _brawlKilled++;
                if (record && dead)
                {
                    Log.Write("BRAWL down: " + name + " lies dead, by " + byName + " (" + why + "; one of enRAGE's people, left to enRAGE)");
                    return;
                }
                _brawlFatal.Add(h);
                // the last blow is the man on him (the Director names one whenever anyone is still up on the other side): the
                // source of the death is his, so the ledger books it as another's hand with his name, never "their own"
                int src = by != 0 && ENTITY.DOES_ENTITY_EXIST(by) ? by : h;
                if (!dead)
                {
                    TASK.CLEAR_PED_TASKS(h, true, false);
                    PED.APPLY_DAMAGE_TO_PED(h, Math.Max(50, ENTITY.GET_ENTITY_HEALTH(h) + 50), 0, 0, src);
                }
                if (k != null && k.Id != null)
                    _brawlFatalDue.Add(new FatalDue { Handle = h, By = src != h ? src : 0, Id = k.Id, Name = k.Name, Kind = k.Kind, At = now + 600, Until = now + 6000 });
                Log.Write("BRAWL down: " + name + " is beaten to death by " + byName + " (" + why + ", " + on + " on him)");
                return;
            }
            _brawlKnockedOut++;
            ENTITY.SET_ENTITY_INVINCIBLE(h, true);   // nobody finishes a knocked-out man; undone when he comes to, and on a reload
            if (!PED.IS_PED_INCAPACITATED(h))
                TASK.CLEAR_PED_TASKS(h, true, false);   // one the game has down already is left down its own way
            Natives.Ragdoll(h, 20000, 30000);
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
                            Natives.Ragdoll(h, 6000, 9000);   // still out: down again
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
                    int src = d.By != 0 && ENTITY.DOES_ENTITY_EXIST(d.By) ? d.By : d.Handle;
                    PED.APPLY_DAMAGE_TO_PED(d.Handle, Math.Max(50, ENTITY.GET_ENTITY_HEALTH(d.Handle) + 50), 0, 0, src);   // the blow didn't take: again, his
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
                if (d.By != 0 && ENTITY.DOES_ENTITY_EXIST(d.By))
                {
                    _known.Remove(d.By);   // the killer too may be cached as nobody from before the brawl named him: his name for the book
                    KnownAt(new Ped(d.By), now);
                }
                LedgerNote(d.Handle, d.Id, d.Name, d.Kind, "a beating", null);
                if (d.By != 0 && d.By != Game.Player.Character.Handle && ENTITY.DOES_ENTITY_EXIST(d.By))
                {
                    MindWho kw = MindKnown(d.By, now);
                    if (kw != null && kw.Id != null)
                        _engine.PostReward(kw.Id);   // a resident beat a man to death: the sheriff posts a reward (FIVEDAY R30)
                }
            }
            if (_brawlFatal.Count > 200)
                _brawlFatal.RemoveWhere(x => !ENTITY.DOES_ENTITY_EXIST(x));
        }

        /// Whether a man of a brawl the game has dead died of its blows (no cause, fists: DeathLedger.ByBlows), not shot or
        /// cut. In the fight (OnFighterDown) and after it (BrawlAfterStep) the same test.
        private static bool DiedOfBlows(int h)
        {
            uint cause = PED.GET_PED_CAUSE_OF_DEATH(h);
            return DeathLedger.ByBlows(cause, cause != 0 ? WEAPON.GET_WEAPONTYPE_GROUP(cause) : 0u);
        }

        /// A reload or the script stopping: nobody stays unhurtable.
        private void BrawlDownsRelease()
        {
            foreach (int h in _brawlOut.Keys)
                if (ENTITY.DOES_ENTITY_EXIST(h))
                    ENTITY.SET_ENTITY_INVINCIBLE(h, false);
            _brawlOut.Clear();
        }

        private int _brawlAfterDowns, _brawlAfterQuietAt, _brawlAfterFor;

        /// Every frame for 15 s after a fight ends (Director.EndedFighters): the game's own melee carries on a moment after
        /// we let them go, and its blows knocked men out before any roll (Lex's Saint Denis brawl, 2026-10-31: two "out cold"
        /// 4 and 9 s after it was over). Anyone of it found incapacitated, or dead by fists, gets his fate rolled as in the
        /// fight, the man he was going for as the one on him. The watch ends early once nobody of it is in melee for 2 s.
        private void BrawlAfterStep(int now)
        {
            if (_director.EndedAt != _brawlAfterFor)
            {
                _brawlAfterFor = _director.EndedAt;   // a new fight's watch: never the last one's quiet moment (the review, 2026-10-31: a stale one ended it at once)
                _brawlAfterQuietAt = 0;
            }
            List<int[]> ended = _director.EndedFighters;
            if (ended.Count == 0)
            {
                _brawlAfterQuietAt = 0;
                return;
            }
            if (!BrawlsOn || now - _director.EndedAt > 15000 || now - _director.EndedAt < 0)
            {
                ended.Clear();
                _brawlAfterQuietAt = 0;
                return;
            }
            bool melee = false;
            for (int i = ended.Count - 1; i >= 0; i--)
            {
                int h = ended[i][0];
                if (!ENTITY.DOES_ENTITY_EXIST(h) || _brawlOut.ContainsKey(h) || _brawlFatal.Contains(h) || _director.TargetOf(h) != 0)
                {
                    ended.RemoveAt(i);   // gone, already down and rolled, or in a fight again (TargetOf: only while fists fly; the two who began it stay in the scene after, so not Involves)
                    continue;
                }
                bool dead = ENTITY.IS_ENTITY_DEAD(h);
                if (dead && !DiedOfBlows(h))
                {
                    ended.RemoveAt(i);   // shot or cut after the fight: not a beating, the ledger's own
                    continue;
                }
                if (dead || PED.IS_PED_INCAPACITATED(h))
                {
                    int by = ended[i][2];
                    if (by == 0 || !ENTITY.DOES_ENTITY_EXIST(by) || ENTITY.IS_ENTITY_DEAD(by))
                    {
                        by = 0;
                        foreach (int[] f in ended)
                            if (f[1] != ended[i][1] && ENTITY.DOES_ENTITY_EXIST(f[0]) && !ENTITY.IS_ENTITY_DEAD(f[0]))
                            {
                                by = f[0];   // the man he went for is down too: one of the other side still standing
                                break;
                            }
                    }
                    _brawlAfterDowns++;
                    Log.Write("BRAWL down (after the fight): " + (MindNameOf(h) ?? "a man") + " " + (dead ? "lies dead" : "is down") + " " + ((now - _director.EndedAt) / 1000.0).ToString("0.0") + " s after it ended; his fate is rolled");
                    _director.DownAfter(h, by, dead);   // removes him from the watch
                    return;   // one a frame
                }
                if (PED.IS_PED_IN_MELEE_COMBAT(h))
                    melee = true;
            }
            if (melee)
                _brawlAfterQuietAt = 0;
            else if (_brawlAfterQuietAt == 0)
                _brawlAfterQuietAt = now;
            else if (now - _brawlAfterQuietAt > 2000)
            {
                ended.Clear();   // nobody of it is swinging now: the watch ends
                _brawlAfterQuietAt = 0;
            }
        }

        private string BrawlDownStatus()
        {
            return "beaten down in brawls " + (_brawlKnockedOut + _brawlKilled) + ": " + _brawlKnockedOut + " knocked out (" + _brawlCameTo + " came to, " + _brawlOut.Count + " out now), " + _brawlKilled + " killed" +
                   (_brawlGameFirst > 0 ? " (" + _brawlGameFirst + " by the game's blow before the roll)" : "") + (_brawlAfterDowns > 0 ? " (" + _brawlAfterDowns + " after the fight was over)" : "") +
                   "; fatal share " + _cfg.BrawlFistsFatal + "%" + (_brawlDownLast != null ? "; last: " + _brawlDownLast : "");
        }
    }
}
