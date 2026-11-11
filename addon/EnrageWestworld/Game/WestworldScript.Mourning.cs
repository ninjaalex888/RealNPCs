// Mourning and graves visited, in game (FIVEDAY item 26; Core/Mourning.cs). Every ten seconds in a town, among the people the
// mind knows near you: once a day a mourner's own thought within 30 m, once a day a neighbour within 8 m of them hushed for
// it. On a Sunday afternoon one mourner at a time near you (40 m), free and on their feet, walks to the grave (the town's
// "cemetery" spot within 150 m, else kneels where they are when within 60 m of the church) and kneels at it (the game's
// graveside kneel, 30 s, which ends by itself), once a Sunday each. If it's the grave of one you killed and you come within
// 10 m while they kneel, they know you. The choices' lean is Mind.cs's (ChoiceContext.Mourning). F7 "TEST mourning:".
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
        private sealed class GraveWalk
        {
            public Ped P;
            public string Id, Name, Dead;
            public bool ByYou, Kneeling, SawYou;
            public bool Night, Spoke;   // sitting up with the grave the night after the burial (DEPTH reserve R1), and whether they've spoken to you
            public Vector3 At;
            public int Until, KneelAt;
        }

        private GraveWalk _graveWalk;
        private int _mourningNextLook, _mourningThoughts, _graveVisits, _graveNights;
        private readonly HashSet<string> _mourningDone = new HashSet<string>();

        private bool Mourns(MindWho w, long g, out string dead, out string deadId)
        {
            dead = deadId = null;
            if (_cfg == null || !_cfg.Mourning || w == null || w.Id == null)
                return false;
            PersonState st = _store.Find(w.Id);
            return st != null && Mourning.MournOf(st.Mind, g, out dead, out deadId);
        }

        private void MourningStep(int now)
        {
            if (_graveWalk != null)
            {
                // a walk in hand: advanced out of town too, and let go if the switch went off (the Part 5 review: a held mourner
                // kept blocked events when you left town mid-walk)
                if (_cfg == null || !_cfg.Mourning)
                    EndGraveWalk("[World] Mourning switched off");
                else
                    GraveStep(now);
                return;
            }
            if (_cfg == null || !_cfg.Mourning || _store == null || _placeTown == null)
                return;
            if (now < _mourningNextLook || _mindNear.Count == 0)
                return;
            _mourningNextLook = now + 10000;
            Safe("mourning", () =>
            {
                Ped me = Game.Player.Character;
                long g = Bridge.Now(), day = g / 1440;
                int hour = (int)(g / 60 % 24);
                if (_mourningDone.Count > 400)
                    _mourningDone.Clear();
                foreach (int h in _mindNear)
                {
                    MindWho w;
                    string dead, deadId;
                    bool saints = _cfg.AllSaintsGraves && Mourning.AllSaints(InnerLife.ClockMonth, InnerLife.ClockDay, _placeTown, hour), saintsYours = false;
                    if (!_mindWho.TryGetValue(h, out w) || !ENTITY.DOES_ENTITY_EXIST(h) || ENTITY.IS_ENTITY_DEAD(h) || (!Mourns(w, g, out dead, out deadId) && !(saints && w.Id != null && _engine.DeadOfTheirs(w.Id, out dead, out deadId, out saintsYours))))
                        continue;
                    Ped p = new Ped(h);
                    float metres = p.Position.DistanceTo(me.Position);
                    string first = Person.FirstOf(dead);
                    // a Sunday afternoon: to the grave; and the night after the burial, one of theirs sits up with it (reserve R1)
                    DeathEntry died = deadId != null ? _store.DeathOf(deadId) : null;
                    bool night = _cfg.GraveNight && died != null && Mourning.GraveNight(g, died.At) && !_mourningDone.Contains("night|" + deadId + "|" + day);
                    if ((Mourning.GraveHour(InnerLife.Weekday, hour) || saints || night) && metres < 40f && _mourningDone.Add("grave|" + w.Id + "|" + day) &&
                        MayDirectWhyNot(p) == null && !NotFree(h) && !_director.Involves(h) && !PED.IS_PED_USING_ANY_SCENARIO(h) && !_staged.Contains(h))
                    {
                        Spot spot = Spots.Nearest(_spots, _placeTown, "cemetery", p.Position.X, p.Position.Y);
                        Vector3 church;
                        Vector3 at;
                        if (spot != null && new Vector3(spot.X, spot.Y, spot.Z).DistanceTo(p.Position) < 150f)
                            at = new Vector3(spot.X, spot.Y, spot.Z);
                        else if (Churches.TryGetValue(_placeTown, out church) && church.DistanceTo(p.Position) < 60f)
                            at = p.Position;   // no graveyard known: by the church, where they stand
                        else
                            continue;
                        bool byYou = saintsYours || _engine.MournsYourKill(w.Id, Mourning.Days);
                        _graveWalk = new GraveWalk { P = p, Id = w.Id, Name = w.Name, Dead = first, ByYou = byYou, At = at, Until = now + (night ? 150000 : 90000), Night = night };
                        if (night)
                            _mourningDone.Add("night|" + deadId + "|" + day);   // one of theirs a night, never a crowd at the grave
                        StagedHold(p);
                        TASK.TASK_FOLLOW_NAV_MESH_TO_COORD(h, at.X, at.Y, at.Z, 1.0f, 60000, 1.5f, 0, 0f);
                        _graveVisits++;
                        Log.Write("MOURNING " + w.Name + " walks to " + (first ?? "a") + "'s grave" + (byYou ? " (one you killed)" : "") + ", " + (int)at.DistanceTo(p.Position) + " m");
                        return;
                    }
                    if (metres <= 30f && _mourningDone.Add("own|" + w.Id + "|" + day))
                    {
                        string photo = _cfg.MemorialPhotos ? MemorialPhoto.MournerThought(_placeTown, first, w.Id + "|" + day) : null;   // the photograph that came (FIVEDAY R11)
                        _ticker.Push((w.Name ?? "Someone") + ", thinking", photo ?? Mourning.OwnThought(first, w.Id + "|" + day), "near", h);
                        _mourningThoughts++;
                        return;
                    }
                    foreach (int nh in _mindNear)
                    {
                        MindWho n;
                        string nd, ndi;
                        if (nh == h || !_mindWho.TryGetValue(nh, out n) || n.Id == null || !ENTITY.DOES_ENTITY_EXIST(nh) || ENTITY.GET_ENTITY_COORDS(nh, true, true).DistanceTo(p.Position) > 8f ||
                            Mourns(n, g, out nd, out ndi) || !_mourningDone.Add("hush|" + n.Id + "|" + w.Id + "|" + day))
                            continue;
                        _ticker.Push((n.Name ?? "Someone") + ", thinking", Mourning.HushThought(Person.FirstOf(w.Name), first, n.Id + "|" + day), "near", nh);
                        _mourningThoughts++;
                        return;
                    }
                }
            });
        }

        private int _graveStepAt;

        private void GraveStep(int now)
        {
            if (now < _graveStepAt && _graveStepAt - now <= 250)
                return;   // (TENHOUR item 1)
            _graveStepAt = now + 250;
            GraveWalk gw = _graveWalk;
            Ped me = Game.Player.Character;
            if (gw.P == null || !gw.P.Exists() || gw.P.IsDead || now > gw.Until || MISC.GET_MISSION_FLAG() || PED.IS_PED_IN_COMBAT(gw.P.Handle, 0))
            {
                EndGraveWalk("over");
                return;
            }
            int h = gw.P.Handle;
            if (!gw.Kneeling)
            {
                if (gw.P.Position.DistanceTo(gw.At) > 2.5f && now < gw.Until - 30000)
                    return;
                TASK.CLEAR_PED_TASKS(h, true, false);
                TASK.TASK_START_SCENARIO_IN_PLACE_HASH(h, Natives.Hash(Kneel), 30000, true, 0u, -1f, false);
                _ticker.Push((gw.Name ?? "Someone") + ", thinking", Mourning.GraveThought(gw.Dead, gw.ByYou, gw.Id + "|grave"), "near", h);
                gw.Kneeling = true;
                gw.KneelAt = now;
                gw.Until = now + 32000;
                Log.Write("MOURNING " + gw.Name + " kneels at " + (gw.Dead ?? "the") + "'s grave");
                return;
            }
            if (gw.Night && gw.Kneeling && !gw.Spoke && me.Position.DistanceTo(gw.P.Position) < 6f)
            {
                gw.Spoke = true;   // sitting up with the grave: whoever comes near hears them speak of the dead (reserve R1)
                SpeechCaption(Person.FirstOf(gw.Name) ?? "Someone", Mourning.SitUpWord(gw.Dead, gw.Id + "|night"), h, 5000);
                _graveNights++;
                Log.Write("MOURNING " + gw.Name + " sits up with " + (gw.Dead ?? "the") + "'s grave the night after the burial, and speaks of them to you");
            }
            if (gw.ByYou && !gw.SawYou && me.Position.DistanceTo(gw.P.Position) < 10f)
            {
                gw.SawYou = true;
                _ticker.Push((gw.Name ?? "Someone") + ", thinking", Mourning.SeeYouThought(gw.Dead, gw.Id + "|you"), "near", h);
                Log.Write("MOURNING " + gw.Name + " sees you at the grave of one you killed");
            }
        }

        private void EndGraveWalk(string why)
        {
            GraveWalk gw = _graveWalk;
            _graveWalk = null;
            if (gw != null && gw.P != null)
                StagedLetGo(gw.P, true);   // even one who's gone: off _staged (the Part 5 review)
        }

        private string MourningStatus()
        {
            if (_cfg == null || !_cfg.Mourning)
                return "off ([World] Mourning)";
            return _mourningThoughts + " mourning thoughts, " + _graveVisits + " walks to a grave this session, " + _graveNights + " sat up with a grave the night after the burial and spoke to you (reserve R1)" + (_graveWalk != null ? "; " + _graveWalk.Name + " at a grave now" : "");
        }
    }
}
