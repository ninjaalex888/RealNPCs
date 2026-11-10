// After a big brawl, the game side (FOURTEENHOUR item 12; Core/BrawlAfter.cs; [Scenes] BrawlAfter). Each join look notes who is on
// which side and the most it grew to; when the fight is over (OnFightOver) the week after it begins: the winner of a saloon's fight
// stands a round (the round's own reason, "won"), and a big one (six or more) has the town's news with its numbers, the barkeep's
// broken chairs (J at the bar pays for them), and the men of opposite sides sore at each other (a brawl tie, so the next fight
// among them grows sooner). A man the law tied up for it is locked up overnight: the next morning, 7 to noon, his wife (or
// his mother) waits at the jail door with his breakfast, and J pays his fine; unpaid, he's out at noon and sore. Runners: the
// Director's brawls and the law's (seen), the J menu (seen), the round and the spawner (built). Looks every 2 s, never a frame.
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
        private sealed class Jailed
        {
            public string Id, Name, Town, Waits;   // Waits: "his wife" or "his mother"
            public long Day;
            public Ped Waiter;
            public bool Asked, Paid;
            public int Tries, ReleaseAt;
        }

        private readonly List<Jailed> _jailed = new List<Jailed>();
        private readonly Dictionary<int, int> _afterSide = new Dictionary<int, int>();
        private readonly Dictionary<string, long> _brawlWon = new Dictionary<string, long>();      // a saloon fight's winner: id -> until (game minute)
        private readonly Dictionary<string, int> _saloonDamage = new Dictionary<string, int>();   // "town|day" -> cents for the chairs
        private int _afterFor = -1, _afterPeak, _afterDowns, _jailNextLook, _finesPaid, _jailSour, _soreTies, _damagePaid, _bigNews;
        private bool _afterSaloon;
        private Jailed _jailAsking;

        private bool BrawlAfterOn { get { return _cfg != null && _cfg.BrawlAfter && _cfg.MindLive; } }

        /// Each join look: who is on which side, and the most it grew to.
        private void AfterNote(List<int> a, List<int> b)
        {
            if (_afterFor != _director.FightId)
            {
                _afterFor = _director.FightId;
                _afterSide.Clear();
                _afterPeak = 0;
                _afterDowns = 0;
                _afterSaloon = _placeKind == "saloon";
            }
            foreach (int h in a)
                _afterSide[h] = 0;
            foreach (int h in b)
                _afterSide[h] = 1;
            _afterPeak = Math.Max(_afterPeak, a.Count + b.Count);
        }

        /// From OnFightOver: the winner's round, and for a big brawl the news, the chairs and the sore sides.
        private void BrawlWeekAfter(int winner, int loser)
        {
            if (!BrawlAfterOn || _afterFor != _director.FightId || _store == null)
                return;
            int now = Game.GameTime;
            long g = _store.LastGameMin;
            MindWho ww = winner != 0 && ENTITY.DOES_ENTITY_EXIST(winner) ? MindKnown(winner, now) : null;
            if (_afterSaloon && ww != null && ww.Id != null)
                _brawlWon[ww.Id] = g + 120;   // he stands the house a round, these two hours (the round's reason "won")
            if (_afterPeak < Brawl.BigAt)
                return;
            _bigNews++;
            NewsPush(_placeTown ?? "Around here", BrawlAfter.News(_afterPeak, _afterDowns, _afterSaloon, CLOCK.GET_CLOCK_HOURS(), "f" + _afterFor), "news");
            if (_afterSaloon && _placeTown != null)
            {
                string k = _placeTown + "|" + g / 1440;
                int d;
                _saloonDamage.TryGetValue(k, out d);
                _saloonDamage[k] = Math.Min(BrawlAfter.DamageMostCents, d + BrawlAfter.Damage(_afterPeak));
            }
            // sore sides: the men of opposite sides keep a brawl tie (hostile), so the next fight among them grows sooner
            var s0 = _afterSide.Where(kv => kv.Value == 0).Select(kv => ENTITY.DOES_ENTITY_EXIST(kv.Key) ? MindKnown(kv.Key, now) : null).Where(w => w != null && w.Id != null).ToList();
            var s1 = _afterSide.Where(kv => kv.Value == 1).Select(kv => ENTITY.DOES_ENTITY_EXIST(kv.Key) ? MindKnown(kv.Key, now) : null).Where(w => w != null && w.Id != null).ToList();
            int pairs = 0;
            foreach (MindWho x in s0)
                foreach (MindWho y in s1)
                {
                    if (pairs >= BrawlAfter.SoreMostPairs || x.Id == y.Id || LadderWarmth(x.Id, y.Id) < 0)
                        continue;   // a tie already sore stays as it is
                    Ties.Note(_store.Ties, x.Id, x.Name, y.Id, y.Name, _placeTown, "brawl", Bridge.Now());
                    pairs++;
                }
            _soreTies += pairs;
            Log.Write("BRAWL after: " + _afterPeak + " were in it, " + _afterDowns + " down; " + pairs + " pairs of opposite sides sore at each other" + (_afterSaloon ? "; the barkeep counts his chairs" : ""));
        }

        /// The law tied him up for it (WestworldScript.Brawl.cs): locked up overnight, and his wife or mother at the door at dawn.
        private void AfterJailed(int t)
        {
            if (!BrawlAfterOn || _placeTown == null || _store == null || !_jailDoor.ContainsKey(_placeTown))
                return;
            MindWho w = ENTITY.DOES_ENTITY_EXIST(t) ? MindKnown(t, Game.GameTime) : null;
            if (w == null || w.Id == null || w.Name == null || _jailed.Any(j => j.Id == w.Id))
                return;
            bool married = w.P != null && w.P.Family == "married";
            _jailed.Add(new Jailed { Id = w.Id, Name = w.Name, Town = _placeTown, Day = _store.LastGameMin / 1440, Waits = married ? "his wife" : "his mother" });
            if (_jailed.Count > 6)
                _jailed.RemoveAt(0);
            Log.Write("BRAWL after: " + w.Name + " is locked up overnight at " + _placeTown + "; " + (married ? "his wife" : "his mother") + " will be at the door in the morning");
        }

        /// The morning after: every 2 s, the jail door of the town he's in.
        private void JailDoorStep(int now)
        {
            if (_jailed.Count == 0 || now < _jailNextLook || !BrawlAfterOn)
                return;
            _jailNextLook = now + 2000;
            Safe("the jail door", () => JailDoorLook(now));
        }

        private void JailDoorLook(int now)
        {
            Ped me = Game.Player.Character;
            long day = _store.LastGameMin / 1440;
            int hour;
            hour = CLOCK.GET_CLOCK_HOURS();
            for (int i = _jailed.Count - 1; i >= 0; i--)
            {
                Jailed j = _jailed[i];
                bool over = day > j.Day + 1 || (day == j.Day + 1 && hour >= BrawlAfter.ToHour);
                if (over)
                {
                    if (!j.Paid)
                    {
                        _jailSour++;
                        if (_placeTown == j.Town)
                            NewsPush(j.Town, j.Name + " " + BrawlAfter.OutSourLine(j.Id), "news");
                        Log.Write("BRAWL after: " + j.Name + " is out of the cells at noon, his fine unpaid");
                    }
                    JailWaiterGo(j, true);
                    _jailed.RemoveAt(i);
                    continue;
                }
                if (j.ReleaseAt > 0 && now >= j.ReleaseAt)
                    JailWaiterGo(j, false);
                Vector3 door;
                if (day != j.Day + 1 || hour < BrawlAfter.FromHour || j.Paid || _placeTown != j.Town || !_jailDoor.TryGetValue(j.Town, out door) || me.Position.DistanceTo(door) > BrawlAfter.DoorMetres)
                    continue;
                if (j.Waiter == null && j.Tries < 30 && !Natives.Looking(door + new Vector3(0f, 0f, 1f), 50f) && Spawner.SafeToSpawn())
                {
                    j.Tries++;
                    uint model = Spawner.ModelFor(j.Town, true, Voice.Hash(j.Id + "|waits"));
                    Vector3 at = door + new Vector3(1.4f, 0.8f, 0f);
                    Ped p = Spawner.Spawn(model, at, Voice.Hash(j.Id) % 360);
                    if (p != null)
                    {
                        j.Waiter = p;
                        TASK.TASK_STAND_STILL(p.Handle, 120000);   // she waits there, at the door
                        Log.Write("BRAWL after: " + j.Waits + " waits at the jail door in " + j.Town + " for " + j.Name);
                    }
                }
                if (j.Waiter != null && !j.Asked && j.Waiter.Exists() && me.Position.DistanceTo(j.Waiter.Position) <= BrawlAfter.AskMetres &&
                    _eventMenu == null && (_eventPrompt == null || Game.GameTime > _eventPromptUntil))
                {
                    j.Asked = true;
                    _jailAsking = j;
                    TASK.TASK_TURN_PED_TO_FACE_ENTITY(j.Waiter.Handle, me.Handle, 3000, 0f, 0f, 0f);
                    SpeechCaption(Grammar.UpperFirst(j.Waits.Replace("his ", Person.FirstOf(j.Name) + "'s ")), BrawlAfter.WaitsLine(j.Id), j.Waiter.Handle, 4500, true, false);
                    PromptSet(BrawlAfter.JailKind, new TownEvent { Kind = BrawlAfter.JailKind, Town = j.Town, AName = j.Name, AId = j.Id }, 15000);
                }
            }
        }

        /// J at the jail door: his fine paid, or not.
        private void JailDoorChoose(string id)
        {
            Jailed j = _jailAsking;
            _jailAsking = null;
            if (j == null || id != "jaildoor:pay" || MONEY._MONEY_GET_CASH_BALANCE() < BrawlAfter.FineCents)
                return;
            Ped me = Game.Player.Character;
            MONEY._MONEY_DECREMENT_CASH_BALANCE(BrawlAfter.FineCents);
            j.Paid = true;
            _finesPaid++;
            if (j.Waiter != null && j.Waiter.Exists())
            {
                SpeechCaption(Grammar.UpperFirst(j.Waits.Replace("his ", Person.FirstOf(j.Name) + "'s ")), BrawlAfter.ThanksLine(j.Id), j.Waiter.Handle, 4500, true, false);
                RecordEvent("paidfine", "player", "you", j.Id, j.Name, j.Waiter.Position, me.Handle, me.Handle, j.Waiter.Handle, "paid his fine");
            }
            j.ReleaseAt = Game.GameTime + 8000;
            NewsPush(j.Town, "A stranger paid " + j.Name + "'s fine at the jail this morning.", "news");
            Log.Write("BRAWL after: you paid " + j.Name + "'s fine (" + BrawlVerbs.Money(BrawlAfter.FineCents) + "); " + j.Waits + " thanks you");
        }

        /// The one waiting goes on her way, and is handed back (deleted if nobody sees).
        private void JailWaiterGo(Jailed j, bool now)
        {
            if (j.Waiter == null)
                return;
            Ped p = j.Waiter;
            j.Waiter = null;
            j.ReleaseAt = 0;
            if (!p.Exists())
                return;
            TASK.TASK_WANDER_STANDARD(p.Handle, 10f, 10);
            Spawner.Release(p, !ENTITY.IS_ENTITY_ON_SCREEN(p.Handle));
        }

        /// What the chairs come to here today, unpaid (for J at the bar).
        private int SaloonDamageToday()
        {
            int d;
            return _placeTown != null && _store != null && _saloonDamage.TryGetValue(_placeTown + "|" + _store.LastGameMin / 1440, out d) ? d : 0;
        }

        /// J at the bar: paying the barkeep for his broken chairs.
        private void SaloonDamagePay()
        {
            int d = SaloonDamageToday();
            if (d <= 0 || MONEY._MONEY_GET_CASH_BALANCE() < d)
                return;
            MONEY._MONEY_DECREMENT_CASH_BALANCE(d);
            _saloonDamage.Remove(_placeTown + "|" + _store.LastGameMin / 1440);
            _damagePaid++;
            if (_barKeeper != null && _barKeeper.Exists())
                SpeechCaption(_barKeeperName ?? "The barkeep", BrawlAfter.PaidChairsLine(_placeTown + "|" + _damagePaid), _barKeeper.Handle, 3500, true, false);
            NewsPush(_placeTown, "A stranger paid for the saloon's broken chairs.", "news");
            Log.Write("BRAWL after: you paid the barkeep " + BrawlVerbs.Money(d) + " for the damage");
        }

        // ---- payday night's word (FOURTEENHOUR item 13): once a town a payday night, as you come into a town with a saloon ----
        private readonly HashSet<string> _payNightSaid = new HashSet<string>();
        private int _payNightLookAt;

        private void PaydayNightStep(int now)
        {
            if (now < _payNightLookAt || _cfg == null || !_cfg.Payday || !_cfg.PaydayNight || _placeTown == null || _store == null)
                return;
            _payNightLookAt = now + 10000;
            if (!Payday.Night(InnerLife.Weekday, CLOCK.GET_CLOCK_HOURS()) || !_saloonDoor.ContainsKey(_placeTown))
                return;
            string key = _placeTown + "|" + (_store.LastGameMin - 240) / 1440;   // (the small hours count with the Saturday they follow)
            if (!_payNightSaid.Add(key))
                return;
            NewsPush(_placeTown, Payday.NightNewsLine(key), "news");
            Log.Write("PAYDAY night in " + _placeTown + ": the hands are paid and in town; the saloon's fights come easier tonight");
        }

        /// F7: the week after this session's fights.
        private string BrawlAfterStatus()
        {
            if (!BrawlAfterOn)
                return "off ([Scenes] BrawlAfter)";
            return _bigNews + " big brawls in the news, " + _soreTies + " sore pairs, " + _brawlWon.Count + " winners due a round; the cells: " + _jailed.Count + " waiting, " + _finesPaid + " fines you paid, " +
                   _jailSour + " out sore; chairs paid for " + _damagePaid + (SaloonDamageToday() > 0 ? "; here today " + BrawlVerbs.Money(SaloonDamageToday()) + " owed for the chairs" : "");
        }
    }
}
