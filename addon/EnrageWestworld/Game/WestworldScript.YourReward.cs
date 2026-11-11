// Post a reward, the game side (DEPTH reserve R4; Core/YourReward.cs; [Scenes] PostReward). A look every 2 s (the frame pass's scheduler;
// Tick04's own call with it off), in its own guard: within 6 m of the town's jail door (known once you've walked out of the sheriff's
// office), J free, a theft in this town in the last three game days against someone whose name you know: J, put five dollars up. The
// rewards posted are settled by the same look wherever you are (an hour to ten game hours on): brought in by a regular (the money his,
// the one robbed fonder of you), a false claim (your money back), or nobody (back after two game days); each told in that town's
// Around town box. In memory: a reload forgets a posted reward (and the game's own save has your money as it was). Runner: the J menu
// and the news box (seen), the jail door (built).
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
        private sealed class PostedReward
        {
            public string VictimId, VictimName, ThiefId, ThiefName, Town, Seed, Outcome;
            public long PostedAt, DueAt;
        }

        private readonly List<PostedReward> _rewardsPosted = new List<PostedReward>();
        private readonly HashSet<string> _rewardOffered = new HashSet<string>();
        private Action _aYourReward;
        private int _grumbleCast, _grumbleNoFriend;   // old hands grumble (DEPTH reserve R5; WestworldScript.Reasons.cs)
        private int _yrNextLook, _yrPosted, _yrBrought, _yrFalse, _yrLapsed, _yrForgot;
        private string _yrLast, _yrVictimId, _yrVictimName, _yrThiefId, _yrThiefName;

        private void YourRewardStep(int now)
        {
            if (!YourReward.On || now < _yrNextLook || _store == null)
                return;
            _yrNextLook = now + 2000;
            Safe("post a reward", _aYourReward ?? (_aYourReward = () => YourRewardLook(_now)));
        }

        private void YourRewardLook(int now)
        {
            long g = _store.LastGameMin;
            for (int i = _rewardsPosted.Count - 1; i >= 0; i--)
            {
                if (g < _rewardsPosted[i].PostedAt)
                {
                    Log.Write("REWARD forgotten: the clock went back past its posting (a reload: the save never spent the money)");
                    _rewardsPosted.RemoveAt(i);   // nothing paid back, nothing told (review)
                    _yrForgot++;
                }
                else if (g >= _rewardsPosted[i].DueAt)
                    YourRewardSettle(_rewardsPosted[i], g);
            }
            Vector3 door;
            if (_placeTown == null || !_jailDoor.TryGetValue(_placeTown, out door) || Game.Player.Character.Position.DistanceTo(door) > YourReward.DoorMetres ||
                _eventMenu != null || (_eventPrompt != null && Game.GameTime <= _eventPromptUntil))
                return;
            foreach (SceneTie t in _store.Ties)
            {
                if (t == null || t.Crime == null || t.CrimeDoer == null || t.Town != _placeTown || g - t.CrimeAt < 0 || g - t.CrimeAt > YourReward.WithinDays * 1440L)
                    continue;
                string victimId = t.OtherId(t.CrimeDoer), key = victimId + "|" + t.CrimeAt;
                PersonState vs = _store.Find(victimId);
                if (victimId == null || vs == null || !vs.KnowsName || _rewardOffered.Contains(key))
                    continue;
                _rewardOffered.Add(key);
                _yrVictimId = victimId;
                _yrVictimName = t.A == victimId ? t.AName : t.BName;
                _yrThiefId = t.CrimeDoer;
                _yrThiefName = t.A == t.CrimeDoer ? t.AName : t.BName;
                PromptSet(YourReward.Kind, new TownEvent { Kind = YourReward.Kind, Town = _placeTown, AName = _yrVictimName, AId = victimId }, 9000);
                return;
            }
        }

        private void YourRewardChoose(string id)
        {
            if (id != "postreward:yes" || _yrVictimId == null || MONEY._MONEY_GET_CASH_BALANCE() < YourReward.Cents || _rewardsPosted.Count >= 6)
                return;
            MONEY._MONEY_DECREMENT_CASH_BALANCE(YourReward.Cents);
            long g = _store.LastGameMin;
            string seed = _yrVictimId + "|" + g;
            var r = new PostedReward { VictimId = _yrVictimId, VictimName = _yrVictimName, ThiefId = _yrThiefId, ThiefName = _yrThiefName, Town = _placeTown, Seed = seed, Outcome = YourReward.Outcome(seed), PostedAt = g };
            r.DueAt = g + (r.Outcome == "none" ? YourReward.LapseMinutes : YourReward.After(seed));
            _rewardsPosted.Add(r);
            _yrPosted++;
            NewsPush(_placeTown, YourReward.Posted(Person.FirstOf(r.VictimName), _placeTown), "news");
            Log.Write("REWARD posted: five dollars with the sheriff in " + _placeTown + " for whoever robbed " + r.VictimName + " (it will be: " + r.Outcome + ")");
        }

        /// It comes to its end: brought in, a false claim, or nobody.
        private void YourRewardSettle(PostedReward r, long g)
        {
            _rewardsPosted.Remove(r);
            var men = _store.Residents.Where(x => x != null && x.Id != null && x.Name != null && !x.Woman && !x.Dead && !x.Left && !x.Fled && x.Town == r.Town && x.Id != r.ThiefId && x.Id != r.VictimId)
                                      .OrderBy(x => x.Personality == "Hothead" || x.Personality == "Proud" || x.Personality == "Greedy" ? 0 : 1).ThenBy(x => Voice.Hash(r.Seed + x.Id)).ToList();
            string thief = Person.FirstOf(r.ThiefName) ?? "the thief";
            if (r.Outcome == "brought" && men.Count > 0)
            {
                NewsPush(r.Town, YourReward.Brought(Person.FirstOf(men[0].Name), thief, Person.FirstOf(r.VictimName), r.Seed), "news");
                PersonState vs = _store.Find(r.VictimId);
                if (vs != null)
                    vs.Affection = Romance.Clamp(vs.Affection + 5, -100, 100);   // the one robbed, fonder of the stranger who put up money
                _yrBrought++;
                _yrLast = men[0].Name + " brought " + r.ThiefName + " in";
                foreach (int h in _mindNear)
                {
                    MindWho w;
                    if (_mindWho.TryGetValue(h, out w) && w.Id == r.VictimId)
                    {
                        _ticker.Push(Person.FirstOf(w.Name) + ", thinking", YourReward.VictimThinks(r.Seed), "near", h);   // the one robbed, if he's near you
                        break;
                    }
                }
            }
            else if (r.Outcome == "false" && men.Count > 1)
            {
                NewsPush(r.Town, YourReward.FalseClaim(Person.FirstOf(men[0].Name), Person.FirstOf(men[1].Name), r.Seed), "news");
                MONEY._MONEY_INCREMENT_CASH_BALANCE(YourReward.Cents, 0);   // your money back
                _yrFalse++;
                _yrLast = "a false claim by " + men[0].Name + "; your money back";
            }
            else
            {
                NewsPush(r.Town, YourReward.Lapsed(Person.FirstOf(r.VictimName)), "news");
                MONEY._MONEY_INCREMENT_CASH_BALANCE(YourReward.Cents, 0);
                _yrLapsed++;
                _yrLast = "nobody claimed it; your money back";
            }
            Log.Write("REWARD settled in " + r.Town + ": " + _yrLast);
        }

        private string YourRewardStatus()
        {
            return !YourReward.On ? "off ([Scenes] PostReward)" : "posted " + _yrPosted + " (open " + _rewardsPosted.Count + "), brought in " + _yrBrought + ", false claims " + _yrFalse + ", lapsed " + _yrLapsed + ", forgotten on a reload " + _yrForgot + (_yrLast != null ? "; the last: " + _yrLast : "");
        }
    }
}
