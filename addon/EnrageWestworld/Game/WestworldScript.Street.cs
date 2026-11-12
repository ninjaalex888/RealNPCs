// The living street, two slices (SIXDAY item 5b and 5c; roadmap 17). (b) The street reacts to other people's fights: the
// moments of a scene between other people (blows, two men squaring up, a gun out, a man killed or knocked out, the body
// after) are raised as the game's own shocking events (Core/Street.cs names them), so its crowd does what it does for the
// player's fights; the look-at fallback (Onlookers, WestworldScript.Reasons.cs) stays. Three seconds on, the street is
// looked at and the log says what the event did; a guard watches ten seconds for the law in combat, a gun out of someone
// else's holster or people fleeing by the player, and holds the town for thirty game minutes when it sees one. [Scenes]
// StreetReacts. (c) Reputation in the street (Core/Reputation.cs): an outlaw walking among people is looked after, stepped
// around, turned from or stared down by their natures, the doctor, a preacher, the law and a keeper out of doors are
// greeted; one witness mutters it; a known killer or peacemaker gets a thought, never a task. [Scenes] StreetReputation.
// Log STREET; F7 "TEST street:".
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
        private sealed class StreetProbe
        {
            public int At, Handle, EventId, Within;
            public string Type, Name, Kind, Town;
            public bool Looked, Body;                 // the look back done; the body's own event still to raise (a killing)
            public readonly HashSet<int> ArmedBefore = new HashSet<int>(), FleeingBefore = new HashSet<int>();   // as they stood at the event
        }

        private readonly HashSet<string> _streetFired = new HashSet<string>();        // scene|moment: once each
        private readonly List<StreetProbe> _streetProbes = new List<StreetProbe>();
        private readonly Dictionary<string, long> _streetHold = new Dictionary<string, long>();   // town -> the game minute its hold ends
        private readonly Queue<string> _streetLast = new Queue<string>();
        private readonly Dictionary<int, long> _repAt = new Dictionary<int, long>();          // notable handle -> the game minute of their last reaction
        private readonly Dictionary<string, long> _repYouAt = new Dictionary<string, long>();  // witness id -> the game minute of their last thought of you
        private readonly List<Ped> _repReactors = new List<Ped>();
        private int _streetNext, _streetEvents, _streetHeld, _streetEscalations, _repNextAt, _repYouNextAt, _repEvents, _repReacted, _repYou;
        private string _streetFallbackTold, _repLast;
        private Action _aStreet;

        private void StreetStep(int now)
        {
            if (_aStreet == null)
                _aStreet = () => StreetTick(_now);
            Safe("street", _aStreet);
        }

        private void StreetTick(int now)
        {
            if (now < _streetNext)
                return;
            _streetNext = now + 500;
            if (_streetProbes.Count > 0)
                StreetLookBack(now);
            if (now >= _repNextAt)
            {
                _repNextAt = now + 2000;
                StreetRepTick(now);
            }
        }

        // ---- (b) the street reacts ----

        /// From the Director (its Shock hook) and the fight's fates: a moment of a scene between other people, raised as the
        /// game's own shocking event on the one it's about, once per scene per moment.
        private void StreetShock(string moment, int h, string sceneKind, string sceneSeed)
        {
            if (_cfg == null || !_cfg.StreetReacts || h == 0 || !ENTITY.DOES_ENTITY_EXIST(h))
                return;
            int now = Game.GameTime;
            string key = Street.OnceKey(sceneSeed, moment);
            if (_streetFired.Contains(key))
                return;
            if (_streetFired.Count > 400)
                _streetFired.Clear();
            _streetFired.Add(key);
            Ped me = Game.Player.Character;
            string town = _placeTown;
            long g = Bridge.Now(), until;
            bool held = town != null && _streetHold.TryGetValue(town, out until) && g < until;
            Vector3 at = ENTITY.GET_ENTITY_COORDS(h, true, true);
            string why = Street.WhyNot(MISC.GET_MISSION_FLAG(), PlayerWanted(me.Handle), EnrageFighterNear(at, 30f, h), held);
            string name = StreetNameOf(h);
            if (why != null)
            {
                _streetHeld++;
                Log.Detail("STREET event not raised for " + name + " (" + moment + ": " + why + ")");
                return;
            }
            string type = Street.EventFor(moment, _placeKind == "saloon");
            if (type == null)
                return;
            var probe = new StreetProbe { At = now, Handle = h, Type = type, Name = name, Kind = sceneKind, Town = town, Body = moment == "killed" };
            probe.EventId = StreetRaise(type, h);
            Vector3 me3 = me.Position;
            foreach (Ped p in Peds.All())
            {
                int ph = p.Handle;
                if (ph == h || ph == me.Handle || !p.Exists() || ENTITY.IS_ENTITY_DEAD(ph))
                    continue;
                float d = p.Position.DistanceTo(at);
                if (d < Street.Metres)
                    probe.Within++;
                if (p.Position.DistanceTo(me3) < 40f && !_director.Involves(ph))   // as they stood around the player, where the guard looks (the review)
                {
                    if (WEAPON.IS_PED_ARMED(ph, 4))
                        probe.ArmedBefore.Add(ph);
                    if (PED.IS_PED_FLEEING(ph))
                        probe.FleeingBefore.Add(ph);
                }
            }
            _streetProbes.Add(probe);
            _streetEvents++;
            if (_streetFallbackTold == null)
            {
                _streetFallbackTold = type;
                Log.Write("STREET: the first shocking event of the session raised (" + type + " on " + name + "); the Onlookers look-at stays on beside it, whatever the events do");
            }
            Log.Write("STREET event " + type + " on " + name + " (" + sceneKind + ", " + moment + "): " + probe.Within + " within " + (int)Street.Metres + " m (event id " + probe.EventId + ")");
        }

        /// The game's own event, on an entity: its trailing arguments are unnamed in the natives' records, so a length in seconds
        /// goes first and the rest zero; what it does is read back three seconds on.
        private int StreetRaise(string type, int h)
        {
            return EVENT.ADD_SHOCKING_EVENT_FOR_ENTITY(Natives.Hash(type), h, Street.Duration, 0f, 0f, 0f, 0f, 0f, false, false, 0, 0);
        }

        /// Three seconds on: what the street did (the log); then the guard, ten seconds from the event.
        private void StreetLookBack(int now)
        {
            Ped me = Game.Player.Character;
            for (int i = _streetProbes.Count - 1; i >= 0; i--)
            {
                StreetProbe pr = _streetProbes[i];
                bool there = ENTITY.DOES_ENTITY_EXIST(pr.Handle);
                if (!pr.Looked && now - pr.At >= Street.LookBackMs)
                {
                    pr.Looked = true;
                    int looking = 0, fleeing = 0, fighting = 0;
                    if (there)
                    {
                        Vector3 at = ENTITY.GET_ENTITY_COORDS(pr.Handle, true, true);
                        foreach (Ped p in Peds.All())
                        {
                            int ph = p.Handle;
                            if (ph == pr.Handle || ph == me.Handle || !p.Exists() || ENTITY.IS_ENTITY_DEAD(ph) || p.Position.DistanceTo(at) > Street.Metres || _director.Involves(ph))
                                continue;
                            if (PED.IS_PED_HEADTRACKING_ENTITY(ph, pr.Handle))
                                looking++;
                            if (PED.IS_PED_FLEEING(ph))
                                fleeing++;
                            if (PED.IS_PED_IN_COMBAT(ph, 0))
                                fighting++;
                        }
                        if (pr.Body && ENTITY.IS_ENTITY_DEAD(pr.Handle))
                            StreetRaise(Street.EventFor("body", false), pr.Handle);   // the body, now that it lies there
                    }
                    string line = pr.Type + " on " + pr.Name + " (" + pr.Kind + "): " + Street.LookBack(pr.Within, looking, fleeing, fighting);
                    Log.Write("STREET " + line);
                    _streetLast.Enqueue(line);
                    while (_streetLast.Count > Street.Keep)
                        _streetLast.Dequeue();
                }
                if (now - pr.At >= Street.GuardMs || !there)
                {
                    _streetProbes.RemoveAt(i);
                    continue;
                }
                // the guard: the law in combat, a gun out of someone else's holster, people fleeing beside the player
                bool lawCombat = false, gunDrawn = false, fleeNear = false;
                Vector3 me3 = me.Position;
                foreach (Ped p in Peds.All())
                {
                    int ph = p.Handle;
                    if (ph == pr.Handle || ph == me.Handle || !p.Exists() || ENTITY.IS_ENTITY_DEAD(ph) || _director.Involves(ph))
                        continue;
                    float dm = p.Position.DistanceTo(me3);
                    if (dm > 40f)
                        continue;
                    if (!lawCombat && Natives.IsLaw(p) && PED.IS_PED_IN_COMBAT(ph, 0))
                        lawCombat = true;
                    else if (!gunDrawn && dm < 30f && !pr.ArmedBefore.Contains(ph) && WEAPON.IS_PED_ARMED(ph, 4) && !Natives.IsLaw(p))
                        gunDrawn = true;
                    else if (!fleeNear && dm < 15f && !pr.FleeingBefore.Contains(ph) && PED.IS_PED_FLEEING(ph))
                        fleeNear = true;
                }
                string esc = Street.Escalation(lawCombat, gunDrawn, fleeNear);
                if (esc != null)
                {
                    _streetEscalations++;
                    if (pr.Town != null)
                        _streetHold[pr.Town] = Bridge.Now() + Street.HoldMinutes;
                    Log.Write("STREET escalation: " + esc + " within " + (now - pr.At) / 1000 + " s of " + pr.Type + " on " + pr.Name + "; no events in " + (pr.Town ?? "this town") + " for " + Street.HoldMinutes + " game minutes");
                    _streetProbes.RemoveAt(i);
                }
            }
        }

        /// One of enRAGE's fighters within reach of a spot (its own fights are its own to run).
        private bool EnrageFighterNear(Vector3 at, float within, int except)
        {
            if (_bridge == null)
                return false;
            foreach (Ped p in Peds.All())
            {
                int h = p.Handle;
                if (h == except || !p.Exists() || ENTITY.IS_ENTITY_DEAD(h) || p.Position.DistanceTo(at) > within)
                    continue;
                if (_bridge.EnrageFighter(h))
                    return true;
            }
            return false;
        }

        /// Free to be turned by the street: a bystander we may direct, in no scene, act, talk, nod, walk-up or speech of the add-on's
        /// own (SightCant's rules, and the holds the sights don't ask: the review).
        private bool StreetFree(Ped p, Ped me)
        {
            int h = p.Handle;
            return SightCant(p, me) == null && !NotFree(h) && !WalkingUp(h);
        }

        private string StreetNameOf(int h)
        {
            MindWho w = MindKnown(h, Game.GameTime);
            if (w != null && w.Name != null)
                return w.Name;
            Ped p = new Ped(h);
            return p.Exists() ? Director.Kind(p, !PED.IS_PED_MALE(h)) : "someone";
        }

        // ---- (c) reputation in the street ----

        /// Every 2 s in a town: the nearest notable walking among people gets the street's reaction, once an hour each, one
        /// reaction per 20 s; and now and then a witness's thought of the player's own standing here.
        private void StreetRepTick(int now)
        {
            if (_cfg == null || !_cfg.StreetReputation || _placeTown == null || _engine == null || MISC.GET_MISSION_FLAG())
                return;
            Ped me = Game.Player.Character;
            if (PlayerWanted(me.Handle))
                return;
            long g = Bridge.Now();
            if (now >= _repYouNextAt)
            {
                _repYouNextAt = now + Reputation.YouGapMs;
                StreetYou(me, now, g);
            }
            if (_repAt.Count > 200)
                _repAt.Clear();
            Ped notable = null;
            string kind = null;
            float best = Reputation.NotableMetres;
            foreach (Ped p in Peds.All())
            {
                int h = p.Handle;
                if (h == me.Handle || !p.Exists() || ENTITY.IS_ENTITY_DEAD(h) || PED.IS_PED_IN_ANY_VEHICLE(h, false))
                    continue;
                float d = p.Position.DistanceTo(me.Position);
                if (d >= best || ENTITY.GET_ENTITY_SPEED(h) < 0.5f || PED.IS_PED_IN_COMBAT(h, 0) || _director.Involves(h) || !ENTITY.IS_ENTITY_ON_SCREEN(h))
                    continue;
                long last;
                if (_repAt.TryGetValue(h, out last) && g - last < Reputation.EachMinutes)
                    continue;
                string k = NotableKind(p, now);
                if (k == null)
                    continue;
                notable = p;
                kind = k;
                best = d;
            }
            if (notable == null)
                return;
            _repAt[notable.Handle] = g;
            _repNextAt = now + Reputation.GapMs;
            StreetReact(me, notable, kind, now, g);
        }

        /// Who counts: the law (never an outlaw), enRAGE's gunmen, a resident whose trouble is the wanted poster, the doctor, a
        /// preacher, a counter keeper out of doors. Null for everyone else.
        private string NotableKind(Ped p, int now)
        {
            int h = p.Handle;
            if (Natives.IsLaw(p))
                return "law";
            // the respected first: the counter keepers and the preachers are the game's own u_ models, which the story-model rule below
            // would turn away (the review: the doctor never counted)
            PostKind post = PostKindOf(p);
            if (post != null)
            {
                if (Natives.InteriorType(h) != 0)
                    return null;   // at their counter, not out in the street
                return post.Kind == "doctor" ? "doctor" : "keeper";
            }
            string model = Models.Name(p);
            if (model != null && (model.IndexOf("preacher", StringComparison.OrdinalIgnoreCase) >= 0 || model.IndexOf("priest", StringComparison.OrdinalIgnoreCase) >= 0))
                return "preacher";
            if (Natives.StoryModel(p))
                return null;
            if (_bridge != null && _bridge.EnrageFighter(h))
                return "outlaw";
            MindWho w = MindKnown(h, now);
            if (w != null && w.P != null)
            {
                string worry;
                int stage;
                bool good;
                if (InnerLife.WorryNow(w.P, _store.LastGameMin / 1440, out worry, out stage, out good) && worry == "law" && stage <= 2)
                    return "outlaw";
            }
            return null;
        }

        /// The street's reaction to one notable: up to three free passers-by who can see him, each by their nature; one of them
        /// mutters it. The respected get a greeting instead (a hat tipped, a nod, a word in their own voice).
        private void StreetReact(Ped me, Ped notable, string kind, int now, long g)
        {
            int h = notable.Handle;
            _repReactors.Clear();
            Vector3 at = notable.Position;
            foreach (Ped p in Peds.All())
            {
                if (_repReactors.Count >= Reputation.MostReactors)
                    break;
                int ph = p.Handle;
                if (ph == h || p.Position.DistanceTo(at) > Reputation.ReactMetres || PED.IS_PED_IN_ANY_VEHICLE(ph, false) || Natives.IsLaw(p) || !StreetFree(p, me) ||
                    !ENTITY.HAS_ENTITY_CLEAR_LOS_TO_ENTITY(ph, h, 17))
                    continue;
                _repReactors.Add(p);
            }
            MindWho known = MindKnown(h, now);
            string name = known != null && known.Name != null ? known.Name : StreetNameOf(h);
            if (_repReactors.Count == 0)
            {
                Log.Detail("STREET rep: " + name + " (" + kind + ") walks by with nobody free to mind him");
                return;
            }
            int looks = 0, steps = 0, turns = 0, stares = 0, greets = 0;
            string mutterer = null;
            bool respected = Reputation.IsRespected(kind);
            string surname = respected && kind == "keeper" && known != null && known.Name != null && known.Name.IndexOf(' ') > 0 ? known.Name.Substring(known.Name.LastIndexOf(' ') + 1) : null;   // the keeper by his name
            int clockHour = CLOCK.GET_CLOCK_HOURS();
            foreach (Ped r in _repReactors)
            {
                int rh = r.Handle;
                MindWho w = MindKnown(rh, now);
                string temper = w != null && w.P != null ? w.P.Personality : Sights.StrangerTemper("rep|" + rh);
                bool woman = !PED.IS_PED_MALE(rh);
                string seed = "rep|" + h + "|" + rh + "|" + now / 1000;
                if (respected)
                {
                    string gesture = Reputation.ToRespected(woman, seed);
                    TASK.TASK_LOOK_AT_ENTITY(rh, h, 3000, 0, 51, 0);
                    NodGesture(r, notable, gesture, woman, seed, w != null && w.Name != null ? w.Name : "a passer-by");
                    if (Reputation.Speaks(seed))
                        NodSay(r, w != null && w.Name != null ? w.Name : "a passer-by", notable, Speech.Greet, Reputation.Greeting(kind, surname, clockHour, seed), "greets the " + kind);
                    greets++;
                }
                else
                {
                    bool old = Director.OldModel(r);   // the game's old builds (_m_o_, _f_o_; the review)
                    string act = Reputation.ToOutlaw(temper, old, PED.IS_PED_USING_ANY_SCENARIO(rh), seed);
                    Vector3 away = r.Position - at;
                    away.Z = 0f;
                    float len = (float)Math.Sqrt(away.X * away.X + away.Y * away.Y);
                    if (len > 0.01f)
                        away = away * (1f / len);
                    switch (act)
                    {
                        case "stare":
                            TASK.TASK_LOOK_AT_ENTITY(rh, h, 6000, 0, 51, 0);
                            stares++;
                            break;
                        case "turnaway":
                            TASK.TASK_TURN_PED_TO_FACE_COORD(rh, r.Position + away * 3f, 2500);
                            turns++;
                            break;
                        case "stepoff":
                            TASK.TASK_LOOK_AT_ENTITY(rh, h, 4000, 0, 51, 0);
                            TASK.TASK_GO_STRAIGHT_TO_COORD(rh, r.Position + away * 2.5f, 1.0f, 4000, ENTITY.GET_ENTITY_HEADING(rh), 0.5f, 0);
                            WanderLater(rh, now + 4500);   // handed back to their day after the step
                            steps++;
                            break;
                        default:
                            TASK.TASK_LOOK_AT_ENTITY(rh, h, 4000, 0, 51, 0);
                            looks++;
                            break;
                    }
                }
                if (mutterer == null && w != null && w.Name != null && NodInView(me, r))
                {
                    string line = Reputation.Mutter(kind, temper, seed);
                    if (line != null)
                    {
                        mutterer = w.Name;
                        _ticker.Push(w.Name + ", thinking", line, "near", rh);
                    }
                }
                _repReacted++;
            }
            _repEvents++;
            _repLast = name + " (" + kind + ")";
            Log.Write("STREET rep: " + name + " (" + kind + ") -> " + _repReactors.Count + " react" + (respected ? " (" + greets + " greet)" : " (" + looks + " look, " + steps + " step off, " + turns + " turn away, " + stares + " stare)") +
                      (mutterer != null ? "; " + mutterer + " mutters" : ""));
            _repReactors.Clear();
        }

        /// The player, lightly: a witness near who knows of him thinks of his standing here (a known killer, a known
        /// peacemaker), once an hour each; never a task.
        private void StreetYou(Ped me, int now, long g)
        {
            string town = _placeTown;
            long since = _store.LastGameMin - Reputation.StandingDays * 1440;
            int kills = 0, kind = 0;
            foreach (Deed d in _store.Deeds)
                if (d.Known && d.Town == town && d.At >= since && (d.Kind == "kill" || d.Kind == "murder" || d.Kind == "lawkill"))
                    kills += Math.Max(1, d.Count);
            foreach (Happening e in _store.WorldLog)
                if (e.DoerId == "player" && !e.Test && e.Town == town && e.At >= since && Fondness.IsKindness(e.Kind))
                    kind++;
            kind += Reputation.GoodDeedsKnown(_store.Deeds, town, since);   // (SEAMS item 19)
            string standing = Reputation.Standing(kills, kind);
            if (standing == null)
                return;
            if (_repYouAt.Count > 300)
                _repYouAt.Clear();
            foreach (Ped p in Peds.All())
            {
                int h = p.Handle;
                if (h == me.Handle || !p.Exists() || ENTITY.IS_ENTITY_DEAD(h) || p.Position.DistanceTo(me.Position) > 12f || _director.Involves(h) || !NodInView(me, p))
                    continue;
                MindWho w = MindKnown(h, now);
                if (w == null || w.Id == null || w.Name == null)
                    continue;
                long last;
                if (_repYouAt.TryGetValue(w.Id, out last) && g - last < Reputation.YouEachMinutes)
                    continue;
                string line = Reputation.YouThought(standing, w.Id + "|" + g / 60);
                if (line == null)
                    return;
                _repYouAt[w.Id] = g;
                _repYou++;
                _ticker.Push(w.Name + ", thinking", line, "near", h);
                Log.Write("STREET rep: you (" + standing + " here: " + kills + " killings known, " + kind + " kindnesses) -> " + w.Name + " thinks of it");
                return;
            }
        }

        private string StreetStatus()
        {
            string reacts = !_cfg.StreetReacts ? "reacts off" : "reacts on: " + _streetEvents + " events raised, " + _streetHeld + " not raised, " + _streetEscalations + " escalations" +
                            (_streetLast.Count > 0 ? "; last: " + string.Join(" | ", _streetLast.ToArray()) : "");
            string rep = !_cfg.StreetReputation ? "reputation off" : "reputation on: " + _repEvents + " notables minded (" + _repReacted + " reactions), " + _repYou + " thoughts of you" + (_repLast != null ? "; last: " + _repLast : "");
            return reacts + "; " + rep;
        }
    }
}
