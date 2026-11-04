// Friends wear a dot on the map whenever they're near you (Lex, 2026-11-03: greeted two people to the top of the bar and
// saw no dot: "a friendly dot on the minimap indicating they are now an ally/friend that would now protect the main
// character"). Every 2 s, the named people within FriendDotMetres with a warmth of 2 or more (AllyWarmth: enRAGE's fondness
// at AlliesLiking or more, someone you saved, a kindness remembered, a trouble mended) get the game's small companion dot
// in AlliesDotColour, labelled "<name>, a friend"; taken off when they go, die, or fall below (afraid of you, angry with
// you). The allies' own dot in a fight is the same dot (someone who wears one keeps it). [Encounters] AlliesDot (with
// AlliesHelp); F7 "TEST friends:"; log "FRIENDS" (detail).
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
        private const float FriendDotMetres = 80f;
        private const int FriendDotsEveryMs = 2000, FriendDotsLookMost = 32;
        private readonly Dictionary<int, Blip> _friendDots = new Dictionary<int, Blip>();
        private readonly Dictionary<int, string> _friendDotNames = new Dictionary<int, string>();
        private int _friendDotsNextAt, _friendDotsMade;

        private void FriendDotsStep(int now)
        {
            if (_cfg == null || now < _friendDotsNextAt)
                return;
            _friendDotsNextAt = now + FriendDotsEveryMs;
            if (!_cfg.AlliesDot || !_cfg.AlliesHelp)
            {
                if (_friendDots.Count > 0)
                    FriendDotsClear();
                return;
            }
            try
            {
                FriendDotsTick(now);
            }
            catch (Exception ex)
            {
                _friendDotsNextAt = now + 30000;
                Log.Write("FRIENDS the dots failed once (" + ex.Message + "); nothing for 30 s");
            }
        }

        private void FriendDotsTick(int now)
        {
            Ped me = Game.Player.Character;
            if (me == null || !me.Exists())
                return;
            Vector3 at = me.Position;
            var keep = new HashSet<int>();
            int looked = 0;
            foreach (Ped p in Peds.All())
            {
                if (p == null || looked >= FriendDotsLookMost)
                    continue;
                int h = p.Handle;
                if (h == me.Handle || !p.Exists() || ENTITY.IS_ENTITY_DEAD(h) || !PED.IS_PED_HUMAN(h) || p.Position.DistanceTo(at) > FriendDotMetres)
                    continue;
                if (Natives.IsLaw(p) || Natives.IsChild(p) || Natives.StoryModel(p) || Spawner.IsOurs(h))
                    continue;
                MindWho w = MindKnown(h, now);
                if (w == null || w.Id == null)
                    continue;
                looked++;
                if (AllyWarmth(p, w) < 2)
                    continue;
                keep.Add(h);
                if (!_friendDots.ContainsKey(h))
                {
                    Blip b = GreenDot(h, Person.FirstOf(w.Name ?? "A friend") + ", a friend");
                    if (b != null)
                    {
                        _friendDots[h] = b;
                        _friendDotNames[h] = w.Name ?? "a friend";
                        _friendDotsMade++;
                        if (Log.Debug)
                            Log.Detail("FRIENDS " + (w.Name ?? "someone") + " wears the dot (a friend of yours)");
                    }
                }
            }
            foreach (int h in _friendDots.Keys.Where(k => !keep.Contains(k)).ToList())
                FriendDotRemove(h);
        }

        /// The game's small companion dot on someone, in [Encounters] AlliesDotColour; null when they wear a dot already
        /// (enRAGE's, a regular's, a friend's) or the game refused.
        private Blip GreenDot(int h, string label)
        {
            try
            {
                if (MAP.GET_BLIP_FROM_ENTITY(h) != 0)
                    return null;
                var dot = new Blip(MAP.BLIP_ADD_FOR_ENTITY(Natives.Hash("BLIP_STYLE_COMPANION"), h));
                if (!string.IsNullOrEmpty(_cfg.AlliesDotColour))
                    MAP.BLIP_ADD_MODIFIER(dot.Handle, Natives.Hash(_cfg.AlliesDotColour.Trim()));
                MAP.SET_BLIP_SCALE(dot.Handle, _cfg.AlliesDotScale);   // the companion dot in that colour drew large (Lex, 2026-11-03): [Encounters] AlliesDotScale, 0.6 as built
                dot.Label = label;
                return dot;
            }
            catch (Exception ex)
            {
                Log.Write("FRIENDS the dot failed: " + ex.Message);
                return null;
            }
        }

        private void FriendDotRemove(int h)
        {
            Blip b;
            if (_friendDots.TryGetValue(h, out b))
            {
                try
                {
                    if (b != null && b.Exists())
                        b.Delete();
                }
                catch { }
            }
            _friendDots.Remove(h);
            _friendDotNames.Remove(h);
        }

        private void FriendDotsClear()
        {
            foreach (int h in _friendDots.Keys.ToList())
                FriendDotRemove(h);
        }

        private string FriendDotsStatus()
        {
            if (!_cfg.AlliesDot || !_cfg.AlliesHelp)
                return "off ([Encounters] AlliesDot with AlliesHelp)";
            return _friendDots.Count + " friends near you wear the dot" + (_friendDots.Count > 0 ? " (" + string.Join(", ", _friendDotNames.Values.Take(6)) + ")" : "") +
                   "; " + _friendDotsMade + " dots this session; within " + (int)FriendDotMetres + " m, a warmth of 2 or more (the bar at " + _cfg.AlliesLiking + " or more, someone you saved, a kindness remembered, a trouble mended)";
        }
    }
}
