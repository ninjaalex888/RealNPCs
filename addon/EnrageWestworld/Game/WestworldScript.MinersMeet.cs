// The miners' meeting, the game side (DEPTH reserve R7; Core/MinersMeet.cs; [Scenes] MinersMeet). A look every 30 s (the frame
// pass's scheduler; Tick04's own call with it off), in its own guard: at Annesburg after the shift on a weekday, once a game week,
// the town's working men in the mind's near scan (nobody made), men, free, grown, no story model, at least four within 10 m of the
// first, you within 25 m of them, and two other men 8 to 30 m off (the company's men). Then a beat every 3 s, in the same guard:
// the speaker's word, one who backs him, one who doubts, the ask, four answers said and the rest counted, the count; the company's
// men think it; the Around town box has it. Captions only: nobody is walked, turned or tasked. It ends quietly if the speaker is
// gone or you go past 40 m. Runner: the captions and the thoughts (seen), the news box (seen).
using System;
using System.Collections.Generic;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Math;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private sealed class MinersMeeting
        {
            public List<int> Men = new List<int>();
            public List<string> Names = new List<string>(), Tempers = new List<string>();
            public int Company0, Company1, Speaker, Beat, NextAt, Ayes, Asked;
            public string Seed, Motion, Week;
            public Vector3 At;
        }

        private MinersMeeting _miners;
        private Action _aMinersMeet;
        private int _mmNextLook, _mmHeld, _mmCarried, _mmLost, _mmCut;
        private string _mmLast;

        private void MinersMeetStep(int now)
        {
            if (!MinersMeet.On || now < _mmNextLook || _store == null)
                return;
            _mmNextLook = now + (_miners != null ? 500 : 30000);
            Safe("miners' meeting", _aMinersMeet ?? (_aMinersMeet = () => MinersMeetLook(_now)));
        }

        private bool MinerFree(int h)
        {
            if (!ENTITY.DOES_ENTITY_EXIST(h) || ENTITY.IS_ENTITY_DEAD(h) || !PED.IS_PED_MALE(h) || NotFree(h) || _director.Involves(h))
                return false;
            var p = new Ped(h);
            return !Natives.StoryModel(p) && IsGrownUp(p);
        }

        private void MinersMeetLook(int now)
        {
            if (_miners != null)
            {
                MinersMeetBeat(now);
                return;
            }
            long g = _store.LastGameMin;
            string week = "minersmeet|" + (g / (7 * 1440));
            if (!MinersMeet.Hour(_placeTown, CLOCK.GET_CLOCK_HOURS(), InnerLife.Weekday) || _store.Count(week) > 0)
                return;
            Vector3 me = Game.Player.Character.Position;
            var men = new List<int>(); var at = new List<Vector3>(); var who = new List<MindWho>();
            foreach (int h in _mindNear)
            {
                MindWho w;
                if (!_mindWho.TryGetValue(h, out w) || w.Id == null || w.P == null || w.P.Home != MinersMeet.Town || !MinerFree(h))
                    continue;
                men.Add(h); at.Add(ENTITY.GET_ENTITY_COORDS(h, true, true)); who.Add(w);
            }
            for (int i = 0, n = men.Count; i < n; i++)
            {
                if (who[i].TradeGroup != "working" || at[i].DistanceTo(me) > MinersMeet.HearMetres)
                    continue;
                var ring = new List<int>();
                for (int j = 0; j < men.Count && ring.Count < MinersMeet.Most; j++)
                    if (who[j].TradeGroup == "working" && at[j].DistanceTo(at[i]) <= MinersMeet.RingMetres)
                        ring.Add(j);
                if (ring.Count < MinersMeet.Least)
                    continue;
                var company = new List<int>();
                foreach (int h in _mindNear)
                {
                    if (company.Count >= 2 || ring.Exists(j => men[j] == h) || !ENTITY.DOES_ENTITY_EXIST(h) || !PED.IS_PED_MALE(h))
                        continue;
                    var cp = new Ped(h);
                    if (!IsGrownUp(cp) || Natives.StoryModel(cp))
                        continue;
                    MindWho cw;
                    if (_mindWho.TryGetValue(h, out cw) && cw.TradeGroup == "working")
                        continue;   // a working man standing off is no company man
                    float d = ENTITY.GET_ENTITY_COORDS(h, true, true).DistanceTo(at[i]);
                    if (d >= MinersMeet.CompanyNear && d <= MinersMeet.CompanyFar)
                        company.Add(h);
                }
                if (company.Count < MinersMeet.CompanyLeast)
                    continue;
                var m = new MinersMeeting { Seed = who[i].Id + "|" + g / 1440, Week = week, At = at[i], Company0 = company[0], Company1 = company[1], NextAt = now };
                foreach (int j in ring) { m.Men.Add(men[j]); m.Names.Add(who[j].Name); m.Tempers.Add(who[j].P.Personality); }
                m.Speaker = MinersMeet.Speaker(m.Tempers, m.Seed);
                m.Motion = MinersMeet.Motion(m.Seed);
                _store.Counts[week] = 1;
                _miners = m;
                _mmNextLook = now + 500;
                Log.Write("MINERS meeting at " + MinersMeet.Town + ": " + m.Men.Count + " men (" + string.Join(", ", m.Names) + "), " + m.Names[m.Speaker] + " speaks; two of the company's men watching; on " + m.Motion);
                return;
            }
        }

        private void MinersMeetBeat(int now)
        {
            MinersMeeting m = _miners;
            if (now < m.NextAt)
                return;
            int sp = m.Men[m.Speaker];
            if (!ENTITY.DOES_ENTITY_EXIST(sp) || ENTITY.IS_ENTITY_DEAD(sp) || Game.Player.Character.Position.DistanceTo(m.At) > 40f || _director.Involves(sp))
            {
                _mmCut++;
                _mmLast = "cut short at beat " + m.Beat;
                Log.Write("MINERS meeting " + _mmLast);
                _miners = null;
                return;
            }
            m.NextAt = now + 3000;
            string spName = Person.FirstOf(m.Names[m.Speaker]);
            int other = (m.Speaker + 1) % m.Men.Count, doubter = (m.Speaker + 2) % m.Men.Count;
            switch (m.Beat++)
            {
                case 0: SpeechCaption(spName, MinersMeet.Open(m.Seed), sp, 4500); return;
                case 1: SpeechCaption(Person.FirstOf(m.Names[other]), MinersMeet.Back(m.Seed), m.Men[other], 3500); return;
                case 2: SpeechCaption(Person.FirstOf(m.Names[doubter]), MinersMeet.Doubt(m.Seed), m.Men[doubter], 3500); return;
                case 3: SpeechCaption(spName, MinersMeet.Ask(m.Motion), sp, 3500); return;
            }
            if (m.Asked < m.Men.Count)
            {
                // the hands, the speaker's own first counted: four said aloud, the rest counted (a caption at a time keeps them readable)
                int i = m.Asked++;
                bool aye = i == m.Speaker || MinersMeet.Aye(m.Tempers[i], m.Seed + "|" + i);
                if (aye) m.Ayes++;
                if (i != m.Speaker && m.Asked <= 5 && ENTITY.DOES_ENTITY_EXIST(m.Men[i]))
                    SpeechCaption(Person.FirstOf(m.Names[i]), MinersMeet.Answer(aye, m.Seed + "|" + i), m.Men[i], 2500);
                else
                    m.NextAt = now;   // nothing said: on to the next hand at once
                return;
            }
            int of = m.Men.Count;
            SpeechCaption(spName, MinersMeet.Tally(m.Ayes, of), sp, 4500);
            bool carried = MinersMeet.Carried(m.Ayes, of);
            foreach (int h in new[] { m.Company0, m.Company1 })
                if (ENTITY.DOES_ENTITY_EXIST(h))
                {
                    MindWho cw;
                    string label = _mindWho.TryGetValue(h, out cw) && cw.Name != null ? Person.FirstOf(cw.Name) + ", thinking" : "The company's man, thinking";
                    _ticker.Push(label, MinersMeet.CompanyThinks(m.Seed + "|" + h), "near", h);
                }
            NewsPush(MinersMeet.Town, MinersMeet.News(m.Motion, m.Ayes, of), "news");
            _mmHeld++;
            if (carried) _mmCarried++; else _mmLost++;
            _mmLast = m.Ayes + " of " + of + " for " + m.Motion + (carried ? ", carried" : ", lost");
            Log.Write("MINERS meeting done: " + _mmLast);
            _miners = null;
        }

        private string MinersMeetStatus()
        {
            return !MinersMeet.On ? "off ([Scenes] MinersMeet)" : "held " + _mmHeld + " (carried " + _mmCarried + ", lost " + _mmLost + ", cut short " + _mmCut + ")" + (_miners != null ? "; one under way, beat " + _miners.Beat : "") + (_mmLast != null ? "; the last: " + _mmLast : "");
        }
    }
}
