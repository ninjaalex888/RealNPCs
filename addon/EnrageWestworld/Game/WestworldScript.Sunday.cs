// Sunday, properly, in game (FIVEDAY item 25; Core/Sunday.cs). Every ten seconds on a Sunday, out of doors in a town whose
// church we know (the Churches table) and within 70 m of it: the hymn from the church now and then in the morning (a game
// quarter hour between), the churchgoers near you thinking over the sermon after it (once each a Sunday), and the dinner on
// the grounds in the box once a town a Sunday. The sermon's subject is the town's week (Sunday.Subject). The walk to
// church is Choosing's lean (Mind.cs's context: Devotion). F7 "TEST sunday:". Behind [World] SundayMorning.
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
        private int _sundayNextLook, _sundayHymns, _sundayThoughts;
        private long _sundayHymnAt;
        private readonly HashSet<string> _sundayDone = new HashSet<string>();   // id|day, and dinner|town|day

        /// The town's week, for the sermon: a killing of yours known here, a theft, a death among them, someone gone to drink.
        private string SermonSubject(string town, long g)
        {
            bool killing = false, theft = false, death = false, drink = false;
            foreach (Deed d in _store.Deeds)
            {
                if (d == null || d.Town != town || !d.Known || g - d.At > 7 * 1440L || g < d.At)
                    continue;
                string grp = Minds.DeedGroup(d.Kind);
                if (SharedFright.IsKilling(d.Kind)) killing = true;
                else if (grp == "deed:theft") theft = true;
            }
            foreach (Resident r in _store.Residents)
                if (r != null && r.Town == town && r.Dead && g - r.DiedAt >= 0 && g - r.DiedAt <= 7 * 1440L)
                {
                    death = true;
                    break;
                }
            foreach (Breakdown b in _store.Breakdowns)
                if (b.Way == StressLoad.Drink && g / 1440 - b.Day <= 7)
                {
                    Resident r = _store.FindResident(b.Id);
                    if (r != null && r.Town == town)
                        drink = true;
                }
            return Sunday.Subject(killing, theft, death, drink);
        }

        private void SundayStep(int now)
        {
            if (_cfg == null || !_cfg.SundayMorning || _store == null || now < _sundayNextLook || InnerLife.Weekday != 0 || _placeTown == null || _interiorType != 0)
                return;
            _sundayNextLook = now + 10000;
            Vector3 church;
            if (!Churches.TryGetValue(_placeTown, out church))
                return;
            Safe("sunday", () =>
            {
                Ped me = Game.Player.Character;
                if (me.Position.DistanceTo(church) > 70f || MISC.GET_MISSION_FLAG())
                    return;
                long g = Bridge.Now(), day = g / 1440;
                int clock = (int)(g % 1440), hour = clock / 60;
                if (_sundayDone.Count > 300)
                    _sundayDone.Clear();
                if (g < _sundayHymnAt)
                    _sundayHymnAt = 0;   // an earlier save loaded (the Part 5 review)
                if (clock >= Sunday.HymnFromMin && clock < Sunday.HymnToMin && g - _sundayHymnAt >= 15)
                {
                    _sundayHymnAt = g;
                    _sundayHymns++;
                    SpeechCaption("The church", Sunday.HymnLine(_placeTown + "|" + day + "|" + clock / 15), 0, 5000, false, false);
                    Log.Write("SUNDAY " + _placeTown + ": the hymn from the church");
                    return;
                }
                if (clock >= Sunday.HymnToMin && hour < Sunday.DinnerTo)
                {
                    if (hour >= Sunday.DinnerFrom && _sundayDone.Add("dinner|" + _placeTown + "|" + day))
                    {
                        NewsPush("Sunday", Sunday.DinnerLine(_placeTown, _placeTown + "|" + day), "news");
                        Log.Write("SUNDAY " + _placeTown + ": dinner on the grounds");
                        return;
                    }
                    string subject = null;
                    foreach (int h in _mindNear)
                    {
                        MindWho w;
                        if (!_mindWho.TryGetValue(h, out w) || w.Id == null || w.P == null || (w.P.Devotion != "Devout" && w.P.Devotion != "Churchgoer") || !ENTITY.DOES_ENTITY_EXIST(h) ||
                            ENTITY.GET_ENTITY_COORDS(h, true, true).DistanceTo(me.Position) > 30f || !_sundayDone.Add(w.Id + "|" + day))
                            continue;
                        subject = subject ?? SermonSubject(_placeTown, g);
                        _ticker.Push((w.Name ?? "Someone") + ", thinking", Sunday.AfterThought(subject, w.Id + "|" + day), "near", h);
                        _sundayThoughts++;
                        Log.Write("SUNDAY " + _placeTown + ": " + w.Name + " thinks over the sermon (" + subject + ")");
                        return;   // one a look
                    }
                }
            });
        }

        private string SundayStatus()
        {
            if (_cfg == null || !_cfg.SundayMorning)
                return "off ([World] SundayMorning)";
            return _sundayHymns + " hymns heard, " + _sundayThoughts + " sermons thought over this session" + (_placeTown != null && _store != null ? "; this week's sermon in " + _placeTown + ": " + SermonSubject(_placeTown, Bridge.Now()) : "");
        }
    }
}
