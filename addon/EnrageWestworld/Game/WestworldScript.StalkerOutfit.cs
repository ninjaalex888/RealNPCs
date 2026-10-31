// The Testing row "Stalker outfit: show the next candidate" (Lex, 2026-10-31: "can we dress the stalker in an all black
// outfit?"). Nothing offline says which preset of which model is black, so this shows them: each press stands a man 4 m in
// front of you in the next preset of the next candidate (Stalker.BlackCandidates, every preset of one model before the
// next), the one before taken away, and the log and the hint give the two ini lines for him. He goes after 90 s, on the
// next press, or with everything else on a reload. Only the preset is put on (the clothing-reading natives froze the game).
using System;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Math;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private int _soModel, _soPreset = -1, _soOutfits, _soUntil;
        private Ped _soPed;

        private string StalkerOutfitNext(int now, out string said)
        {
            said = null;
            Ped me = Game.Player.Character;
            if (PED.IS_PED_ON_MOUNT(me.Handle) || PED.IS_PED_IN_ANY_VEHICLE(me.Handle, false))
                return "you're riding: get down first";
            string[] all = Stalker.BlackCandidates;
            int mi = _soModel, preset = _soPreset + 1;
            if (_soPreset >= 0 && preset >= _soOutfits)
            {
                mi = (_soModel + 1) % all.Length;   // every preset of this one seen: the next model, from its first
                preset = 0;
            }
            uint model = Natives.Hash(all[mi]);
            if (!STREAMING.IS_MODEL_VALID(model))
            {
                _soModel = (mi + 1) % all.Length;
                _soPreset = -1;
                Log.Write("STALKER OUTFIT: " + all[mi] + " is not a model the game knows; skipped");
                return all[mi] + " is not a model the game knows: press again for the next";
            }
            if (!Spawner.Ready(model))
                return all[mi] + " is loading: press again in a moment";
            StalkerOutfitClear();
            Vector3 f = me.ForwardVector;
            Vector3 want = me.Position + new Vector3(f.X, f.Y, 0f) * 4f, at;
            if (!Natives.StandSpot(want, me.Position.Z, out at))
                at = want;
            float heading = (float)(Math.Atan2(-(me.Position.X - at.X), me.Position.Y - at.Y) * 180.0 / Math.PI);
            Ped p = Spawner.Spawn(model, at, heading);
            if (p == null)
                return "the game wouldn't make him here: try again a few steps on";
            int h = p.Handle;
            int n = PED.GET_NUM_META_PED_OUTFITS(h);
            if (preset >= n && n > 0)
            {
                // fewer presets than the last model had: this model from its first
                preset = 0;
            }
            if (n > 0)
            {
                PED._EQUIP_META_PED_OUTFIT_PRESET(h, preset, false);
                PED._UPDATE_PED_VARIATION(h, false, true, true, true, false);
            }
            PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(h, true);
            TASK.TASK_STAND_STILL(h, 90000);
            TASK.TASK_TURN_PED_TO_FACE_ENTITY(h, me.Handle, 3000, 0f, 0f, 0f);
            _soPed = p;
            _soModel = mi;
            _soPreset = n > 0 ? preset : 0;
            _soOutfits = Math.Max(1, n);
            _soUntil = now + 90000;
            string line = all[mi] + " preset " + _soPreset + " of " + n + " (model " + (mi + 1) + " of " + all.Length + "): ini StalkerBlackModel = " + all[mi] + ", StalkerBlackOutfit = " + _soPreset;
            Log.Write("STALKER OUTFIT candidate " + line);
            Hint("Stalker outfit: " + line, 12000);
            said = "A candidate for the stalker in black: " + all[mi] + " preset " + _soPreset;
            return null;
        }

        /// The candidate standing there goes (the next press, 90 s, a reload's cleanup has him too).
        private void StalkerOutfitClear()
        {
            if (_soPed != null && _soPed.Exists())
                Spawner.Release(_soPed, true);
            _soPed = null;
        }

        private void StalkerOutfitStep(int now)
        {
            if (_soPed != null && now > _soUntil)
                StalkerOutfitClear();
        }
    }
}
