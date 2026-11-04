// People talk to each other from what's on their minds, the game side (Lex, 2026-11-03: "have the NPCs use their voicelines,
// having conversations with other NPCs nearby based on their inner thoughts"; [Mind] MindTalks; Core/MindTalk.cs builds the
// talk, Core/Times.cs holds the times). A mindtalk reason (Core/Reasons.cs: the reason of last resort among people who know
// each other, ahead of small talk) comes here from BuildSceneOf with the two of them: what each is to the mind (MindKnown:
// their traits, their needs, their people, how they take the player) is gathered into the talk's context, with what the first
// remembers of the player and a plan they carry, and the Director plays the scene like any other (voiced with VoicedDialogue).
// With [Mind] Language and LanguageTalk, the second one's answer is asked of the model through the talks' channel as the
// scene starts (the answer's meaning is the written line; the one speaking to them is named, Language.Talk.cs) and taken as
// the answer's beat plays (Beat.Late), the written line standing when the model is late or its words don't pass. One ask on
// its way at a time, and only within the subtitles' reach. Testing row "mindtalk"; F7 "TEST mind talks:".
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
        private int _mindTalks, _mindTalkModelAsked, _mindTalkModelSaid, _mindTalkModelHeld, _mindTalkModelLate, _mindTalkAskAt;
        private readonly Dictionary<string, int> _mindTalkTopics = new Dictionary<string, int>();
        private string _mindTalkLast, _mindTalkAskKey;

        /// The scene for a mindtalk reason: what's on the first one's mind, said to the second, who answers from their own nature.
        private Scene MindTalkScene(Ped first, Ped second, Someone doer, Someone done, SceneCast cast, string seed, bool note)
        {
            long g = Bridge.Now(), day = g > 0 ? g / 1440 : -1;
            int now = _now;
            MindWho wa = MindKnown(first.Handle, now), wb = MindKnown(second.Handle, now);
            string wk;
            int wst;
            bool wg;
            var ctx = new MindTalkContext
            {
                Year = InnerLife.ClockYear, Month = InnerLife.ClockMonth, Day = day, Town = _placeTown,
                Rumours = _placeTown != null ? _engine.RumourPool(_placeTown, 3) : null,
                A = MindTalkMindOf(first, doer, wa), B = MindTalkMindOf(second, done, wb),
                BAtWorst = (InnerLife.WorryNow(done.P, day, out wk, out wst, out wg) && wst == 2) || done.Hurt != null
            };
            // what the first remembers of the player, as their thoughts put it, and a plan they carry (Core/Plans.cs)
            try
            {
                if (doer.P != null && doer.P.Memories != null)
                {
                    MemoryItem m = doer.P.Memories.Where(x => x.AboutYou && x.Intensity >= 30).OrderByDescending(x => x.Intensity).FirstOrDefault();
                    PersonState st = m != null ? _store.Find(doer.Id) : null;
                    if (m != null && st != null && _engine.Player != null)
                        ctx.AMemory = Voice.MemoryThought(doer.P, m, Talk.RefFor(doer.P, st, _engine.Player), seed + "|mem");
                }
                if (PlansOn)
                {
                    Plan pl = _store.Plans.FirstOrDefault(p => !p.Done && p.HolderId == doer.Id);
                    if (pl != null)
                    {
                        ctx.APlan = Plans.Thought(pl, seed + "|plan");
                        ctx.APlanKind = pl.Kind;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Detail("MINDTALK the memory or the plan couldn't be read (" + ex.Message + "): without them");
            }
            string fn = cast.Names[0] ?? Person.FirstOf(doer.Name), sn = cast.Names[1] ?? Person.FirstOf(done.Name);
            string topic, manner;
            Scene s = MindTalk.Make(doer, done, fn, sn, ctx, seed, out topic, out manner);
            if (s == null)
                return null;
            if (note)
            {
                _mindTalks++;
                int n;
                _mindTalkTopics.TryGetValue(topic, out n);
                _mindTalkTopics[topic] = n + 1;
                _mindTalkLast = doer.Name + " to " + done.Name + ": " + MindTalk.TopicWords(topic, null) + " (" + manner + ")";
                Log.Write("MINDTALK " + doer.Name + " brings up " + MindTalk.TopicWords(topic, null) + " with " + done.Name + ", who answers " + manner + "ly (" + s.Kind + ")");
                if (_cfg.Language && _cfg.LanguageTalk && _cfg.MindLive && wb != null && wb.P != null && wb.Id == done.Id)
                    MindTalkAskModel(s, wb, second.Handle, done, fn, now);
            }
            return s;
        }

        /// Who they are to the talk beyond Someone: the sides of their nature that decide a stance (from their traits), their
        /// people (as their name was chosen), their most pressing need, and how they take the player.
        private MindTalkMind MindTalkMindOf(Ped p, Someone who, MindWho w)
        {
            var m = new MindTalkMind { WellOff = who.WellOff };
            ResBind rb;
            if (_resBind.TryGetValue(p.Handle, out rb) && rb != null && rb.R != null)
                m.Origin = rb.R.Origin;
            Traits t = null;
            if (w != null)
            {
                if (w.Traits == null && w.Src != null)
                    w.Traits = _engine.Shadow.TraitsOf(w.Src);
                t = w.Traits;
            }
            if (t != null)
            {
                m.Devout = t[Traits.Piety] > 1.2f;
                m.Lawful = t[Traits.Deference] > 0.6f;
                m.Reckless = t[Traits.Risk] > 0.9f;
                m.Compassionate = t[Traits.Compassion] > 0.6f;
                m.Hard = t[Traits.Compassion] < -0.6f || t[Traits.Anger] > 0.9f;
                m.Curious = t[Traits.Curiosity] > 0.6f;
                m.Orderly = t[Traits.Order] > 0.7f;
            }
            if (who.Temper == "Cold" || who.Temper == "Proud")
                m.Hard = true;
            if (w != null && w.Needs != null)
            {
                int top = w.Needs.Top(0.85f);   // hunger, rest, warmth, money, safety, company, standing, meaning, a vice (Needs.Names)
                m.Need = top == 0 ? "hungry" : top == 1 ? "rest" : top == 3 ? "money" : top == 4 ? "safety" : top == 5 ? "lonely" : null;
            }
            string regard = SightRegard(p, who.Id);
            m.StrangerRegard = regard == "fond" ? "warm" : regard;
            return m;
        }

        /// The second one's answer asked of the model as the scene starts; taken as its beat plays. One on its way at a time,
        /// only within the subtitles' reach (the written line does for a talk you can't read).
        private void MindTalkAskModel(Scene s, MindWho wb, int handle, Someone done, string fn, int now)
        {
            Beat open = s.Beats.FirstOrDefault(b => b.Who == 0 && b.Line != null), reply = s.Beats.FirstOrDefault(b => b.Who == 1 && b.Line != null);
            if (open == null || reply == null)
                return;
            if (ENTITY.GET_ENTITY_COORDS(handle, true, true).DistanceTo(Game.Player.Character.Position) > _cfg.ScenesOnTickerMetres + 4f)
                return;
            if (_mindTalkAskKey != null && now - _mindTalkAskAt < 20000 && now >= _mindTalkAskAt)
                return;
            string why;
            LanguageRequest r = MindRequestOf(wb, handle, now, out why);
            if (r == null)
                return;
            r.Kind = "talk";
            r.Speaker = "your neighbour " + fn + ", whom you know";
            r.Said = fn + " says to you: " + open.Line;
            r.Mean = reply.Line;
            if (!r.Near.Contains(fn))
                r.Near.Add(fn);   // the one speaking to them may be named back (Language.Check allows the names given)
            r.Memories.Clear();
            r.Beliefs.Clear();
            r.YouAs = null;
            r.Worry = null;   // the meaning carries what matters (their own trouble crept into every answer, Talk found)
            string key = "mindtalk|" + done.Id + "|" + now, written = reply.Line, bName = done.Name;
            var ask = new LanguageAsk { Key = key, Name = done.Name, PersonId = done.Id, Request = r, Written = written, AskedAtMs = Environment.TickCount };
            if (!_engine.LanguageAnswers.AskPick(ask))
                return;
            if (_langThread == null)
                LanguageStart();
            _mindTalkAskKey = key;
            _mindTalkAskAt = now;
            _mindTalkModelAsked++;
            reply.Late = () =>
            {
                _mindTalkAskKey = null;
                string answer;
                long ms;
                if (!_engine.LanguageAnswers.TakePick(key, out answer, out ms))
                {
                    _mindTalkModelLate++;
                    Log.Detail("MINDTALK model: no answer for " + bName + " in time; the written line");
                    return null;
                }
                string clean = Language.StripAddress(Language.Tidy(answer), fn, bName);   // a name it made up for the speaker, out; the speaker's own kept
                string bad = clean == null ? "empty" : Language.TalkCheck(r, clean, false);
                if (bad == null)
                {
                    _mindTalkModelSaid++;
                    Log.Write("MINDTALK model for " + bName + " in " + ms + " ms: \"" + clean + "\" (written: \"" + written + "\")");
                    return clean;
                }
                _mindTalkModelHeld++;
                Log.Detail("MINDTALK model for " + bName + ": held (" + bad + "): \"" + (clean ?? "") + "\"; the written line");
                return null;
            };
        }

        /// The Testing row: the nearest two named people free near you talk of what's on the nearer one's mind, now.
        private string MindTalkTest(int now, out string said)
        {
            said = null;
            if (!_cfg.MindTalks)
                return "off ([Mind] MindTalks)";
            string why = MindSceneWhyNot(true);
            if (why != null)
                return why;
            Ped me = Game.Player.Character;
            List<Candidate> people = ReasonCandidates(me, now, true).OrderBy(c => c.Ped.Position.DistanceTo(me.Position)).ToList();
            if (people.Count < 2)
                return "fewer than two named people free within " + ReasonRange + " m (" + ReasonRejects(me, now) + ")";
            Candidate a = people[0], b = people.Skip(1).OrderBy(c => c.Ped.Position.DistanceTo(a.Ped.Position)).First();
            float apart = a.Ped.Position.DistanceTo(b.Ped.Position);
            if (apart > ReasonApart)
                return a.Who.Name + " and " + b.Who.Name + " are " + (int)apart + " m apart (" + ReasonApart + " at most)";
            var pr = new PairReason { A = a, B = b, Apart = apart, Why = new Reason { Kind = MindTalk.Kind, Strength = 99, Why = "the Testing row" } };
            if (!PlayPairReason(pr, me, _placeKind == "saloon", now, false, "the Testing row"))
                return "the talk couldn't start (the log says why)";
            said = a.Who.Name + " brings up what's on their mind with " + b.Who.Name + (_mindTalkLast != null ? " (" + _mindTalkLast + ")" : "");
            return null;
        }

        private string MindTalkStatus()
        {
            return (_cfg.MindTalks ? "on" : "off ([Mind] MindTalks)") + "; " + _mindTalks + " talks this session" +
                   (_mindTalkTopics.Count > 0 ? " (" + string.Join(", ", _mindTalkTopics.OrderByDescending(kv => kv.Value).Select(kv => kv.Key + " " + kv.Value)) + ")" : "") +
                   "; the model asked " + _mindTalkModelAsked + ", said " + _mindTalkModelSaid + ", held " + _mindTalkModelHeld + ", late " + _mindTalkModelLate +
                   (_mindTalkLast != null ? "; last: " + _mindTalkLast : "");
        }
    }
}
