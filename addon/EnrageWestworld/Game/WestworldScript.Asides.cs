// What they say and what they think (FIVEDAY2 item 25; Core/Asides.cs; [Ticker] Asides). Both of the ways a line gets said near you end up here as it shows (Feed.Tick for the voiced
// queue, CaptionNow for the street's captions): the nearest person in view within earshot of the speaker, who isn't busy thinking something else, may have a private thought about it,
// over their head, beside whatever else is up. Lines said to the player are left out: the Talk menu is frozen, and a bystander's opinion of your conversation would be a Talk feature.
// Someone in a scene of ours (a road scene, a circle, an execution) has that scene's own thoughts, so they think those.
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
        private int _asideNextAt, _asidesShown;
        private string _asideLast;
        private bool _asideForce;   // the Testing row: no roll and no gap, this once
        private readonly Dictionary<int, int> _asideAt = new Dictionary<int, int>();

        private void ListenersThink(int speaker, string label, string said, int ms)
        {
            if (_cfg == null || !_cfg.Asides || _engine == null || _ticker.Several < 2 || speaker == 0 || string.IsNullOrEmpty(said) || !ENTITY.DOES_ENTITY_EXIST(speaker))
                return;
            int now = Game.GameTime;
            if (now < _asideNextAt && now >= _asideNextAt - Asides.GapMs && !_asideForce)
                return;
            Vector3 at = ENTITY.GET_ENTITY_COORDS(speaker, true, true);
            NearThought best = null;
            float bd = Asides.EarshotMetres;
            for (int i = 0; i < _near.Count; i++)
            {
                NearThought n = _near[i];
                int was;
                if (n.Handle == 0 || n.Handle == speaker || n.Key == null || _ticker.Waiting(n.Handle) || !ENTITY.DOES_ENTITY_EXIST(n.Handle))
                    continue;
                if (!_asideForce && _asideAt.TryGetValue(n.Handle, out was) && now - was < Asides.PersonGapMs && now >= was)
                    continue;
                float d = ENTITY.GET_ENTITY_COORDS(n.Handle, true, true).DistanceTo(at);
                if (d < bd)
                {
                    bd = d;
                    best = n;
                }
            }
            if (best == null)
                return;
            string seed = best.Key + "|" + now / 15000;
            if (!_asideForce && !Voice.Chance(seed + "|aside", Asides.Chance))
                return;
            string line = AsideFor(best, label, said, seed);
            _asideForce = false;
            if (line == null || !_ticker.Aside(best.Name + ", thinking", line, best.Key, best.Handle, Math.Max(3500, ms + 1500), now))
                return;
            _asideNextAt = now + Asides.GapMs;
            if (_asideAt.Count > 200)
                PruneTimes(_asideAt, now, Asides.PersonGapMs);
            _asideAt[best.Handle] = now;
            _asidesShown++;
            _asideLast = best.Name + " on " + label + ": " + line;
            NoteTheirThought(best.Handle, now, line);
            Log.Detail("ASIDE " + _asideLast);
        }

        private string AsideFor(NearThought n, string label, string said, string seed)
        {
            if (IsGatherer(n.Handle) || IsHermit(n.Handle) || IsExecutionPed(n.Handle) || IsWaysidePed(n.Handle))
                return n.Text != null ? n.Text() : null;
            // someone who's passing or a stranger has no record, and asking who they are would make one
            Someone who = n.Key.StartsWith("road:", StringComparison.Ordinal) || n.Key.StartsWith("stranger:", StringComparison.Ordinal) ? null : SomeoneOf(new Ped(n.Handle));
            return Asides.Thought(said, who != null ? who.Temper : null, label, seed);
        }

        /// The Testing row: the nearest person says something, and the next nearest thinks about it.
        private string AsideTest(int now, out string said)
        {
            said = null;
            if (!_cfg.Asides || _ticker.Several < 2)
                return "[Ticker] Asides is off, or ThoughtsAtOnce is 1";
            List<NearThought> two = _near.Where(n => n.Handle != 0 && n.Key != null).OrderBy(n => n.Distance).Take(2).ToList();
            if (two.Count < 2)
                return "it wants two people in view";
            string line = Asides.TestLine(_asidesShown + _fires);
            _asideForce = true;
            SpeechCaption(two[0].Name, line, two[0].Handle, 4500, false, false);
            _asideForce = false;   // said at once, so spent, whether or not anyone was near enough to hear
            said = two[0].Name + " said a line; " + two[1].Name + ", if within " + (int)Asides.EarshotMetres + " m of them, thinks something about it";
            return null;
        }

        private string AsideStatus()
        {
            return (_cfg.Asides ? "listeners' thoughts on" : "listeners' thoughts off ([Ticker] Asides)") + ", " + _asidesShown + " this session" + (_asideLast != null ? ", last " + _asideLast : "");
        }
    }
}
