// Three whistles in game (FIVEDAY item 28; Core/MineWhistle.cs, Core/Engine.MineWhistle.cs). When the engine's accident has
// just happened (the last two game hours) and you're in Annesburg out of doors, it plays once, in beats a few seconds apart:
// - the three whistles (a caption);
// - up to six of the free people near you make for the pithead (the town's "pithead" spot when recorded, within 400 m;
//   let go after a minute);
// - a woman's thought, waiting on the names;
// - the names up from the shaft (the box);
// - a man's anger at the company, and the hat going round (the box).
// F7 "TEST mine:". Behind [World] MineWhistle.
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
        private long _mineShownAt = -1;
        private int _mineBeat = -1, _mineBeatAt, _mineRan;
        private MineAccident _mineNow;

        private void MineStep(int now)
        {
            if (_cfg == null || !_cfg.MineWhistle || _engine == null)
                return;
            MineAccident acc = _engine.LastMine;
            if (_mineNow == null)
            {
                if (acc == null || acc.At == _mineShownAt || _placeTown != MineWhistle.Town || _interiorType != 0 || Bridge.Now() - acc.At > 120)
                    return;
                _mineNow = acc;
                _mineShownAt = acc.At;
                _mineBeat = 0;
                _mineBeatAt = now;
            }
            if (now < _mineBeatAt)
                return;
            Safe("mine whistle", () => MineBeat(now));
        }

        private void MineBeat(int now)
        {
            MineAccident acc = _mineNow;
            Ped me = Game.Player.Character;
            string seed = "mine|" + acc.At;
            switch (_mineBeat)
            {
                case 0:
                    SpeechCaption("The mine", MineWhistle.Whistle(seed), 0, 6000, false, false);
                    Log.Write("MINE three whistles heard in Annesburg");
                    _mineBeatAt = now + 7000;
                    break;
                case 1:
                    {
                        Spot pit = Spots.Nearest(_spots, MineWhistle.Town, "pithead", me.Position.X, me.Position.Y);
                        if (pit != null && new Vector3(pit.X, pit.Y, pit.Z).DistanceTo(me.Position) < 400f)
                        {
                            int sent = 0;
                            foreach (Ped p in Peds.All())
                            {
                                if (sent >= 6)
                                    break;
                                int h = p.Handle;
                                if (p == null || !p.Exists() || p.IsDead || h == me.Handle || !PED.IS_PED_HUMAN(h) || p.Position.DistanceTo(me.Position) > 60f ||
                                    MayDirectWhyNot(p) != null || NotFree(h) || _director.Involves(h) || _staged.Contains(h) || PED.IS_PED_ON_MOUNT(h))
                                    continue;
                                TASK.TASK_FOLLOW_NAV_MESH_TO_COORD(h, pit.X + (sent % 3) * 1.5f, pit.Y + (sent / 3) * 1.5f, pit.Z, 2.0f, 60000, 2f, 0, 0f);
                                WanderLater(h, now + 60000);
                                sent++;
                            }
                            _mineRan += sent;
                            Log.Write("MINE " + sent + " near you make for the pithead");
                        }
                        _mineBeatAt = now + 9000;
                        break;
                    }
                case 2:
                    MineThought(true, MineWhistle.WaitingThought(seed));
                    _mineBeatAt = now + 25000;
                    break;
                case 3:
                    NewsPush("Annesburg", MineWhistle.Names(acc.Names, seed), "news");
                    _mineBeatAt = now + 15000;
                    break;
                case 4:
                    MineThought(false, MineWhistle.AngerThought(seed));
                    NewsPush("Annesburg", MineWhistle.Collection(seed), "news");
                    _mineNow = null;
                    _mineBeat = -1;
                    return;
            }
            _mineBeat++;
        }

        /// A thought from one of the town near you: a woman waiting on the names, or a man angry at the company.
        private void MineThought(bool woman, string line)
        {
            Ped me = Game.Player.Character;
            foreach (int h in _mindNear)
            {
                MindWho w;
                if (!_mindWho.TryGetValue(h, out w) || w.Id == null || !ENTITY.DOES_ENTITY_EXIST(h) || PED.IS_PED_MALE(h) == woman || ENTITY.GET_ENTITY_COORDS(h, true, true).DistanceTo(me.Position) > 40f)
                    continue;
                _ticker.Push((w.Name ?? "Someone") + ", thinking", line, "near", h);
                return;
            }
        }

        private string MineStatusLine()
        {
            return _engine.MineStatus() + "; " + _mineRan + " ran to the pithead this session" + (_mineNow != null ? "; playing now, beat " + _mineBeat : "");
        }
    }
}
