// Returning people, cast (Run 3, item 5: the recognition plan's Stage 1; Core/Returners.cs says who and which outfit). With
// [World] Returners on, the town regulars' scan (WestworldScript.Regulars.cs), which every two seconds looks at the people the
// game has put in the street who have never been on screen, 20 to 150 metres away and hidden from you, tries a returner on
// ReturnersChance in a hundred of those bodies before the regulars: someone who matters to RealNPCs (woken, met before, a
// worry at its worst) of that body's build and sex, kept in the save, of this town. Their first return gives them an outfit
// of their own (one that none of the town's regulars, other returners or posts wears in that build); every return dresses
// them in it, unseen, so from then on they look the same each time. One scan and one set of bodies already looked at serve
// both, so the two never race for a body (the plan's critic, point 3). With Returners on, the woken no longer turn up in a
// stranger's body in plain sight: the old pick in ResidentOfMade stands down. No clothes are read: identity is written onto
// the body. enRAGE may still take a dressed body for one of its own people later; CastTakenStep logs each one it does.
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
        private int _returnersCast, _returnersWaiting, _castTaken, _nextCastTaken;
        private string _returnerLast;
        private readonly HashSet<string> _returnerNoRoomTold = new HashSet<string>();   // town|model: "no outfit left", told once a session
        private readonly HashSet<int> _castTakenTold = new HashSet<int>();               // our dressed bodies enRAGE took up, told once each

        /// Returners may be cast: switched on, and enRAGE can be asked whom it has looked at (else nobody is dressed at all).
        private bool ReturnersOn { get { return _cfg != null && _cfg.Returners && _bridge != null && _bridge.CanTellLooked; } }

        /// From the regulars' scan, on a body it has checked: someone who matters comes back in it, dressed as themselves. True
        /// when someone did.
        private bool ReturnerCast(int h, uint model, bool woman, string town, float d, long gameNow, int now)
        {
            Resident r = Returners.Back(_store.Residents, town, model, woman, x => _resBind.Values.Any(v => v.R == x) || RegularGone(x, now), ReturnerPull);
            if (r == null)
                return false;
            bool first = !r.HasOutfit;
            if (first)
            {
                int outfit = Returners.NewOutfit(_store.Residents, town, model, OutfitsOf(h, model), _cfg.ReturnersPerTown, r.Id + "|outfit", PostOutfits(town, model));
                if (outfit < 0)
                {
                    _returnersWaiting++;
                    if (_returnerNoRoomTold.Add(town + "|" + model))
                        Log.Write("RETURNER waits: " + r.Name + " (" + (r.Trade ?? "a resident") + " of " + town + ") has no outfit of their own and none is left for their build there " +
                                  "(ReturnersPerTown " + _cfg.ReturnersPerTown + ", or every preset worn); not back until one is (told once a session for this town and build)");
                    return false;
                }
                r.Outfit = outfit;
                r.HasOutfit = true;
            }
            int w = WatchEnter("returner dressed");
            PED._EQUIP_META_PED_OUTFIT_PRESET(h, r.Outfit, false);   // the preset only: nothing of the body is read
            PED._UPDATE_PED_VARIATION(h, false, true, true, true, false);
            Dressed.Note(h);   // not read for a while (Game/Dressed.cs)
            WatchLeave(w);
            _resBind[h] = new ResBind { R = r, Model = model };   // bound at once: ResidentOf's shortcut knows the body for whose it is
            r.Returns++;
            r.LastBodyAt = gameNow;
            _engine.KeepResident(r, gameNow);   // touched: the least lately back come first next time, and they stay in the save
            _returnersCast++;
            _returnerLast = r.Name;
            PersonState st = _store.Find(r.Id);
            Log.Write("RETURNER back: " + r.Name + " (" + (r.Trade ?? "a resident") + " of " + town + "), outfit " + r.Outfit + (first ? " (theirs from now on)" : "") + ", " + (int)d + " m off, unseen, " +
                      "back " + r.Returns + " time" + (r.Returns == 1 ? "" : "s") + (st != null && st.Sightings > 0 ? ", met " + st.Sightings + " time" + (st.Sightings == 1 ? "" : "s") + " before" : "") +
                      " (" + ReturnerWhy(r) + ")");
            return true;
        }

        /// Something to come back for (Core/Returners.Pull): a stage of waking, having met you, a worry at its worst now.
        private int ReturnerPull(Resident r)
        {
            PersonState st = _store.Find(r.Id);
            return Returners.Pull(st != null ? st.WakeStage : 0, st != null && st.Sightings > 0, WorryPressing(r));
        }

        /// Their worry is getting worse or at its crisis today (stage 1 or 2 of InnerLife's run): something pressing to come
        /// back to the street for.
        private bool WorryPressing(Resident r)
        {
            string kind;
            int stage;
            bool good;
            return InnerLife.WorryNow(_engine.PersonOfResident(r), _store.LastGameMin / 1440, out kind, out stage, out good) && (stage == 1 || stage == 2);
        }

        private string ReturnerWhy(Resident r)
        {
            PersonState st = _store.Find(r.Id);
            var why = new List<string>();
            if (st != null && st.WakeStage > 0)
                why.Add("waking, stage " + st.WakeStage);
            if (st != null && st.Sightings > 0)
                why.Add("met before");
            if (WorryPressing(r))
                why.Add("a worry at its worst");
            return why.Count > 0 ? string.Join(", ", why) : "kept";
        }

        /// The outfits the town's posts dress their holders of this build in (a post dresses its own: WestworldScript.Posts.cs).
        private List<int> PostOutfits(string town, uint model)
        {
            return _store.Posts.Where(x => x != null && x.Town == town && x.Outfit >= 0 && x.Model != null && MindScenarios.Joaat(x.Model) == model).Select(x => x.Outfit).ToList();
        }

        /// A save loaded: the store's dressed people marked as having an outfit (Core/Returners.MarkOutfits: 0 is an outfit too),
        /// and this session's marks of bodies dressed, read and taken up forgotten (after a load a handle is someone else).
        private void ReturnersLoaded()
        {
            int marked = _store != null ? Returners.MarkOutfits(_store.Residents) : 0;
            if (marked > 0)
                Log.Detail("RETURNERS: " + marked + " kept residents marked as wearing an outfit of their own (the regulars, the test cast, anyone given one)");
            Dressed.Clear();
            _lookProbed.Clear();
            _castTakenTold.Clear();
            _returnerNoRoomTold.Clear();
        }

        /// Every ten seconds, with enRAGE running: a body we dressed as one of ours (a town regular, a returner, the test cast)
        /// that enRAGE has since named or read the look of, logged once each. enRAGE may take our dressed people for its own (the
        /// plan's critic, point 12); how often it does is what Lex needs to see before Returners stays on.
        private void CastTakenStep(int now)
        {
            if (now - _nextCastTaken < 0 || _bridge == null || !_bridge.Present || !_bridge.CanTellLooked)
                return;
            _nextCastTaken = now + 10000;
            foreach (KeyValuePair<int, ResBind> kv in _resBind)
            {
                Resident r = kv.Value.R;
                if (r == null || !(r.Regular || r.Cast != null || (r.HasOutfit && r.Returns > 0)) || _castTakenTold.Contains(kv.Key) || !_bridge.Looked(kv.Key))
                    continue;
                _castTakenTold.Add(kv.Key);
                _castTaken++;
                string named = _bridge.EnrageNameAt(kv.Key, true);
                Log.Write("CAST taken up by enRAGE: " + r.Name + " (" + (r.Regular ? "a town regular" : r.Cast != null ? "the test cast" : "a returner") + ") is in a body enRAGE has " +
                          (named != null ? "named " + named : "taken a print of") + " since we dressed it; RealNPCs keeps its own name on them (once a body)");
            }
        }

        /// F7: the switch, who came back this session, how many wear an outfit of their own, who waits, and our dressed bodies
        /// enRAGE has taken up.
        private string ReturnersStatus()
        {
            if (_cfg == null || _store == null)
                return "not started";
            int dressed = _store.Residents.Count(x => x.HasOutfit && !x.Regular && x.Cast == null && x.PostId == null);
            string taken = _bridge != null && _bridge.Present ? "; " + _castTaken + " of our dressed bodies taken up by enRAGE this session (regulars and the test cast too)" : "";
            if (!_cfg.Returners)
                return "off ([World] Returners, in testing): the woken may turn up again in a stranger's body as the game dressed it; " + dressed + " kept residents with an outfit of their own" + taken;
            if (!ReturnersOn)
                return "waiting: enRAGE can't be asked whom it has looked at, so nobody is dressed" + taken;
            return _returnersCast + " came back this session" + (_returnerLast != null ? " (last: " + _returnerLast + ")" : "") + ", " + dressed + " kept residents with an outfit of their own (at most " +
                   _cfg.ReturnersPerTown + " a town), " + _returnersWaiting + " times one waited for an outfit; tried on " + _cfg.ReturnersChance + "% of the bodies the regulars' scan looks at" + taken;
        }
    }
}
