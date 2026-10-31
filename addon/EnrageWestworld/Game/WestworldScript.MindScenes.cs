// People start the street's interactions from their own thoughts (NIGHT3 item 1, step 2 of the model plan agreed with Lex
// on 2026-10-31). Until now a scene began when the Director's own cadence took the strongest pair reason near the player
// (Reasons.cs ReasonScene). Now, when the model is asked what someone near does (Picks.cs PickAsk), the reasons the street
// already has for them and another, where they are the one who would start it, join the list in their own words
// (Core/Reasons.Act.cs: "goes to have words with Hiram over money", "goes to offer Ruth sympathy"). If the model picks
// one, it plays through the very path the Director's pick takes (PlayPairReason): nothing is invented, only who chooses
// changes. Only while the street could take a scene (MindSceneWhyNot), the pair's rest kept (NotePairPlayed), and only
// with [Mind] MindStartsScenes (on), LanguagePicks, Language and MindLive. Log "MIND starts:"; F7 TEST picks:.
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
        private const int MindReasonsMs = 10000;   // the pair reasons near the player, worked out at most this often for the picks (3 s until the cost audit: a 1-3 ms frame each time in a crowd)
        private const int MindScenesOffered = 2;  // at most this many of a person's own reasons join one pick's list
        private List<PairReason> _mindReasons = new List<PairReason>();
        private int _mindReasonsAt = -100000;
        private int _mindScenesOffer, _mindScenesPicked, _mindScenesStarted, _mindScenesFailed;
        private string _mindSceneLast;

        /// Why a person's own pick can't start a scene now (the street's own rails, as the Director's tick has them), or null.
        private string MindSceneWhyNot(bool test = false)
        {
            if (!test && !_cfg.MindStartsScenes) return "off ([Mind] MindStartsScenes)";
            if (!_cfg.Drama) return "street scenes are off";
            if (_director.Busy) return "a street scene is going on";
            if (_stalker != null || _wedding != null || _theft != null || _plea != null || _visitor != null) return "something else has the street";
            if (_interiorType != 0 && _placeKind != "saloon") return "indoors";
            if (_missionPaused) return "a mission";
            return null;
        }

        /// This person's own reasons with someone near, where they'd start it, strongest first, with their words.
        private List<KeyValuePair<PairReason, string>> MindReasonsFor(string id, int now, bool test = false)
        {
            var mine = new List<KeyValuePair<PairReason, string>>();
            if (id == null)
                return mine;
            if (now - _mindReasonsAt >= MindReasonsMs || now < _mindReasonsAt || test)
            {
                _mindReasonsAt = now;
                int w = WatchEnter("mind scenes: the pair reasons");
                List<Candidate> people = ReasonCandidates(Game.Player.Character, now, test);
                _mindReasons = people.Count >= 2 ? PairReasons(people, _placeKind == "saloon").OrderByDescending(x => x.Why.Strength).ToList() : new List<PairReason>();   // sorted once, strongest first
                WatchLeave(w);
            }
            foreach (PairReason pr in _mindReasons)
            {
                Candidate first = pr.Why.BFirst ? pr.B : pr.A, second = pr.Why.BFirst ? pr.A : pr.B;
                Candidate chooser = Reasons.ChosenBySecond(pr.Why, first.Who, Bridge.Now() / 1440) ? second : first, other = chooser == first ? second : first;   // the friend offers sympathy, the wronged keep clear, the one owed calls it in
                if (chooser.Who == null || chooser.Who.Id != id || other.Who == null)
                    continue;
                string words = Reasons.ActWords(pr.Why, Person.FirstOf(other.Who.Name));
                if (words == null)
                    continue;
                mine.Add(new KeyValuePair<PairReason, string>(pr, words));
                if (mine.Count >= MindScenesOffered)
                    break;
            }
            return mine;
        }

        /// The reasons offered alongside a pick's own options (ids "scene:0", "scene:1"); null when none.
        private List<PairReason> MindScenesInto(MindWho w, int now, List<string> ids, List<string> words)
        {
            if (MindSceneWhyNot() != null)
                return null;
            List<KeyValuePair<PairReason, string>> mine = MindReasonsFor(w.Id, now);
            if (mine.Count == 0)
                return null;
            var scenes = new List<PairReason>();
            foreach (KeyValuePair<PairReason, string> kv in mine)
            {
                ids.Add("scene:" + scenes.Count);
                words.Add(kv.Value);
                scenes.Add(kv.Key);
            }
            _mindScenesOffer++;
            return scenes;
        }

        /// The model picked one of the person's own reasons: played as the Director would play it, if the street still can.
        private bool MindStartScene(PairReason pr, MindWho w, string words, string by, int now)
        {
            _mindScenesPicked++;
            string why = MindSceneWhyNot(by == "the Testing row");
            if (why == null && (!pr.A.Ped.Exists() || pr.A.Ped.IsDead || !pr.B.Ped.Exists() || pr.B.Ped.IsDead))
                why = "one of them is gone";
            if (why == null && (NotFree(pr.A.Ped.Handle) || NotFree(pr.B.Ped.Handle)))
                why = "one of them is taken up (a scene, Talk, a walk-up)";
            bool started = why == null && PlayPairReason(pr, Game.Player.Character, _placeKind == "saloon", now, false, by);
            if (started)
            {
                _mindScenesStarted++;
                _mindSceneLast = w.Name + " " + words;
                Log.Write("MIND starts: " + w.Name + " " + words + " (" + by + ")");
            }
            else
            {
                _mindScenesFailed++;
                Log.Write("MIND starts: " + w.Name + " would " + words + ", but not now (" + (why ?? "the scene couldn't be played") + "): the mind's own instead");
            }
            return started;
        }

        /// The Testing row: the nearest person with a reason of their own, asked (the model, when it answers) or played.
        private string MindSceneTest(int now)
        {
            string why = MindSceneWhyNot(true);
            if (why != null)
                return why;
            Ped me = Game.Player.Character;
            foreach (Candidate c in ReasonCandidates(me, now, true).OrderBy(x => x.Ped.Position.DistanceTo(me.Position)))
            {
                List<KeyValuePair<PairReason, string>> mine = MindReasonsFor(c.Who != null ? c.Who.Id : null, now, true);
                if (mine.Count == 0)
                    continue;
                int h = c.Ped.Handle;
                MindWho w = MindKnown(h, now);
                if (PicksOn && w != null && w.Id != null && w.Id == c.Who.Id && w.Choice != null)
                {
                    string own = w.Choice;
                    var ids = new List<string> { own };
                    var words = new List<string> { Choosing.WordsOf(own) };
                    var scenes = new List<PairReason>();
                    foreach (KeyValuePair<PairReason, string> kv in mine)
                    {
                        ids.Add("scene:" + scenes.Count);
                        words.Add(kv.Value);
                        scenes.Add(kv.Key);
                    }
                    string rwhy;
                    LanguageRequest r = MindRequestOf(w, h, now, out rwhy);
                    if (r != null)
                    {
                        r.Kind = "pick";
                        r.Options = words;
                        r.WithThought = true;
                        string key = "pick|" + w.Id + "|test|" + Environment.TickCount;
                        if (_engine.LanguageAnswers.AskPick(new LanguageAsk { Key = key, Name = w.Name, PersonId = w.Id, Request = r, AskedAtMs = Environment.TickCount }))
                        {
                            _picksAsked++;
                            _mindScenesOffer++;
                            _pending.Add(new PendingPick { W = w, Handle = h, Key = key, Mind = w.Choice, Ids = ids, Words = words, AskedAt = now, Deadline = now + PickWaitMs, Req = r, Scenes = scenes, Test = true });
                            Log.Write("MIND starts (test): " + w.Name + " is asked: " + string.Join(" | ", words.Select((x, k) => (k + 1) + " " + x)));
                            return null;
                        }
                    }
                }
                // no model to ask: their strongest reason, played as the model's pick would be
                MindWho who = w ?? new MindWho { Id = c.Who.Id, Name = c.Who.Name };
                return MindStartScene(mine[0].Key, who, mine[0].Value, "the Testing row", now) ? null : "their reason couldn't be played now (the log says why)";
            }
            return "nobody near with a reason of their own to start a scene (named people with ties, worries or a quarrel, within the street's reach)";
        }

        private string MindScenesStatus()
        {
            return (_cfg.MindStartsScenes ? "on" : "off ([Mind] MindStartsScenes)") + ": offered in " + _mindScenesOffer + " picks, picked " + _mindScenesPicked + ", started " + _mindScenesStarted +
                   ", not now " + _mindScenesFailed + (_mindSceneLast != null ? "; last: " + _mindSceneLast : "");
        }
    }
}
