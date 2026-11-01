// Passing the player, from the mind, the game side (night 2026-10-29, item 6f; Core/Passing.ToYou.cs; with [Mind] MindLive
// and [Mind] MindPassing). Every 1.5 s at most one person near the player whom the mind knows (the same body: its model
// checked), who feels something about him from a while ago, facing him within 9 m, in his view and free (the street
// reactions' own check, SightCant), shows it, spaced as the passing nods are: the look first, the gesture 450 ms on, the
// thought at 1.6 s; a spoken greeting (the familiar set) when grateful, at most one each game day. Busy for every other feature while
// it plays (IsAcknowledging). Once a person in five minutes. The player is never tasked. Log (detail) NOD you:.
// NIGHT4 item 5 ([Mind] MindPassingLasts, on): the feeling read by the mood's slower fading, from 12 m, traits filled when
// missing (they were skipped), and every person passed over counted by why (F7 TEST nods you:; a NOD you minute: line).
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
        private sealed class YouNod
        {
            public int Handle, At;
            public string Id, Name, Gesture, Thought;
            public bool Woman, Speaks, GestureDone, ThoughtDone;
        }

        private const float YouNodMetres = 9f, YouNodMetresLasting = 12f;
        private float YouNodReach { get { return _cfg != null && _cfg.MindPassingLasts ? YouNodMetresLasting : YouNodMetres; } }
        private const int YouNodEveryMs = 1500, YouNodAgainMs = 300000, YouNodGestureMs = 450, YouNodThoughtMs = 1600, YouNodBusyMs = 3500;
        private static readonly string[] YouGreet = { "GREET_GENERAL_FAMILIAR", "HOWS_IT_GOING" };
        private readonly Dictionary<string, int> _youNodAt = new Dictionary<string, int>();
        private readonly Dictionary<string, long> _youSpokeDay = new Dictionary<string, long>();
        private YouNod _youNod;

        /// With the shadow's (a load, a new engine): a greeting pending on a handle the game may give to someone else, and the
        /// day's and the five minutes' marks (the branch read, night 2026-10-29).
        private void YouNodForget()
        {
            _youNod = null;
            _youNodAt.Clear();
            _youSpokeDay.Clear();
        }
        private int _youNodNext, _youNods;
        private string _youNodLast;
        // why people near were passed over (plain counts, no allocation): no identity, no traits, the five minutes, no
        // feeling about you, busy, a handle changed, too far, not facing, not in view, the sight's own refusal
        private int _ynNoId, _ynNoTraits, _ynCooldown, _ynNoFeeling, _ynBusy, _ynHandle, _ynFar, _ynFacing, _ynView, _ynSight;
        private int _ynGrateful, _ynSore, _ynSullen, _ynAfraid, _ynShamed, _ynMinuteAt, _ynMinuteNods, _ynMinuteSum;

        /// Someone acknowledging the player now (busy for everything else).
        private bool IsAcknowledgingYou(int h)
        {
            return _youNod != null && _youNod.Handle == h;
        }

        /// From the mind's tick: the one acknowledging the player plays on; else, at most every 1.5 s, the next.
        private void YouNodStep(int now)
        {
            if (_cfg == null || !_cfg.MindLive || !_cfg.MindPassing)
            {
                _youNod = null;
                return;
            }
            try
            {
                Ped me = Game.Player.Character;
                if (_youNod != null)
                {
                    YouNodPlay(me, now);
                    return;
                }
                if (now < _youNodNext || me == null || !me.Exists())
                    return;
                _youNodNext = now + YouNodEveryMs;
                YouNodMinute(now);
                YouNodPass(me, now, false);
            }
            catch (Exception ex)
            {
                Log.Write("NOD you failed: " + ex.Message);
                _youNod = null;
                _youNodNext = now + 60000;
            }
        }

        /// One pass over the people near: the first who feels something about him and can show it starts. Null if one did,
        /// else the commonest reason nobody did. `test` (the Testing row): the five minutes and the facing skipped.
        /// "id|hour", made once an hour a person (the frame-cost audit, NIGHT4 item 6: a new string for everyone near every 1.5 s).
        private static string YouNodSeed(MindWho w, long g)
        {
            long hour = g / 60;
            if (w.NodSeed == null || w.NodSeedHour != hour)
            {
                w.NodSeed = w.Id + "|" + hour;
                w.NodSeedHour = hour;
            }
            return w.NodSeed;
        }

        private readonly List<string> _youNodGone = new List<string>();

        /// Only the cooldowns that have run out go. Its own method, so the pass captures nothing (a lambda over `now` made a
        /// closure on every pass).
        private void YouNodPrune(int now)
        {
            _youNodGone.Clear();
            foreach (KeyValuePair<string, int> kv in _youNodAt)
                if (now - kv.Value >= YouNodAgainMs || now < kv.Value)
                    _youNodGone.Add(kv.Key);
            for (int i = 0; i < _youNodGone.Count; i++)
                _youNodAt.Remove(_youNodGone[i]);
        }

        private string YouNodPass(Ped me, int now, bool test)
        {
            {
                long g = _store.LastGameMin;
                Vector3 mePos = me.Position;
                bool lasts = _cfg.MindPassingLasts;
                float reach = YouNodReach;
                int noId = 0, noTraits = 0, cool = 0, noFeel = 0, busy = 0, handle = 0, far = 0, facing = 0, view = 0, sight = 0;
                foreach (int h in _mindNear)
                {
                    // the pure tests first, the natives after: the same people pass (the cost audit, night 2026-10-29: about 120
                    // natives every 1.5 s in a Saint Denis crowd, for the few with a feeling about you)
                    MindWho w;
                    if (!_mindWho.TryGetValue(h, out w) || w.Id == null || w.P == null)
                    {
                        noId++;
                        continue;
                    }
                    if (w.Traits == null && !lasts)
                    {
                        noTraits++;
                        continue;   // as before the switch: skipped until something else has worked them out
                    }
                    int at;
                    if (!test && _youNodAt.TryGetValue(w.Id, out at) && now - at >= 0 && now - at < YouNodAgainMs)
                    {
                        cool++;
                        continue;
                    }
                    long day;
                    bool spoke = _youSpokeDay.TryGetValue(w.Id, out day) && day == g / 1440;
                    Passing.Act a = Passing.ToYou(_engine.Shadow.Felt(w.Id), w.P.Woman, w.Src != null ? w.Src.Temper : null, g, YouNodSeed(w, g), spoke,
                                                  _engine.KnowsYourName(w.Id) ? YourSurname() : null, lasts);   // the cheap test first (his name, if they know it)
                    if (a == null)
                    {
                        noFeel++;
                        continue;
                    }
                    if (w.Traits == null && w.Src != null)
                        w.Traits = _engine.Shadow.TraitsOf(w.Src);   // worked out only for one who feels something (the frame-cost review: not a crowd at once)
                    if (w.Traits == null)
                    {
                        noTraits++;
                        continue;
                    }
                    if (_director.Involves(h) || _director.Leaving(h) || IsOnlooking(h) || IsRescuer(h))
                    {
                        busy++;
                        continue;
                    }
                    if (!ENTITY.DOES_ENTITY_EXIST(h) || ENTITY.IS_ENTITY_DEAD(h) || w.Model != ENTITY.GET_ENTITY_MODEL(h))
                    {
                        handle++;
                        continue;   // a handle the game gave to someone else since (the review of 6f)
                    }
                    Ped p = new Ped(h);
                    if (p.Position.DistanceTo(mePos) > reach)
                    {
                        far++;
                        continue;
                    }
                    if (!test && !Facing(p, mePos, 70f))
                    {
                        facing++;
                        continue;
                    }
                    if (!NodInView(me, p))
                    {
                        view++;
                        continue;
                    }
                    if (SightCant(p, me) != null)
                    {
                        sight++;
                        continue;
                    }
                    if (_youNodAt.Count > 500)
                        YouNodPrune(now);
                    _youNodAt[w.Id] = now;
                    if (a.Speaks)
                    {
                        if (_youSpokeDay.Count > 500)
                            _youSpokeDay.Clear();
                        _youSpokeDay[w.Id] = g / 1440;
                    }
                    if (a.LookMs > 0)
                        TASK.TASK_LOOK_AT_ENTITY(h, me.Handle, a.LookMs, 0, 51, 0);
                    _youNod = new YouNod { Handle = h, At = now, Id = w.Id, Name = w.Name, Gesture = a.Gesture, Thought = a.Thought, Woman = w.P.Woman, Speaks = a.Speaks };
                    _youNods++;
                    _ynMinuteNods++;
                    string felt = a.Kind.Substring(4);
                    if (felt == "gratitude") _ynGrateful++;
                    else if (felt == "anger" || felt == "contempt") _ynSore++;
                    else if (felt == "sullen") _ynSullen++;
                    else if (felt == "fear") _ynAfraid++;
                    else if (felt == "shame") _ynShamed++;
                    _youNodLast = w.Name + " (" + felt + ")" + (test ? ", a test" : "");
                    Log.Detail("NOD you: " + w.Name + " " + felt + " about you: " + a.Gesture + (a.Speaks ? " and a greeting" : "") + ", looks " + a.LookMs + " ms" + (test ? " (a test)" : ""));
                    return null;   // one a look
                }
                _ynNoId += noId; _ynNoTraits += noTraits; _ynCooldown += cool; _ynNoFeeling += noFeel; _ynBusy += busy; _ynHandle += handle;
                _ynFar += far; _ynFacing += facing; _ynView += view; _ynSight += sight;
                if (!test)
                    return null;   // only the Testing row reads the reason
                int top = Math.Max(Math.Max(Math.Max(noFeel, far), Math.Max(facing, view)), Math.Max(Math.Max(busy, cool), Math.Max(Math.Max(noTraits, sight), Math.Max(handle, noId))));
                if (top == 0)
                    return "nobody the mind knows is near";
                return top == noFeel ? "nobody near feels anything about you that shows (" + noFeel + ")" : top == far ? "too far (" + far + ", past " + (int)reach + " m)"
                     : top == facing ? "not facing you (" + facing + ")" : top == view ? "not in your view (" + view + ")" : top == busy ? "busy in a scene or watching one (" + busy + ")"
                     : top == cool ? "showed you already in the last five minutes (" + cool + ")" : top == noTraits ? "no traits worked out (" + noTraits + ")"
                     : top == sight ? "can't see you (" + sight + ")" : top == handle ? "the body changed hands (" + handle + ")" : "nobody the mind knows (" + noId + ")";
            }
        }

        /// Once a real minute while the step runs, the counts as a Write-level line, so a log shows which gate dominates.
        private void YouNodMinute(int now)
        {
            if (_ynMinuteAt == 0 || now < _ynMinuteAt)
            {
                _ynMinuteAt = now;
                return;
            }
            if (now - _ynMinuteAt < 60000)
                return;
            _ynMinuteAt = now;
            int sum = _youNods + _ynNoFeeling + _ynFar + _ynFacing + _ynView + _ynBusy + _ynCooldown + _ynNoTraits + _ynSight + _ynHandle + _ynNoId;
            if (sum == _ynMinuteSum)
                return;   // nobody looked at this minute (no one near): no line
            _ynMinuteSum = sum;
            Log.Write("NOD you minute: " + _ynMinuteNods + " shown; " + YouNodPassedOver());
            _ynMinuteNods = 0;
        }

        private string YouNodPassedOver()
        {
            return "passed over: no feeling " + _ynNoFeeling + ", too far " + _ynFar + ", not facing " + _ynFacing + ", not in view " + _ynView + ", busy " + _ynBusy +
                   ", cooldown " + _ynCooldown + ", no traits " + _ynNoTraits + ", can't see " + _ynSight + ", handle changed " + _ynHandle + ", no identity " + _ynNoId;
        }

        /// F7: this session's, by feeling, the last, and who was passed over and why (counts since the session began).
        private string YouNodStatus()
        {
            if (!_cfg.MindLive || !_cfg.MindPassing)
                return "off ([Mind] MindLive and MindPassing)";
            return _youNods + " this session (gratitude " + _ynGrateful + ", sore " + _ynSore + ", sullen " + _ynSullen + ", fear " + _ynAfraid + ", shame " + _ynShamed + ")" +
                   (_cfg.MindPassingLasts ? ", lasting feelings, " + (int)YouNodMetresLasting + " m" : ", quick feelings, " + (int)YouNodMetres + " m") +
                   "; last: " + (_youNodLast ?? "none") + "; " + YouNodPassedOver();
        }

        /// The Testing row: one pass now, the five minutes and the facing skipped.
        private string YouNodTest(int now)
        {
            if (!_cfg.MindLive || !_cfg.MindPassing)
                return "[Mind] MindLive and MindPassing must be on";
            if (_youNod != null)
                return "someone is showing you already";
            Ped me = Game.Player.Character;
            if (me == null || !me.Exists() || _store == null)
                return "not loaded";
            return YouNodPass(me, now, true);
        }

        /// The acknowledgement, spaced: the gesture (and a greeting) a moment after the look, the thought after that.
        private void YouNodPlay(Ped me, int now)
        {
            YouNod n = _youNod;
            if (!ENTITY.DOES_ENTITY_EXIST(n.Handle) || ENTITY.IS_ENTITY_DEAD(n.Handle) || now - n.At > YouNodBusyMs || now < n.At)
            {
                _youNod = null;
                return;
            }
            Ped p = new Ped(n.Handle);
            if (!n.GestureDone && now - n.At >= YouNodGestureMs)
            {
                n.GestureDone = true;
                if (MayDirectWhyNot(p) == null)
                {
                    NodGesture(p, me, n.Gesture, n.Woman, n.Id + "|you", n.Name);
                    if (n.Speaks)
                        Speech.SayAny(n.Handle, YouGreet, me.Handle);
                }
            }
            if (!n.ThoughtDone && now - n.At >= YouNodThoughtMs)
            {
                n.ThoughtDone = true;
                NodThought(p, n.Name, n.Thought);
            }
        }
    }
}
