// The posse rides in game (FIVEDAY reserve R32; Core/PosseRides.cs, Core/Engine.Posse.cs). The ride is off screen; here,
// every thirty seconds in a town whose posse is out, a named person near thinks of the road. And the probe for the ride on
// screen (Testing row "posse:probe"): two named men near get up on two riderless horses; the first rides off from you (the
// smart flee from the saddle, as the horse thief's), the second follows him (the go-to-entity, as the mounted stalker's).
// Every two seconds for twenty, the log says how far each has gone and the gap between them: "PROBE posse".
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
        private int _posseNextThought, _posseThoughts;

        private void PosseStep(int now)
        {
            if (_cfg == null || !_cfg.PosseRides || _placeTown == null || _engine == null || now < _posseNextThought)
                return;
            _posseNextThought = now + 30000;
            if (!_engine.PosseOut(_placeTown))
                return;
            long day = Bridge.Now() / 1440;
            foreach (int h in _mindNear)
            {
                MindWho mw;
                if (!_mindWho.TryGetValue(h, out mw) || mw.Id == null || !ENTITY.DOES_ENTITY_EXIST(h) || ENTITY.IS_ENTITY_DEAD(h) || _store.Counts.ContainsKey(PosseRides.ManKey(mw.Id)) || !Voice.Chance(mw.Id + "|posse|" + day, 35))
                    continue;
                _ticker.Push((mw.Name ?? "Someone") + ", thinking", PosseRides.WaitingThought(mw.Id + "|" + day, mw.P != null && mw.P.Woman), "near", h);
                _posseThoughts++;
                break;
            }
        }

        // ---- the probe ----
        private sealed class PosseProbe
        {
            public Ped[] Men = new Ped[2], Horses = new Ped[2];
            public Vector3[] From = new Vector3[2];
            public bool[] Up = new bool[2];
            public int Started, Next, Until, Count;
        }
        private PosseProbe _probe;

        private string PosseProbeStart(int now)
        {
            if (_probe != null)
                return "a probe is running";
            Ped me = Game.Player.Character;
            int mine = PED.IS_PED_ON_MOUNT(me.Handle) ? PED.GET_MOUNT(me.Handle) : PED._GET_LAST_MOUNT(me.Handle);
            var horses = new List<Ped>();
            var riders = new HashSet<int>();
            var men = new List<Ped>();
            foreach (Ped p in Peds.All())
            {
                if (p == null || p.IsDead || p.Handle == me.Handle || p.Position.DistanceTo(me.Position) > 35f)
                    continue;
                if (PED.IS_PED_HUMAN(p.Handle))
                {
                    if (PED.IS_PED_ON_MOUNT(p.Handle))
                        riders.Add(PED.GET_MOUNT(p.Handle));
                    else if (!PED.IS_PED_USING_ANY_SCENARIO(p.Handle) && !_director.Involves(p.Handle) && !_staged.Contains(p.Handle) && MindNameOf(p.Handle) != null)
                        men.Add(p);
                    continue;
                }
                if (Natives.ModelName(p).StartsWith("a_c_horse", System.StringComparison.OrdinalIgnoreCase) && p.Handle != mine && ENTITY.GET_ENTITY_SPEED(p.Handle) < 0.3f)
                    horses.Add(p);
            }
            horses.RemoveAll(h => riders.Contains(h.Handle));
            int n = System.Math.Min(2, System.Math.Min(horses.Count, men.Count));
            if (n == 0)
                return "needs a named man and a riderless horse within 35 m (found " + men.Count + " men, " + horses.Count + " horses)";
            var pr = new PosseProbe { Started = now, Next = now + 2000, Until = now + 25000, Count = n };
            for (int i = 0; i < n; i++)
            {
                pr.Men[i] = men[i];
                pr.Horses[i] = horses.OrderBy(h => h.Position.DistanceTo(men[i].Position)).First(h => i == 0 || h.Handle != pr.Horses[0].Handle);
                PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(men[i].Handle, true);
                TASK.TASK_MOUNT_ANIMAL(men[i].Handle, pr.Horses[i].Handle, 20000, -1, 1.6f, 1, 0, 0);
                _staged.Add(men[i].Handle);
            }
            _probe = pr;
            Log.Write("PROBE posse: " + n + " men to " + n + " horses, mounting");
            return null;
        }

        private void PosseProbeStep(int now)
        {
            PosseProbe pr = _probe;
            if (pr == null || now < pr.Next)
                return;
            pr.Next = now + 2000;
            Ped me = Game.Player.Character;
            var b = new System.Text.StringBuilder("PROBE posse " + (now - pr.Started) / 1000 + " s:");
            for (int i = 0; i < pr.Count; i++)
            {
                int h = pr.Men[i].Handle;
                if (!ENTITY.DOES_ENTITY_EXIST(h) || ENTITY.IS_ENTITY_DEAD(h))
                {
                    b.Append(" man " + i + " gone;");
                    continue;
                }
                bool on = PED.IS_PED_ON_MOUNT(h) && PED.GET_MOUNT(h) == pr.Horses[i].Handle;
                if (on && !pr.Up[i])
                {
                    pr.Up[i] = true;
                    pr.From[i] = pr.Men[i].Position;
                    if (i == 0)
                        TASK.TASK_SMART_FLEE_PED(h, me.Handle, 400f, -1, 0, 3f, 0);   // rides off, as the horse thief
                    else
                        TASK.TASK_GO_TO_ENTITY(h, pr.Men[0].Handle, -1, 4f, 3f, 0f, 0);   // follows the first, as the mounted stalker
                }
                b.Append(" man " + i + (on ? " mounted, " + pr.Men[i].Position.DistanceTo(pr.From[i]).ToString("0") + " m ridden" : " not up yet") + ";");
            }
            if (pr.Count == 2 && pr.Up[0] && pr.Up[1])
                b.Append(" gap " + pr.Men[0].Position.DistanceTo(pr.Men[1].Position).ToString("0") + " m");
            Log.Write(b.ToString());
            if (now < pr.Until)
                return;
            for (int i = 0; i < pr.Count; i++)
            {
                int h = pr.Men[i].Handle;
                _staged.Remove(h);
                if (ENTITY.DOES_ENTITY_EXIST(h))
                {
                    PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(h, false);
                    WanderLater(h, now + 1000);
                }
            }
            Log.Write("PROBE posse: done");
            _probe = null;
        }

        /// A reload or the script stopping: the probe's men let go (the review).
        private void PosseProbeAbort()
        {
            PosseProbe pr = _probe;
            _probe = null;
            if (pr == null)
                return;
            for (int i = 0; i < pr.Count; i++)
            {
                if (pr.Men[i] == null)
                    continue;
                int h = pr.Men[i].Handle;
                _staged.Remove(h);
                if (ENTITY.DOES_ENTITY_EXIST(h))
                {
                    PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(h, false);
                    TASK.CLEAR_PED_TASKS(h, true, false);
                }
            }
        }

        private string PosseStatus()
        {
            return "posse: " + (_cfg != null && _cfg.PosseRides ? "on" : "off") + ", out now " + (_engine != null ? _engine.PossesOut : 0) + ", rode this session " +
                   (_engine != null ? _engine.PossesRode : 0) + ", thoughts " + _posseThoughts + (_probe != null ? ", a probe running" : "");
        }
    }
}
