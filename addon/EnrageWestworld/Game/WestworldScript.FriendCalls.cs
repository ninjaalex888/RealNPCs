// Friends come to you, the game side (Core/FriendCalls.cs has the choosing and the words; Run 3, item 7). Every
// FriendCalls.LookMs (50 s, x the storyteller's) in a town, with [Scenes] FriendCalls, never while another walk-up is going,
// a street scene is on, in a mission, or while you're wanted, fighting or aiming: a friend near (AllyWarmth 2 or more: the
// friend dot's rule), free, standing, in view within 25 m, and not held back by Pacing "friendcall" (six game hours between
// any two, a friend once in two days) walks over the late thanks' way (a walk timed by the distance, the line at 2.6 m,
// facing you), says what they came with, hands over what there is ([Scenes] FriendGifts: a few cents, cartridges for the gun
// you carry, a meal or a drink into your cores, the doctor's patching), thinks a thought of their own and goes on. Testing
// row "A friend comes over" (TestFriendCallKey): the nearest friend within 20 m at once (no waits, no roll, no pace; what
// they'd hand over is said, not given).
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
        private sealed class FriendWalk
        {
            public Ped Ped;
            public string Id, Name, Kind, Detail, Line, After, Mark;   // Mark: the fight talked over, kept once it's said (FriendCalls.TalkedMark)
            public int Until, SaidAt, Cents;
            public bool Test;
        }

        private FriendWalk _friendWalk;
        private int _nextFriendCall, _friendStepAt, _friendCalls, _friendGivenCents, _friendGifts;
        private string _friendLast, _friendWhyNot;
        private readonly Dictionary<string, int> _friendKinds = new Dictionary<string, int>();
        private Action _aFriends;
        private const uint FriendGiveReason = 0x2CD419DC;   // the game's default reason for a weapon or ammo given (as WakeActs' GiveReason)

        private void FriendCallsStep(int now)
        {
            if (!_cfg.FriendCalls)
            {
                if (_friendWalk != null)
                    FriendAbort();   // switched off mid-walk: let them go
                return;
            }
            if (_store == null || _engine == null || (_friendWalk == null && now < _nextFriendCall))
                return;
            if (_aFriends == null)
                _aFriends = () =>
                {
                    Ped me = Game.Player.Character;
                    if (me == null || !me.Exists() || me.IsDead)
                        return;
                    if (_friendWalk != null)
                    {
                        if (_now < _friendStepAt)
                            return;
                        _friendStepAt = _now + 250;
                        FriendWalkStep(me, _now);
                    }
                    else if (_now >= _nextFriendCall)
                        FriendLook(me, _now, false);
                };
            Safe("friends come", _aFriends);
        }

        /// A friend near comes over with something. Null if one set off, else why not.
        private string FriendLook(Ped me, int now, bool test)
        {
            _nextFriendCall = now + (int)(FriendCalls.LookMs * Story.WalkLook);
            if (WalkUpGoing())
                return _friendWhyNot = "someone is already coming up to you, or you're in the Talk menu";
            if (!test && (_placeTown == null || _director.Busy))
                return _friendWhyNot = _placeTown == null ? "out of the towns" : "a scene is going on";
            if (MISC.GET_MISSION_FLAG() || PLAYER.GET_PLAYER_WANTED_LEVEL(Game.Player.Handle) > 0 || PED.IS_PED_IN_COMBAT(me.Handle, 0) || PLAYER.IS_PLAYER_FREE_AIMING(Game.Player.Handle))
                return _friendWhyNot = "a mission, or you're wanted, fighting or aiming";
            long g = _store.LastGameMin;
            string skipped;
            string pre = Pacing.WhyNot(_store.Pace, "friendcall", _placeTown, null, g, _rng.Next(100), test, out skipped);
            if (pre != null)
                return _friendWhyNot = "rests: " + pre;
            float reach = test ? 20f : FriendCalls.Metres;
            foreach (Ped p in Peds.All().Where(x => x != null && x.Exists() && !x.IsDead && x.Handle != me.Handle && x.Position.DistanceTo(me.Position) < reach)
                                        .OrderBy(x => x.Position.DistanceTo(me.Position)))
            {
                int h = p.Handle;
                if (!PED.IS_PED_HUMAN(h) || MayDirectWhyNot(p) != null || NotFree(h) || _director.Involves(h) || !NodInView(me, p) ||
                    PED.IS_PED_USING_ANY_SCENARIO(h) || AimedLately(h, now))
                    continue;   // standing, not sat, leaning or at work, and not just aimed at (the review of items 5-7, as the lonely and the favours)
                MindWho w = MindKnown(h, now);
                if (w == null || w.Id == null || !_engine.CanRemember(w.Id) || (_secondTestId != null && w.Id != _secondTestId))
                    continue;
                if (w.Id != _secondTestId && AllyWarmth(p, w) < 2)
                    continue;   // a friend by the friend dot's rule only (the second acts' Testing row's one aside)
                if (Pacing.WhyNot(_store.Pace, "friendcall", _placeTown, w.Id, g, 0, test, out skipped) != null)
                    continue;
                Person who = w.P ?? _engine.Find(w.Id);
                string trade = who != null ? who.Trade : null, group = InnerLife.TradeGroup(trade);
                bool woman = !PED.IS_PED_MALE(h);
                if (woman && (Natives.Night() || _engine.NowIndoors))
                    continue;   // the period's rule the late thanks keep: a woman doesn't walk up to a man after dark or indoors
                long day = g / 1440;
                string seed = w.Id + "|" + day;
                // what there is to bring: a danger to warn of, the news, a favour of their trade, a gift
                string danger = null, warnKind = FriendDanger(w, g, out danger);
                string news = FriendNews(seed);
                string favour = FriendCalls.Favour(trade, group);
                bool gun = FriendGun(me) != 0;
                if (favour == "rounds" && !gun)
                    favour = null;   // no gun to load: the gunsmith's favour waits, and they may come with something else (a day's seed kept them away: the review)
                string fallen = null;
                MindMemory foughtMem = null;
                if (_cfg.AlliesRemember)
                    foughtMem = FriendFought(w, g, out fallen);
                if (foughtMem == null)
                    fallen = null;
                MindMemory savedMem = null;
                if (SecondActs.On)
                {
                    PersonState sst = _store.Find(w.Id);
                    savedMem = sst != null ? SecondActs.SavedMemory(sst.Mind, g, sst.Told) : null;   // one you saved, not yet thanked for it (FOURTEENHOUR reserve R3)
                }
                string kind = FriendCalls.Which(warnKind != null, news != null, favour, _cfg.FriendGifts, seed, foughtMem != null, savedMem != null);
                if (kind == null)
                    continue;
                string detail = kind == FriendCalls.Warn ? (warnKind == "law" ? "law" : warnKind == "pinkerton" ? "pinkerton" : warnKind == "thief" ? "thief" : warnKind == FriendCalls.ThiefYours ? FriendCalls.ThiefYours : danger != null ? "named" : "nameless")
                              : kind == FriendCalls.Gift ? FriendCalls.GiftOf(group, woman, seed)
                              : kind == SecondActs.Saved ? (_cfg.FriendGifts ? favour ?? FriendCalls.GiftOf(group, woman, seed) : null)   // the token: a favour of their trade, else a gift
                              : kind == FriendCalls.Trade ? favour
                              : kind == FriendCalls.Fought ? (fallen != null ? "fell" : "won") : null;
                if (detail == "rounds" && !gun)
                    detail = "coin";   // cartridges with no gun to load: a coin instead
                if (kind == FriendCalls.Trade && detail == null)
                    continue;
                string you = _engine.KnowsYourName(w.Id) ? YourSurname() : null;
                string line = kind == SecondActs.Saved ? SecondActs.Line(SecondActs.HowOf(savedMem.Key), detail, you, seed)
                                                       : FriendCalls.Line(kind, detail, you, kind == FriendCalls.Fought ? fallen : danger, news, _placeTown, seed);
                if (kind == FriendCalls.Warn && warnKind == "pinkerton")
                {
                    line = Pinkertons.Warning(you, danger, seed);   // who talked, or that nobody did
                    PinkVisit pv = PinkToWarn(_placeTown, g);
                    if (pv != null && !test)
                        pv.Warned = true;
                }
                if (line == null)
                    continue;
                if (!test)
                {
                    Pacing.Note(_store.Pace, "friendcall", _placeTown, w.Id, g);
                    StoryNoticed("you:friend", 2, p.Position.DistanceTo(me.Position));   // the storyteller: a friend's visit is something near you
                }
                float metres = p.Position.DistanceTo(me.Position);
                int walkMs = Math.Min(25000, 4000 + (int)(900 * metres));   // a walk timed by the distance (the late thanks')
                TASK.TASK_GO_TO_ENTITY(h, me.Handle, walkMs, 2.0f, 1.0f, 0f, 0);
                TASK.TASK_LOOK_AT_ENTITY(h, me.Handle, walkMs, 0, 51, 0);
                _friendWalk = new FriendWalk
                {
                    Ped = p, Id = w.Id, Name = w.Name ?? TalkName(p), Kind = kind, Detail = detail, Line = line, After = FriendCalls.After(kind, seed), Mark = kind == FriendCalls.Fought && !test ? FriendCalls.TalkedMark(foughtMem) : kind == SecondActs.Saved && !test ? SecondActs.ThankedMark(savedMem) : null,
                    Cents = (kind == FriendCalls.Gift || kind == SecondActs.Saved) && detail == "coin" ? FriendCalls.CentsOf(group, seed) : 0, Until = now + walkMs, Test = test
                };
                Log.Write("FRIEND " + _friendWalk.Name + " (" + (trade ?? "someone") + ") comes over with " + FriendWhat(kind, detail, _friendWalk.Cents) + " (" + (int)metres + " m)" +
                          (test ? ", a test" + (skipped != null ? " (skipped: " + skipped + ")" : "") : ""));
                return null;
            }
            return _friendWhyNot = "no friend of yours is near, free and can see you";
        }

        /// A fight this friend stood in with you (the allies' "ally:" memory, Sub "defend": WestworldScript.Allies.cs) in the
        /// last week, two game hours or more back, that they haven't talked over with you yet (Told 0); and a comrade who fell
        /// in it whom they knew (their "allyfell:" memory within three game hours of it), or null. FIVEDAY item 4.
        private MindMemory FriendFought(MindWho friend, long g, out string fallen)
        {
            fallen = null;
            PersonState st = _store.Find(friend.Id);
            return st != null ? FriendCalls.FoughtMemory(st.Mind, g, out fallen, st.Told) : null;
        }

        /// What a friend would warn of, by what the mind knows: "grudge" (someone near mourns one you killed, or is still hot with
        /// anger at you; their name only if the friend knows them) or "law" (a deed of yours in this town in the last two days);
        /// null for nothing.
        private string FriendDanger(MindWho friend, long g, out string danger)
        {
            danger = null;
            PinkVisit pv = _cfg.Pinkertons ? PinkToWarn(_placeTown, g) : null;
            if (pv != null)
            {
                danger = pv.Talked;
                return "pinkerton";   // a Pinkerton man asked after you here: said first, the freshest danger (FIVEDAY item 15)
            }
            foreach (int oh in _mindNear)
            {
                MindWho o;
                if (!_mindWho.TryGetValue(oh, out o) || o.Id == null || o.Id == friend.Id)
                    continue;
                if (!_engine.MournsYourKill(o.Id, 7) && FeltAboutYou(o.Id, "anger", g) < Warnings.GrudgeAnger)
                    continue;
                if (!ENTITY.DOES_ENTITY_EXIST(oh) || ENTITY.IS_ENTITY_DEAD(oh))
                    continue;
                danger = WarnKnowsOf(friend, o) ? o.Name : null;
                return "grudge";
            }
            if (FriendCalls.ThiefWarnOn && LiftsOn() && Game.GameTime >= _liftGuardUntil)
            {
                // a known pickpocket near you (DEEPEN item 3): the friend has seen him at it, or heard (his name only if they know him)
                Ped me = Game.Player.Character;
                string plain = null, plainName = null;   // a known pickpocket near; the one who robbed you is preferred (review 3)
                foreach (int oh in _mindNear)
                {
                    MindWho o;
                    if (!_mindWho.TryGetValue(oh, out o) || o.Id == null || o.Id == friend.Id || !ENTITY.DOES_ENTITY_EXIST(oh) || ENTITY.IS_ENTITY_DEAD(oh))
                        continue;
                    PersonState ps = _store.Find(o.Id);
                    if (ps == null || !ps.Has(PickpocketMark) || ENTITY.GET_ENTITY_COORDS(oh, true, true).DistanceTo(me.Position) > 40f)
                        continue;
                    string named = WarnKnowsOf(friend, o) ? o.Name : null;
                    if (FriendCalls.YoursOn && ps.Has(RobbedYouMark))
                    {
                        danger = named;
                        return FriendCalls.ThiefYours;   // the one who robbed you (SEAMS item 14)
                    }
                    if (plain == null)
                    {
                        plain = "thief";
                        plainName = named;
                    }
                }
                if (plain != null)
                {
                    danger = plainName;
                    return plain;
                }
            }
            if (_placeTown != null)
                for (int i = _store.Deeds.Count - 1; i >= 0; i--)
                {
                    Deed d = _store.Deeds[i];
                    if (d == null || g - d.At > 2880)
                        continue;
                    string grp = Minds.DeedGroup(d.Kind);
                    if (d.Known && d.Town == _placeTown && (grp == "deed:violence" || grp == "deed:theft"))
                        return "law";   // a known violence or theft of yours here in two days, the warnings' own rule: never a visit, a purchase or a kindness (the review)
                }
            return null;
        }

        /// The news a friend would bring: one person's words on the almanac's freshest story, or null (the almanac off, or
        /// nothing fresh).
        private string FriendNews(string seed)
        {
            if (!_cfg.HistoricalAccuracy || !_cfg.Almanac)
                return null;
            AlmanacEvent e = Almanac.Freshest(InnerLife.ClockYear, InnerLife.ClockMonth, InnerLife.ClockDay);
            if (e == null || e.Lines == null || e.Lines.Length == 0)
                return null;
            return e.Lines[Voice.Hash(seed + "|news") % e.Lines.Length];
        }

        /// The gun you carry, to load (0: none).
        private static uint FriendGun(Ped me)
        {
            uint w = WEAPON.GET_BEST_PED_WEAPON(me.Handle, false, false);
            return w != 0 && WEAPON.IS_WEAPON_A_GUN(w) ? w : 0;
        }

        private static string FriendWhat(string kind, string detail, int cents)
        {
            switch (kind)
            {
                case FriendCalls.Warn: return detail == "law" ? "a warning: the law has been asking" : "a warning: someone in town holds a grudge";
                case FriendCalls.News: return "the news";
                case SecondActs.Saved: return "their thanks for saving them" + (detail != null ? " (and " + (detail == "coin" ? "$" + (cents / 100.0).ToString("0.00") : detail) + ")" : "");   // (reserve R3)
                case FriendCalls.Fought: return detail == "fell" ? "the fight they stood in with you, and the one who fell in it" : "the fight they stood in with you";
                case FriendCalls.Gift: return detail == "coin" ? "a gift of $" + (cents / 100.0).ToString("0.00") : detail == "rounds" ? "a gift of cartridges" : "something to eat";
                default: return detail == "patch" ? "the doctor's favour: a look at your hurts" : detail == "rounds" ? "the gunsmith's favour: a box of rounds" : detail == "drink" ? "a drink on the house" : "something for the road";
            }
        }

        /// The one walking over: says it to your face once there, hands it over, then goes on their way.
        private void FriendWalkStep(Ped me, int now)
        {
            FriendWalk w = _friendWalk;
            if (w == null)
                return;
            if (w.Ped == null || !w.Ped.Exists() || w.Ped.IsDead || _director.Involves(w.Ped.Handle) || MayDirectWhyNot(w.Ped) != null || MISC.GET_MISSION_FLAG() || PED.IS_PED_IN_COMBAT(w.Ped.Handle, 0))
            {
                _friendWalk = null;   // someone else has them now (enRAGE, a fright, a scene): our orders stop here
                return;
            }
            int h = w.Ped.Handle;
            if (w.SaidAt == 0)
            {
                if (_menu != null || _grudgeWalk != null || _kinWalk != null || _lifter != null || _thanksWalk != null)
                {
                    TASK.TASK_CLEAR_LOOK_AT(h);
                    TASK.TASK_WANDER_STANDARD(h, 10f, 10);   // you're taken up with someone else: another time
                    _friendWalk = null;
                    Log.Write("FRIEND " + w.Name + " saw you busy and went on their way");
                    return;
                }
                if (w.Ped.Position.DistanceTo(me.Position) < 2.6f)
                {
                    TASK.TASK_TURN_PED_TO_FACE_ENTITY(h, me.Handle, 4500, 0f, 0f, 0f);
                    bool warm = w.Kind == FriendCalls.Gift || w.Kind == FriendCalls.Trade || w.Kind == SecondActs.Saved;
                    Feed.Say(w.Name, w.Line, Speech.For(h, w.Line, me.Handle, 0, 0, true, warm ? "thanks" : "mild"), h, toYou: true);
                    string given = w.Test ? null : FriendGive(me, w);
                    w.SaidAt = now;
                    if (w.Id != null)
                        _youNodAt[w.Id] = now;   // came over to you: no passing hat-tip straight after (ToYou's rest)
                    _friendCalls++;
                    if (w.Kind == SecondActs.Saved)
                        SecondActThanked(w.Name, w.Line);   // (FOURTEENHOUR reserve R3)
                    if (w.Kind == FriendCalls.Warn && (w.Detail == "thief" || w.Detail == FriendCalls.ThiefYours) && !w.Test)
                    {
                        _liftGuardUntil = now + FriendCalls.GuardMs;   // forewarned: on your guard a while, and the thieves can tell (DEEPEN item 3)
                        Log.Write("FRIEND warned you of a pickpocket: on your guard for " + FriendCalls.GuardMs / 60000 + " min (no lift tried)");
                    }
                    int c;
                    _friendKinds.TryGetValue(w.Kind, out c);
                    _friendKinds[w.Kind] = c + 1;
                    _friendLast = w.Name + ": " + w.Line + (w.Test ? " (a test)" : "");
                    if (w.Mark != null)
                    {
                        PersonState fs = _store.Find(w.Id);
                        if (fs != null)
                        {
                            if (fs.Told == null)
                                fs.Told = new List<string>();
                            if (!fs.Told.Contains(w.Mark))
                                fs.Told.Add(w.Mark);   // talked it over with you: once a fight
                        }
                    }
                    Log.Write("FRIEND " + w.Name + " to your face: " + w.Line + (given != null ? " (" + given + ")" : w.Test && (w.Kind == FriendCalls.Gift || w.Kind == FriendCalls.Trade) ? " (a test: nothing handed over)" : ""));
                }
                else if (now > w.Until)
                {
                    TASK.TASK_CLEAR_LOOK_AT(h);
                    TASK.TASK_WANDER_STANDARD(h, 10f, 10);
                    _friendWalk = null;
                    Log.Write("FRIEND " + w.Name + " never reached you: went on their way");
                }
                return;
            }
            if (now - w.SaidAt > 5000)
            {
                _ticker.Push(w.Name + ", thinking", w.After, "near", h);   // their own thought as they go
                TASK.TASK_CLEAR_LOOK_AT(h);
                TASK.TASK_WANDER_STANDARD(h, 10f, 10);
                _friendWalk = null;
            }
        }

        /// What's handed over, and the hint that says so; null for words only. Cents into your purse, cartridges into the gun you
        /// carry, a meal into your health core, a drink into your stamina core, the doctor's patching back to full health.
        private string FriendGive(Ped me, FriendWalk w)
        {
            if (!_cfg.FriendGifts)
                return null;
            int you = me.Handle;
            string said = null;
            if ((w.Kind == FriendCalls.Gift || w.Kind == SecondActs.Saved) && w.Detail == "coin" && w.Cents > 0)
            {
                MONEY._MONEY_INCREMENT_CASH_BALANCE(w.Cents, 0);
                _friendGivenCents += w.Cents;
                said = w.Name + " gave you $" + (w.Cents / 100.0).ToString("0.00") + ".";
            }
            else if (w.Detail == "rounds")
            {
                uint gun = FriendGun(me);
                if (gun == 0)
                    return null;
                WEAPON._ADD_AMMO_TO_PED(you, gun, FriendCalls.RoundsGiven, FriendGiveReason);
                said = w.Name + " gave you " + FriendCalls.RoundsGiven + " cartridges.";
            }
            else if (w.Detail == "meal" || w.Detail == "drink")
            {
                int core = w.Detail == "meal" ? 0 : 1;   // 0 health, 1 stamina
                int now = ATTRIBUTE._GET_ATTRIBUTE_CORE_VALUE(you, core);
                ATTRIBUTE._SET_ATTRIBUTE_CORE_VALUE(you, core, Math.Min(100, Math.Max(0, now) + FriendCalls.CoreGiven));
                said = w.Detail == "meal" ? w.Name + " gave you something to eat." : w.Name + " stood you a drink.";
            }
            else if (w.Detail == "patch")
            {
                ENTITY.SET_ENTITY_HEALTH(you, ENTITY.GET_ENTITY_MAX_HEALTH(you, false), 0);
                ATTRIBUTE._SET_ATTRIBUTE_CORE_VALUE(you, 0, 100);
                said = w.Name + " patched you up.";
            }
            if (said != null)
            {
                _friendGifts++;
                Hint(said, 3000);
            }
            return said;
        }

        private string FriendCallTest(int now)
        {
            if (!_cfg.FriendCalls)
                return "off ([Scenes] FriendCalls)";
            if (_friendWalk != null)
                return "a friend is already on the way";
            _nextFriendCall = 0;
            return FriendLook(Game.Player.Character, now, true);
        }

        private void FriendAbort()
        {
            if (_friendWalk != null && _friendWalk.Ped != null && _friendWalk.Ped.Exists())
            {
                TASK.TASK_CLEAR_LOOK_AT(_friendWalk.Ped.Handle);
                TASK.TASK_WANDER_STANDARD(_friendWalk.Ped.Handle, 10f, 10);
            }
            _friendWalk = null;
        }

        /// F7: on or off, the visits this session by kind, what was handed over, the last, and why none now.
        private string FriendCallsStatus()
        {
            if (!_cfg.FriendCalls)
                return "off ([Scenes] FriendCalls)";
            return _friendCalls + " friends came over this session" + (_friendKinds.Count > 0 ? " (" + string.Join(", ", _friendKinds.Select(kv => kv.Value + " " + kv.Key)) + ")" : "") +
                   ", " + _friendGifts + " things handed over" + (_friendGivenCents > 0 ? " ($" + (_friendGivenCents / 100.0).ToString("0.00") + " in all)" : "") +
                   (_cfg.FriendGifts ? "" : " (FriendGifts off: words only)") + (_friendLast != null ? "; last: " + _friendLast : "") + (_friendWhyNot != null ? "; last look: " + _friendWhyNot : "");
        }
    }
}
