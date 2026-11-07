// Lasting marks in game (FIVEDAY item 24; Core/LastingMarks.cs). Every few seconds, the people near you the mind knows who
// carry a mark of yours (a beating these four days, a knocking down today; Trust.LastWrong): once a day they think of it
// when you're within 25 m, and once a day someone fond of them nearby names you for it. With [Mind] LastingMarksLook (off
// until Lex has seen the probe) their body shows it the first time they're near this session: the gait and the damage pack
// in [Mind] LastingMarksGait / LastingMarksPack; the gait is cleared when the marks are gone. The Testing row "marks" tries
// the next candidate of each on the nearest named person and says which. F7 "TEST marks:".
using System;
using System.Collections.Generic;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private readonly HashSet<string> _marksToldToday = new HashSet<string>();
        private readonly Dictionary<int, string> _marksShown = new Dictionary<int, string>();   // ped -> id, the body marked this session
        private int _marksNextLook, _marksThoughts, _marksLooked, _marksProbe;
        private string _marksProbeLast;

        private void MarksStep(int now)
        {
            if (_cfg == null || !_cfg.LastingMarks || _store == null || now < _marksNextLook || _mindNear.Count == 0)
                return;
            _marksNextLook = now + 3000;
            Safe("lasting marks", () =>
            {
                Ped me = Game.Player.Character;
                long g = Bridge.Now(), day = g / 1440;
                if (_marksToldToday.Count > 400)
                    _marksToldToday.Clear();
                foreach (int h in _mindNear)
                {
                    MindWho w;
                    if (!_mindWho.TryGetValue(h, out w) || w.Id == null || !ENTITY.DOES_ENTITY_EXIST(h) || ENTITY.IS_ENTITY_DEAD(h))
                        continue;
                    Deed wrong = Trust.LastWrong(_store.Deeds, w.Id, g);
                    bool marked = wrong != null && LastingMarks.Shows(wrong.Kind, g - wrong.At);
                    string shown;
                    if (_marksShown.TryGetValue(h, out shown) && !marked)
                    {
                        PED._CLEAR_PED_DESIRED_LOCO_FOR_MODEL(h);   // mended
                        _marksShown.Remove(h);
                    }
                    if (!marked)
                        continue;
                    if (_cfg.LastingMarksLook && !_marksShown.ContainsKey(h))
                    {
                        if (!string.IsNullOrEmpty(_cfg.LastingMarksGait))
                            PED._SET_PED_DESIRED_LOCO_FOR_MODEL(h, _cfg.LastingMarksGait);
                        if (!string.IsNullOrEmpty(_cfg.LastingMarksPack))
                            PED.APPLY_PED_DAMAGE_PACK(h, _cfg.LastingMarksPack, wrong.Kind == "assault" ? 60f : 25f, 1f);
                        _marksShown[h] = w.Id;
                        _marksLooked++;
                    }
                    float metres = ENTITY.GET_ENTITY_COORDS(h, true, true).DistanceTo(me.Position);
                    if (metres > 25f)
                        continue;
                    if (_marksToldToday.Add(w.Id + "|" + day))
                    {
                        _ticker.Push((w.Name ?? "Someone") + ", thinking", LastingMarks.OwnThought(w.Id + "|" + day), "near", h);
                        _marksThoughts++;
                        Log.Write("MARKS " + w.Name + " carries your " + wrong.Kind + " (" + (g - wrong.At) / 1440 + " days on)");
                        return;   // one a look
                    }
                    List<string> friends = Trials.FriendsOf(w.Id, _store.Ties);
                    foreach (int fh in _mindNear)
                    {
                        MindWho f;
                        if (fh == h || !_mindWho.TryGetValue(fh, out f) || f.Id == null || !friends.Contains(f.Id) || !_marksToldToday.Add("f|" + f.Id + "|" + w.Id + "|" + day))
                            continue;
                        string you = _engine.KnowsYourName(f.Id) ? YourSurname() : null;
                        _ticker.Push((f.Name ?? "Someone") + ", thinking", LastingMarks.FriendThought(Person.FirstOf(w.Name), you, f.Id + "|" + day), "near", fh);
                        _marksThoughts++;
                        Log.Write("MARKS " + f.Name + " names you for " + w.Name + "'s " + wrong.Kind);
                        return;
                    }
                }
            });
        }

        /// The Testing row: the next gait and damage pack on the nearest named person (each press the next pair), said aloud.
        private string MarksProbe(Ped me)
        {
            int best = 0;
            float bd = 15f;
            foreach (int h in _mindNear)
            {
                if (!ENTITY.DOES_ENTITY_EXIST(h) || ENTITY.IS_ENTITY_DEAD(h))
                    continue;
                float d = ENTITY.GET_ENTITY_COORDS(h, true, true).DistanceTo(me.Position);
                if (d < bd)
                {
                    bd = d;
                    best = h;
                }
            }
            if (best == 0)
                return "nobody named within 15 m to try it on";
            int i = _marksProbe++;
            string gait = LastingMarks.Gaits[i % LastingMarks.Gaits.Length], pack = LastingMarks.Packs[i % LastingMarks.Packs.Length];
            PED.CLEAR_PED_BLOOD_DAMAGE(best);
            PED._SET_PED_DESIRED_LOCO_FOR_MODEL(best, gait);
            PED.APPLY_PED_DAMAGE_PACK(best, pack, 60f, 1f);
            TASK.TASK_WANDER_STANDARD(best, 10f, 10);   // walking, so the gait shows
            _marksProbeLast = "gait \"" + gait + "\", damage pack \"" + pack + "\" on " + TalkName(new Ped(best));
            Log.Write("MARKS probe " + (i + 1) + ": " + _marksProbeLast);
            return null;
        }

        private string MarksStatus()
        {
            if (_cfg == null || !_cfg.LastingMarks)
                return "off ([Mind] LastingMarks)";
            return _marksThoughts + " thoughts of a mark of yours this session; look " + (_cfg.LastingMarksLook ? "on (gait \"" + _cfg.LastingMarksGait + "\", pack \"" + _cfg.LastingMarksPack + "\"), " + _marksLooked + " bodies marked" : "off ([Mind] LastingMarksLook)") +
                   (_marksProbeLast != null ? "; the last probe: " + _marksProbeLast : "");
        }
    }
}
