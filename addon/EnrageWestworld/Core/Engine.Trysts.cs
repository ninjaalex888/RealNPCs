// Nights together, the next steps (SIXDAY item 15; roadmap 26; Core/Trysts.cs): the engine's side. A night written down
// (her record: the first and the last, how many, what was paid); a married woman's husband may hear of it and come to
// town as the kin do (Core/KinGrudges.cs: a walk-up and words, never a gun; two to five days on; once); the morning
// after's thought when he's still about; a sweetheart's word when he's been gone a few days after two nights, and her
// cooling (affection down, the heart with it) when he's gone a fortnight. Behind [Romance] TrystsAfter. Nothing here
// touches the game.
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public sealed partial class Engine
    {
        private int _trystMornings, _trystExpectsSaid, _trystCooledCount, _husbandsHeard;

        /// The night, written down: her record, the husband's chance of hearing, and her thought after (the old Tryst's).
        /// town: where it was; seen: the saloon saw them go up; cents: what was paid.
        public string Tryst(Person p, string kind, long now, string town, bool seen, int cents)
        {
            string after = Tryst(p, kind, now);
            if (Cfg == null || !Cfg.TrystsAfter || p == null || p.Id == null)
                return after;
            if (Store.Trysts == null)
                Store.Trysts = new List<TrystRecord>();
            TrystRecord r = Store.Trysts.FirstOrDefault(x => x != null && x.WomanId == p.Id);
            if (r == null)
            {
                r = new TrystRecord { WomanId = p.Id, Name = p.Name, Town = town ?? p.Home, Kind = kind, At = now };
                Store.Trysts.Add(r);
                while (Store.Trysts.Count > Trysts.Keep)
                    Store.Trysts.RemoveAt(0);
            }
            r.Count++;
            r.LastAt = now;
            r.Kind = kind;
            r.Town = town ?? r.Town;
            r.MorningAt = 0;
            r.ExpectAt = 0;
            r.Cooled = false;
            r.PaidCents += cents;
            if (kind == "taken")
                HusbandHears(p, town ?? p.Home, now, seen);
            return after;
        }

        /// A married woman's man hears of it, or doesn't: one time in three, two in three when the saloon saw; once for her.
        /// He comes as the kin do (the walk-up in WestworldScript.KinGrudges.cs), two to five days on, with [Scenes]
        /// KinGrudges on. A woman spoken for but not married has nobody to send.
        private void HusbandHears(Person p, string town, long now, bool seen)
        {
            if (!Cfg.KinGrudges || town == null)
                return;
            PersonState st = Store.Get(p);
            if (st.Has("husband"))
                return;
            if (st.Family == null)
                st.Family = Family.For(p);
            Relative h = st.Family.FirstOrDefault(x => x != null && !x.Woman && x.Relation == "husband" && x.Name != null);
            if (h == null)
                return;
            int chance = Trysts.HusbandChance(seen);
            if (!Voice.Chance(p.Id + "|husband|" + now, chance))
            {
                Log("TRYST " + p.Name + ": her husband hears nothing of it (" + chance + "%)");
                return;
            }
            st.Mark("husband");
            if (Store.KinGrudges == null)
                Store.KinGrudges = new List<KinGrudge>();
            KinGrudge k = new KinGrudge
            {
                Id = Ids.New(), VictimId = p.Id, VictimName = p.Name, VictimTown = town, Wrong = "tryst", WrongWord = KinGrudges.WrongWord("tryst"), At = now,
                DueAt = now + (2 + Voice.Hash(p.Id + "|hdue") % 4) * 1440L, Name = h.Name, Relation = "husband", Town = h.Town, Temper = KinGrudges.TemperOf(h.Name)
            };
            Store.KinGrudges.Add(k);
            _husbandsHeard++;
            Log("TRYST " + p.Name + ": her husband " + h.Name + " hears of it (" + chance + "%; " + (seen ? "the saloon saw" : "nobody saw") + "); he comes to " + town + " in " + (k.DueAt - now) / 1440 + " days");
        }

        private TrystRecord TrystOf(string id)
        {
            return id != null && Store.Trysts != null ? Store.Trysts.FirstOrDefault(x => x != null && x.WomanId == id) : null;
        }

        /// Her thought the morning after (six to thirty-six hours on), once; null otherwise.
        public string TrystMorning(string id, long now)
        {
            TrystRecord r = Cfg != null && Cfg.TrystsAfter ? TrystOf(id) : null;
            if (r == null || r.MorningAt != 0)
                return null;
            long since = now - r.LastAt;
            if (since < 6 * 60L || since > 36 * 60L)
                return null;
            r.MorningAt = now;
            _trystMornings++;
            return Trysts.Morning(r.Kind, r.Count, id + "|" + r.LastAt);
        }

        /// A sweetheart's word to his face: a single woman after two nights, gone three days to a fortnight, once; null otherwise.
        public string TrystExpects(string id, long now)
        {
            TrystRecord r = Cfg != null && Cfg.TrystsAfter ? TrystOf(id) : null;
            if (r == null || r.Kind != "single" || r.Count < 2 || r.ExpectAt != 0 || r.Cooled)
                return null;
            long since = now - r.LastAt;
            if (since < Trysts.ExpectsFromDays * 1440L || since > Trysts.CoolsAfterDays * 1440L)
                return null;
            r.ExpectAt = now;
            _trystExpectsSaid++;
            return Trysts.Expects(id + "|" + r.LastAt);
        }

        /// A sweetheart who gave up waiting: a fortnight gone, her affection down and the heart with it, once; the thought.
        public string TrystCooled(string id, long now)
        {
            TrystRecord r = Cfg != null && Cfg.TrystsAfter ? TrystOf(id) : null;
            if (r == null || r.Kind != "single" || r.Count < 2 || r.Cooled || now - r.LastAt <= Trysts.CoolsAfterDays * 1440L)
                return null;
            r.Cooled = true;
            Person p = KinPerson(id);
            if (p != null)
            {
                PersonState st = Store.Get(p);
                st.Affection = Romance.Clamp(st.Affection - Trysts.CoolsBy, -100, 100);
                st.Heart = Math.Min(3, Romance.HeartFor(st.Affection, st.Flirts));
            }
            _trystCooledCount++;
            Log("TRYST " + (r.Name ?? id) + " gave up waiting for you (a fortnight); affection down " + Trysts.CoolsBy);
            return Trysts.Cooled(id + "|" + r.LastAt);
        }

        /// For the F7 dump.
        public string TrystsStatus(long now)
        {
            if (Cfg == null || !Cfg.TrystsAfter)
                return "the days after: off";
            List<TrystRecord> all = Store.Trysts ?? new List<TrystRecord>();
            TrystRecord last = all.OrderByDescending(x => x.LastAt).FirstOrDefault();
            return "the days after: " + all.Count + " women remembered, " + _trystMornings + " mornings after, " + _trystExpectsSaid + " words from a sweetheart, " + _trystCooledCount + " cooled, " +
                   _husbandsHeard + " husbands who heard" + (last != null ? "; last: " + last.Name + " (" + last.Kind + ", " + last.Count + ")" + (last.PaidCents > 0 ? ", $" + last.PaidCents / 100 + " paid" : "") + ", " + (now - last.LastAt) / 60 + " h ago" : "");
        }
    }
}
