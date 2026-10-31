// Being watched, the game side (Core/Observed.cs has the rules and the words; NIGHT3). When the street's own timing
// would start trouble (a reason's theft, shove, fight, standoff, or the noon callout) and a lawman within 25 m has a
// clear sight of the one who'd start it, or it's a Sunday morning by the church, it waits: the would-be doer glances at
// the law and thinks why (in view of the player, one thought in five minutes each), and the pair's reason stays theirs
// for later. A bold thief tries it anyway now and then ([Scenes] BoldThieves; a lift on you too). A chance meeting with the law looking keeps to words. A mind's own pick still decides. [Scenes] BeingWatched.
// F7 `TEST watched:`; log detail `WATCHED`.
using System.Collections.Generic;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Math;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private int _watchedLaw, _watchedChurch, _watchedChance;
        private string _watchedLast;
        private readonly Dictionary<int, int> _watchedThoughtAt = new Dictionary<int, int>();   // by the doer's handle: when they may think it again

        /// The nearest lawman (not one of the two, not the player) within Observed.LawMetres of `doer` with a clear sight
        /// of them, else null.
        private Ped LawWatching(Ped doer, Ped other)
        {
            Ped me = Game.Player.Character;
            Vector3 at = doer.Position;
            Ped best = null;
            float bestD = Observed.LawMetres;
            foreach (Ped p in World.GetAllPeds())
            {
                if (p == null || p.Handle == doer.Handle || (other != null && p.Handle == other.Handle) || (me != null && p.Handle == me.Handle))
                    continue;
                float d = p.Position.DistanceTo(at);   // the distance first: two natives fewer for each far body
                if (d >= bestD || !p.Exists() || p.IsDead || !Natives.IsLaw(p) || !Facing(p, at, 75f) || !ENTITY.HAS_ENTITY_CLEAR_LOS_TO_ENTITY(p.Handle, doer.Handle, 17))   // looking their way, not just a clear line (the review)
                    continue;
                best = p;
                bestD = d;
            }
            return best;
        }

        /// A Sunday morning, out of doors, by this town's church (the Sabbath scenes' hours and table).
        private bool ByChurchOnSunday(Ped p)
        {
            Vector3 church;
            return _placeTown != null && _interiorType == 0 && Churches.TryGetValue(_placeTown, out church) && Sabbath.Hour(InnerLife.Weekday, CLOCK.GET_CLOCK_HOURS()) &&
                   church.DistanceTo2D(p.Position) < Observed.ChurchMetres;
        }

        /// The chance meeting, once it has chosen trouble: whether the law looks on either of them (or it's Sunday by the
        /// church), so it keeps to words.
        private bool ChanceWatched(Ped a, Ped b)
        {
            if (!_cfg.BeingWatched || !(LawWatching(a, b) != null || LawWatching(b, a) != null || ByChurchOnSunday(a)))
                return false;
            _watchedChance++;
            return true;
        }

        /// A reason's scene: whether `kind` waits for the watcher (Observed.Waits by the doer's temper, drink and devotion);
        /// if so the doer glances at the law and thinks why, and it's counted.
        private bool WatchedHolds(string kind, Ped doerPed, Someone doer, Ped other, Ped me, string seed, int now)
        {
            if (!_cfg.BeingWatched || !Observed.Holds(kind) || doerPed == null || !doerPed.Exists())
                return false;
            Ped law = LawWatching(doerPed, other);
            bool church = law == null && ByChurchOnSunday(doerPed);
            if (law == null && !church)
                return false;
            // one roll a day for each doer and kind (the review: seeded by the minute, a hothead swung within three ticks)
            string daySeed = (doer != null ? doer.Id : doerPed.Handle.ToString()) + "|" + kind + "|" + _store.LastGameMin / 1440;
            if (!Observed.Waits(kind, law != null, church, doer != null ? doer.Temper : null, doer != null && doer.Drunk, doer != null && doer.P != null ? doer.P.Devotion : null, daySeed))
            {
                if (law != null && Observed.Thieving(kind))
                    NoteBold(doer != null ? doer.Name : "someone", kind, law, doerPed, daySeed);
                return false;
            }
            int next;
            if (_watchedThoughtAt.TryGetValue(doerPed.Handle, out next) && now < next)
                return true;   // held again: counted, logged and thought once in five minutes (the reason comes back every tick)
            if (law != null)
                _watchedLaw++;
            else
                _watchedChurch++;
            string name = doer != null ? doer.Name : "someone";
            Log.Detail("WATCHED " + name + ": " + kind + " waits (" + (law != null ? "a lawman " + (int)law.Position.DistanceTo(doerPed.Position) + " m off has them in sight" : "a Sunday morning by the church") + ")");
            if (_watchedThoughtAt.Count > 64)
                _watchedThoughtAt.Clear();
            _watchedThoughtAt[doerPed.Handle] = now + 300000;
            if (law != null)
                TASK.TASK_LOOK_AT_ENTITY(doerPed.Handle, law.Handle, 2500, 0, 51, 0);   // a glance at the badge
            if (me != null && doerPed.Position.DistanceTo(me.Position) < 25f && NodInView(me, doerPed))
            {
                string thought = Observed.Thought(kind, law != null, seed);
                _ticker.Push(name + ", thinking", thought, "near", doerPed.Handle);
                _watchedLast = name + ": " + thought;
                Log.Detail("WATCHED thought, " + name + ": " + thought);
            }
            return true;
        }

        /// A bold thief goes ahead with the law looking on ([Scenes] BoldThieves): counted and logged once a day each.
        private void NoteBold(string name, string kind, Ped law, Ped doerPed, string daySeed)
        {
            if (!_watchedBoldSeen.Add(daySeed))
                return;
            if (_watchedBoldSeen.Count > 64)
                _watchedBoldSeen.Clear();
            _watchedBold++;
            Log.Write("WATCHED " + name + ": " + kind + " goes ahead, bold, with a lawman " + (law != null && doerPed != null ? (int)law.Position.DistanceTo(doerPed.Position) + " m off" : "near") +
                      " looking on (" + Observed.BoldChance(null, false) + "% a day, [Scenes] BoldThieves)");
        }

        private int _watchedBold;
        private readonly HashSet<string> _watchedBoldSeen = new HashSet<string>();

        /// F7: on or off, what waited this session and the last thought.
        private string WatchedStatus()
        {
            if (!_cfg.BeingWatched)
                return "off ([Scenes] BeingWatched)";
            return "on; this session " + _watchedLaw + " held off with the law looking, " + _watchedChurch + " on a Sunday by the church, " + _watchedChance +
                   " chance meetings kept to words, " + _watchedBold + " bold thieves went ahead anyway (" + _cfg.BoldThieves + "%)" + (_watchedLast != null ? "; last: " + _watchedLast : "");
        }
    }
}
