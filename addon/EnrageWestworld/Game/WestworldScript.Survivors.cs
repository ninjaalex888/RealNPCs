// Survivors come back changed, in game (FIVEDAY item 21; Core/Survivors.cs). The way a wrong of yours left someone you beat
// or robbed: read by the callout (the broken never come; the get-even bring up to two friends near, who stand and fight with
// them), by their choices (Mind.cs's context: the broken keep clear), by their thoughts (the act thoughts' chain, once a day
// the way's own), and by a friend near them, once a day a pair (SurvivorsStep). Behind [Mind] SurvivorsChanged.
using System;
using System.Collections.Generic;
using System.Linq;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private readonly HashSet<string> _survivorToldToday = new HashSet<string>();   // id|day and friend|id|day: once a day each
        private int _survivorNextLook, _survivorThoughts, _survivorFriendsBrought;

        /// The way a wrong of yours left this person, a day or more on, or null (none, or they let it lie).
        private string SurvivorWay(string id, Traits t, string temper, string name = null)
        {
            if (_cfg == null || !_cfg.SurvivorsChanged || id == null || _store == null)
                return null;
            long g = Bridge.Now();
            Deed wrong = Trust.LastWrong(_store.Deeds, id, g, name);
            if (wrong == null || g - wrong.At < Survivors.AfterMinutes)
                return null;
            return Survivors.Way(t, temper, id + "|" + (wrong.Id ?? wrong.At.ToString()));
        }

        private string SurvivorWayOf(MindWho w)
        {
            if (w == null || w.Id == null)
                return null;
            Traits t = w.Traits ?? (w.Src != null ? (w.Traits = _engine.Shadow.TraitsOf(w.Src)) : null);
            return SurvivorWay(w.Id, t, w.P != null ? w.P.Personality : null, w.Name);
        }

        private string SurvivorWayOf(Ped p, Someone who)
        {
            MindWho w = MindKnown(p.Handle, Game.GameTime);
            Traits t = w != null ? (w.Traits ?? (w.Src != null ? (w.Traits = _engine.Shadow.TraitsOf(w.Src)) : null)) : null;
            return SurvivorWay(who.Id, t, who.Temper, who.Name);
        }

        /// Up to two of their friends near (warm ties), free and on their feet, to come with them.
        private List<Ped> SurvivorFriends(Ped p, Someone who, Ped me)
        {
            var list = new List<Ped>();
            List<string> friends = Trials.FriendsOf(who.Id, _store.Ties);
            if (friends.Count == 0)
                return list;
            foreach (int h in _mindNear)
            {
                MindWho w;
                if (h == p.Handle || !_mindWho.TryGetValue(h, out w) || w.Id == null || !friends.Contains(w.Id) || !ENTITY.DOES_ENTITY_EXIST(h) || ENTITY.IS_ENTITY_DEAD(h))
                    continue;
                Ped f = new Ped(h);
                if (f.Position.DistanceTo(p.Position) > 30f || MayDirectWhyNot(f) != null || NotFree(h) || _director.Involves(h) || PED.IS_PED_ON_MOUNT(h))
                    continue;
                list.Add(f);
                if (list.Count >= Survivors.FriendsMost)
                    break;
            }
            return list;
        }

        /// The act thoughts' chain: the way's own thought, once a day each (half the time an act comes up).
        private string SurvivorActThought(MindWho w, string act)
        {
            if (_cfg == null || !_cfg.SurvivorsChanged || w == null || w.Id == null)
                return null;
            string way = SurvivorWayOf(w);
            if (way == null)
                return null;
            long day = _store.LastGameMin / 1440;
            string seed = w.Id + "|" + day;
            if (!Voice.Chance(seed + "|" + act + "|surv", 50) || !_survivorToldToday.Add(w.Id + "|" + day))
                return null;
            _survivorThoughts++;
            return Survivors.Thought(way, seed);
        }

        /// A friend near one who came back changed thinks of it, once a day a pair.
        private void SurvivorsStep(int now)
        {
            if (_cfg == null || !_cfg.SurvivorsChanged || _store == null || now < _survivorNextLook || _mindNear.Count < 2)
                return;
            _survivorNextLook = now + 20000;
            Safe("survivors", () =>
            {
                long day = _store.LastGameMin / 1440;
                if (_survivorToldToday.Count > 400)
                    _survivorToldToday.Clear();
                foreach (int h in _mindNear)
                {
                    MindWho w;
                    if (!_mindWho.TryGetValue(h, out w) || w.Id == null)
                        continue;
                    string way = SurvivorWayOf(w);
                    if (way == null)
                        continue;
                    List<string> friends = Trials.FriendsOf(w.Id, _store.Ties);
                    foreach (int fh in _mindNear)
                    {
                        MindWho f;
                        if (fh == h || !_mindWho.TryGetValue(fh, out f) || f.Id == null || !friends.Contains(f.Id) || !_survivorToldToday.Add("f|" + f.Id + "|" + w.Id + "|" + day))
                            continue;
                        string line = Survivors.FriendThought(way, Person.FirstOf(w.Name), f.Id + "|" + w.Id + "|" + day);
                        if (line == null)
                            continue;
                        _ticker.Push((f.Name ?? "Someone") + ", thinking", line, "near", fh);
                        _survivorThoughts++;
                        Log.Write("SURVIVOR " + f.Name + " thinks of " + w.Name + " (" + way + "): " + line);
                        return;   // one a look
                    }
                }
            });
        }

        private string SurvivorsStatus()
        {
            return _cfg == null || !_cfg.SurvivorsChanged ? "off ([Mind] SurvivorsChanged)" : _survivorThoughts + " thoughts this session; " + _survivorFriendsBrought + " friends brought to a callout";
        }
    }
}
