// What the re-staged encounters share (Core/Staged.cs; NIGHT4 item 4): the people they hold (kept out of every other
// feature's hands while they do), each person's turn chosen by their mind and, with [Mind] LanguagePicks, put to the
// model among the same options (2.5 s at most; late, empty or unreadable is the mind's own), and the game's own clips
// for the encounter played on the upper body, with the street's standing gestures when a set or a clip doesn't play
// (each set and clip is logged the first time it plays or fails, so a log proves them).
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
        private sealed class StagedAsk
        {
            public string Key, Mind, Picked, Name, Id, Role;
            public List<string> Ids, Words;
            public int Handle, AskedAt, Deadline;
            public LanguageRequest Req;
            public bool Done, ByModel, Asked;
        }

        private sealed class StagedClipCheck
        {
            public int Handle, At;
            public string Dict, Clip, Tone, Seed;
            public bool Woman;
        }

        private readonly HashSet<int> _staged = new HashSet<int>();   // held by a re-staged encounter now: nobody else's to direct
        private readonly Dictionary<string, int> _stagedDictAsked = new Dictionary<string, int>();
        private readonly HashSet<string> _stagedProven = new HashSet<string>(), _stagedMissing = new HashSet<string>();
        private readonly List<StagedClipCheck> _stagedChecks = new List<StagedClipCheck>();
        private int _stagedAsked, _stagedTaken, _stagedLate, _stagedUnread, _stagedMindOwn, _stagedClipsPlayed, _stagedClipsFell;

        private bool IsStaged(int h)
        {
            return _staged.Contains(h);
        }

        /// A person's leanings for an encounter's tables, from who the street knows them as.
        private StagedMind StagedMindOf(Someone who, string otherId)
        {
            var m = new StagedMind { Temper = who.Temper, Mood = who.Mood, Woman = who.Woman, Drunk = who.Drunk, Shamed = who.Shamed };
            if (otherId != null)
            {
                SceneTie t = _store.Ties.FirstOrDefault(x => x.Has(who.Id) && x.Has(otherId));
                m.Warmth = t != null ? t.Warmth : 0;
            }
            PersonState st = _store.Find(who.Id);
            string regard = who.P != null && st != null ? Regard.OfPerson(who.P, st, false) : null;
            m.ToYou = regard == "hostile" ? -2 : regard == "afraid" ? -1 : regard == "fond" || regard == "smitten" ? 2 : 0;
            return m;
        }

        /// The mind's own choice for this turn, and with the model on, the same options asked of it.
        private StagedAsk StagedChoose(int h, string role, Staged.Weights w, string seed, Func<string, string> words, string situation, string otherFirst, int now)
        {
            string mind = Staged.Choose(w, seed);
            var a = new StagedAsk { Handle = h, Role = role, Mind = mind, Picked = mind, AskedAt = now, Deadline = now + PickWaitMs };
            a.Ids = Staged.Offer(w, mind, Language.MostOptions);
            a.Words = a.Ids.Select(words).ToList();
            a.Done = true;
            if (!PicksOn || a.Ids.Count < 2 || !ENTITY.DOES_ENTITY_EXIST(h))
                return a;
            MindWho mw = MindKnown(h, now);
            if (mw == null || mw.Id == null)
                return a;
            string why;
            LanguageRequest r = MindRequestOf(mw, h, now, out why);
            if (r == null)
                return a;
            r.Kind = "pick";
            r.Options = a.Words;
            r.WithThought = true;
            r.Now = situation;
            if (otherFirst != null && !r.Near.Contains(otherFirst))
                r.Near.Add(otherFirst);   // the other in it may be named in the thought
            string key = "enc|" + role + "|" + mw.Id + "|" + Environment.TickCount + "|" + _stagedAsked;
            if (!_engine.LanguageAnswers.AskPick(new LanguageAsk { Key = key, Name = mw.Name, PersonId = mw.Id, Request = r, AskedAtMs = Environment.TickCount }))
                return a;
            _stagedAsked++;
            a.Key = key;
            a.Req = r;
            a.Name = mw.Name;
            a.Id = mw.Id;
            a.Asked = true;
            a.Done = false;
            return a;
        }

        private bool StagedSettle(StagedAsk a, string what, int now)
        {
            return StagedSettle(a, what, null, now);
        }

        /// True once the turn is settled: the model's pick, or the mind's own when it's late or names none of them. The log's
        /// label, "kind, name", is made only once it settles (the frame-cost audit, NIGHT4 item 6: made every 250 ms wait before).
        private bool StagedSettle(StagedAsk a, string kind, string name, int now)
        {
            if (a == null || a.Done)
                return true;
            string answer = null;
            long ms = 0;
            bool came = _engine != null && _engine.LanguageAnswers.TakePick(a.Key, out answer, out ms);
            if (!came && now < a.Deadline && now >= a.AskedAt)
                return false;
            a.Done = true;
            string what = name == null ? kind : kind + ", " + name;
            string offered = string.Join(" | ", a.Words.Select((x, k) => (k + 1) + " " + x));
            if (!came)
            {
                _stagedLate++;
                Log.Write("ENCOUNTER pick " + what + ": " + a.Name + " offered " + offered + " -> no answer in " + PickWaitMs / 1000.0 + " s: the mind's own (" + a.Mind + ")");
                return true;
            }
            string thought;
            string id = Staged.Picked(answer, a.Ids, out thought);
            if (id == null)
            {
                _stagedUnread++;
                Log.Write("ENCOUNTER pick " + what + ": " + a.Name + " offered " + offered + " -> \"" + (answer ?? "").Trim() + "\" names none of them: the mind's own (" + a.Mind + ")");
                return true;
            }
            _stagedTaken++;
            ModelMs(ms);
            a.Picked = id;
            a.ByModel = true;
            if (id == a.Mind)
                _stagedMindOwn++;
            Log.Write("ENCOUNTER pick " + what + ": " + a.Name + " offered " + offered + " -> " + id + (id == a.Mind ? ", the mind's own too" : ", not the mind's (" + a.Mind + ")") + ", " + ms + " ms");
            if (ENTITY.DOES_ENTITY_EXIST(a.Handle))
                PickThought(a.Name, a.Handle, thought, a.Req, a.Id);
            return true;
        }

        private static string By(StagedAsk a)
        {
            return a == null ? "mind" : a.ByModel ? "model" : "mind";
        }

        /// The encounter's clip sets and the street's gestures, asked for at its start.
        private void StagedLoad(IEnumerable<string> dicts, int now)
        {
            foreach (string d in dicts.Concat(Gestures.Dicts))
            {
                STREAMING.REQUEST_ANIM_DICT(d);
                if (!_stagedDictAsked.ContainsKey(d))
                    _stagedDictAsked[d] = now;
            }
        }

        /// The game's own clip on the upper body (8|16, as the street's gestures), or a standing gesture of that tone when
        /// its set hasn't loaded in 3 s. Never on someone in a scenario or a ragdoll.
        private void StagedClip(int h, string dict, string clip, string tone, bool woman, string seed, int now)
        {
            if (!ENTITY.DOES_ENTITY_EXIST(h) || ENTITY.IS_ENTITY_DEAD(h) || PED.IS_PED_USING_ANY_SCENARIO(h) || PED.IS_PED_RAGDOLL(h))
                return;
            if (dict != null && clip != null && !_stagedMissing.Contains(dict + "/" + clip))
            {
                if (STREAMING.HAS_ANIM_DICT_LOADED(dict))
                {
                    TASK.TASK_PLAY_ANIM(h, dict, clip, 4f, -4f, -1, 8 | 16, 0f, false, 0, false, "", false);
                    _stagedClipsPlayed++;
                    if (!_stagedProven.Contains(dict + "/" + clip))
                        _stagedChecks.Add(new StagedClipCheck { Handle = h, At = now + 600, Dict = dict, Clip = clip, Tone = tone, Woman = woman, Seed = seed });
                    return;
                }
                int asked;
                STREAMING.REQUEST_ANIM_DICT(dict);
                if (!_stagedDictAsked.TryGetValue(dict, out asked))
                    _stagedDictAsked[dict] = asked = now;
                if (now - asked > 3000 && _stagedMissing.Add(dict + "/" + clip))
                    Log.Write("ENCOUNTER clips: " + dict + " hasn't loaded in " + (now - asked) / 1000 + " s (" + (STREAMING.DOES_ANIM_DICT_EXIST(dict) ? "a set the game knows" : "a set unknown to the game") + "): the street's gestures instead");
            }
            StagedGesture(h, tone, woman, seed);
        }

        private void StagedGesture(int h, string tone, bool woman, string seed)
        {
            string g;
            string gd = Gestures.For(tone, woman, seed, out g);
            if (STREAMING.HAS_ANIM_DICT_LOADED(gd))
                TASK.TASK_PLAY_ANIM(h, gd, g, 4f, -4f, -1, 8 | 16, 0f, false, 0, false, "", false);
            _stagedClipsFell++;
        }

        /// Each tick: a clip asked for a moment ago is playing (proven, logged once) or isn't (logged once, a gesture now).
        private void StagedChecksStep(int now)
        {
            for (int i = _stagedChecks.Count - 1; i >= 0; i--)
            {
                StagedClipCheck c = _stagedChecks[i];
                if (now < c.At)
                    continue;
                _stagedChecks.RemoveAt(i);
                string k = c.Dict + "/" + c.Clip;
                if (_stagedProven.Contains(k) || _stagedMissing.Contains(k) || !ENTITY.DOES_ENTITY_EXIST(c.Handle))
                    continue;
                if (ENTITY.IS_ENTITY_PLAYING_ANIM(c.Handle, c.Dict, c.Clip, 3))
                {
                    _stagedProven.Add(k);
                    Log.Write("ENCOUNTER clips: " + k + " plays (seen playing; the catalogue can mark it proven)");
                }
                else if (_stagedMissing.Add(k))
                {
                    Log.Write("ENCOUNTER clips: " + k + " didn't play (the set loaded; no such clip, or the body wouldn't take it): the street's gestures instead");
                    if (!PED.IS_PED_RAGDOLL(c.Handle) && !PED.IS_PED_USING_ANY_SCENARIO(c.Handle))
                        StagedGesture(c.Handle, c.Tone, c.Woman, c.Seed);
                }
            }
        }

        /// Held by an encounter: events blocked, its own task kept, and nobody else's to direct.
        private void StagedHold(Ped p)
        {
            _staged.Add(p.Handle);
            PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(p.Handle, true);
            PED.SET_PED_KEEP_TASK(p.Handle, true);
        }

        /// Let go to their own ways: a walk on, the events theirs again (never someone the game has taken since).
        private void StagedLetGo(Ped p, bool wander)
        {
            if (p == null)
                return;
            int h = p.Handle;
            _staged.Remove(h);
            if (!p.Exists() || p.IsDead)
                return;
            if (_director.Involves(h) || _ring.Contains(h) || JoinIsPending(h))
                return;   // a scene, the fight's ring or a join has them now and sets its own block and tasks (NIGHT4 item 6, the review across features)
            PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(h, false);
            PED.SET_PED_KEEP_TASK(h, false);
            if (wander && !AimedLately(h, _now) && !PED.IS_PED_IN_COMBAT(h, 0) && !PED.IS_PED_FLEEING(h) && !PED.IS_PED_RAGDOLL(h))
                TASK.TASK_WANDER_STANDARD(h, 10f, 10);   // never over the game's answer to a gun (AimLetGo's ten seconds)
        }

        private string StagedStatus()
        {
            return "the model " + (PicksOn ? "on" : "off") + ": asked " + _stagedAsked + ", picked " + _stagedTaken + " (" + _stagedMindOwn + " the mind's own too), late " + _stagedLate + ", unreadable " + _stagedUnread +
                   "; clips played " + _stagedClipsPlayed + " (proven " + _stagedProven.Count + "), gestures instead " + _stagedClipsFell + ", missing " + _stagedMissing.Count +
                   (_stagedMissing.Count > 0 ? " (" + string.Join(", ", _stagedMissing.Take(4)) + ")" : "");
        }
    }
}
