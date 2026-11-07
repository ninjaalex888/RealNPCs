// The shape-up in game (FIVEDAY item 29; Core/ShapeUp.cs). From five to half past six in Saint Denis, once a day, when the
// town's "wharf" spot is recorded and you're within 80 m of it: up to eight of the named men the mind knows within 70 m of
// it, free and on foot, are held and walk to the wharf. The first is the foreman. After twenty seconds he calls his
// gang. The picked think of the wage and go, the passed-over think of the shame and go too, and their day leans to the
// saloon (ChoiceContext.PassedOver). A bitter one may have hard words with the last man picked, and a rematch plan
// (with MindPlans) between them follows. All are let go after. F7 "TEST shape-up:". Behind [World] ShapeUp.
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
        private sealed class ShapeUpRun
        {
            public readonly List<Ped> Men = new List<Ped>(8);   // the foreman first
            public List<MindWho> Who = new List<MindWho>();
            public Vector3 At;
            public int Step, NextAt, Until;
            public string Seed;
        }

        private ShapeUpRun _shape;
        private int _shapeNextLook, _shapeRuns, _shapeQuarrels;
        private long _shapeDay = -1;
        private string _shapeWhyNot;
        private readonly Dictionary<string, long> _passedOver = new Dictionary<string, long>();   // id -> the game day they were passed over

        private bool PassedOverToday(string id)
        {
            long day;
            return _cfg != null && _cfg.ShapeUp && id != null && _passedOver.TryGetValue(id, out day) && day == Bridge.Now() / 1440;
        }

        private void ShapeUpStep(int now)
        {
            if (_cfg == null || !_cfg.ShapeUp || _store == null)
                return;
            if (_shape != null)
            {
                if (now >= _shape.NextAt)
                    Safe("shape-up", () => ShapeUpBeat(now));
                return;
            }
            if (now < _shapeNextLook || _placeTown != ShapeUp.Town)
                return;
            _shapeNextLook = now + 5000;
            Safe("shape-up", () => ShapeUpLook(now, false));
        }

        private string ShapeUpLook(int now, bool test)
        {
            long g = Bridge.Now();
            if (!test && (!ShapeUp.Hour((int)(g % 1440)) || _shapeDay == g / 1440))
                return _shapeWhyNot = "dawn only (five to half past six), once a day";
            Ped me = Game.Player.Character;
            Spot wharf = Spots.Nearest(_spots, ShapeUp.Town, "wharf", me.Position.X, me.Position.Y);
            if (wharf == null)
                return _shapeWhyNot = "no wharf spot recorded in Saint Denis (the spots tool)";
            var at = new Vector3(wharf.X, wharf.Y, wharf.Z);
            if (at.DistanceTo(me.Position) > 80f)
                return _shapeWhyNot = "you're not near the wharf";
            var run = new ShapeUpRun { At = at, Seed = "shape|" + g / 1440, NextAt = now + 1000, Until = now + 120000 };
            foreach (int h in _mindNear)
            {
                MindWho w;
                if (run.Men.Count >= ShapeUp.MostMen || !_mindWho.TryGetValue(h, out w) || w.Id == null || !ENTITY.DOES_ENTITY_EXIST(h) || ENTITY.IS_ENTITY_DEAD(h) || !PED.IS_PED_MALE(h))
                    continue;
                Ped p = new Ped(h);
                if (p.Position.DistanceTo(at) > 70f || MayDirectWhyNot(p) != null || NotFree(h) || _director.Involves(h) || _staged.Contains(h) || PED.IS_PED_ON_MOUNT(h))
                    continue;
                run.Men.Add(p);
                run.Who.Add(w);
            }
            if (run.Men.Count < ShapeUp.LeastMen)
                return _shapeWhyNot = "too few men about the wharf (" + run.Men.Count + ")";
            _shapeDay = g / 1440;
            for (int i = 0; i < run.Men.Count; i++)
            {
                StagedHold(run.Men[i]);
                Vector3 to = i == 0 ? at : at + new Vector3((float)Math.Cos(i * 0.9) * 3f, (float)Math.Sin(i * 0.9) * 3f, 0f);
                TASK.TASK_FOLLOW_NAV_MESH_TO_COORD(run.Men[i].Handle, to.X, to.Y, to.Z, 1.4f, 30000, 1f, 0, 0f);
            }
            _shape = run;
            _shapeRuns++;
            Log.Write("SHAPEUP " + run.Men.Count + " men crowd the wharf in Saint Denis; " + (run.Who[0].Name ?? "the foreman") + " is the foreman" + (test ? " (a test)" : ""));
            return _shapeWhyNot = null;
        }

        private void ShapeUpBeat(int now)
        {
            ShapeUpRun r = _shape;
            if (now > r.Until || MISC.GET_MISSION_FLAG() || r.Men.Any(p => !p.Exists() || p.IsDead || PED.IS_PED_IN_COMBAT(p.Handle, 0)))
            {
                EndShapeUp("broken off");
                return;
            }
            Ped foreman = r.Men[0];
            switch (r.Step)
            {
                case 0:   // gathering
                    r.NextAt = now + 20000;
                    foreach (Ped p in r.Men.Skip(1))
                        TASK.TASK_TURN_PED_TO_FACE_ENTITY(p.Handle, foreman.Handle, 22000, 0f, 0f, 0f);
                    break;
                case 1:   // the call
                    {
                        SpeechCaption(r.Who[0].Name ?? "The foreman", ShapeUp.Call(r.Seed), foreman.Handle, 5000);
                        var men = r.Who.Skip(1).Select(w => w.Traits ?? (w.Src != null ? (w.Traits = _engine.Shadow.TraitsOf(w.Src)) : null)).ToList();
                        List<int> picked = ShapeUp.Pick(men, r.Seed);
                        long day = Bridge.Now() / 1440;
                        int last = picked.Count > 0 ? picked[picked.Count - 1] : -1;
                        for (int i = 0, all = men.Count; i < all; i++)
                        {
                            MindWho w = r.Who[i + 1];
                            bool got = picked.Contains(i);
                            if (!got)
                                _passedOver[w.Id] = day;
                            if (i < 3)
                                _ticker.Push((w.Name ?? "Someone") + ", thinking", got ? ShapeUp.PickedThought(w.Id + "|" + day) : ShapeUp.PassedThought(w.Id + "|" + day), "near", r.Men[i + 1].Handle);
                        }
                        int bitter = ShapeUp.Bitter(men, picked, r.Seed);
                        if (bitter >= 0 && last >= 0)
                        {
                            MindWho b = r.Who[bitter + 1], p = r.Who[last + 1];
                            SpeechCaption(b.Name ?? "A man passed over", ShapeUp.BitterWords(Person.FirstOf(p.Name), r.Seed), r.Men[bitter + 1].Handle, 4500);
                            SpeechCaption(p.Name ?? "The last man picked", ShapeUp.Answer(r.Seed), r.Men[last + 1].Handle, 3500);
                            if (_cfg.MindPlans)
                                Plans.Add(_store.Plans, b.Id, b.Name, Rematch.Again, p.Id, p.Name, ShapeUp.Town, 3, Bridge.Now());   // they'll have it out (the plans' fight, every brawl gate)
                            _shapeQuarrels++;
                            Log.Write("SHAPEUP " + b.Name + " passed over, takes it out on " + p.Name + ", the last man picked");
                        }
                        Log.Write("SHAPEUP the foreman picks " + picked.Count + " of " + men.Count);
                        r.NextAt = now + 9000;
                        break;
                    }
                default:
                    EndShapeUp(null);
                    return;
            }
            r.Step++;
        }

        private void EndShapeUp(string why)
        {
            ShapeUpRun r = _shape;
            _shape = null;
            if (r == null)
                return;
            foreach (Ped p in r.Men)
                if (p.Exists())
                    StagedLetGo(p, true);
            if (why != null)
                Log.Write("SHAPEUP over (" + why + ")");
        }

        private string ShapeUpStatus()
        {
            if (_cfg == null || !_cfg.ShapeUp)
                return "off ([World] ShapeUp)";
            return _shapeRuns + " shape-ups this session, " + _shapeQuarrels + " quarrels over the last place, " + _passedOver.Count + " passed over" + (_shapeWhyNot != null ? "; none now: " + _shapeWhyNot : "");
        }
    }
}
