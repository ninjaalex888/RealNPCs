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
            public Person P;      // who he is, for the chance he keeps the peace if you vouch for him (reserve R22)
            public bool Test;     // the Testing row's "vouch": waited for at any hour
            public List<string> Mates;   // the men who fought on his side (DEPTH item 6: they keep their heads down when she has her say)
            public bool Coming;          // he's to come out to her now (paid, or noon with you near): the door is tried until he does
            public int OutTries;
            public Ped Him;              // his body, made at the door when he comes out
        }

        private int _jailWords, _jailWordsNoScene, _jailMatesAway;

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
                _afterStarter = a.Count > 0 ? NameOfFighter(a[0]) : null;   // who began it, for the tale (FOURTEENHOUR item 24)
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
            // EndedFightId: by OnFightOver the live fight is gone and FightId reads 0, so until the review of items 1-13 this never
            // got past here (only the jail door worked)
            if (!BrawlAfterOn || _afterFor <= 0 || _afterFor != _director.EndedFightId || _store == null)
                return;
            int fid = _afterFor;
            _afterFor = -1;   // once a fight
            int now = Game.GameTime;
            long g = _store.LastGameMin;
            VouchSeenFight(now);   // a man you vouched for, in it on the side that began it (WestworldScript.Vouch.cs; reserve R22)
            MindWho ww = winner != 0 && ENTITY.DOES_ENTITY_EXIST(winner) ? MindKnown(winner, now) : null;
            if (_afterSaloon && ww != null && ww.Id != null)
                _brawlWon[ww.Id] = g + 120;   // he stands the house a round, these two hours (the round's reason "won")
            if (_afterPeak < Brawl.BigAt)
                return;
            _bigNews++;
            NewsPush(_placeTown ?? "Around here", BrawlAfter.News(_afterPeak, _afterDowns, _afterSaloon, CLOCK.GET_CLOCK_HOURS(), "f" + fid), "news");
            TaleNote(fid, loser);   // and its tale, to grow in the telling (FOURTEENHOUR item 24; WestworldScript.Tales.cs)
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
                    SceneTie st = Ties.Note(_store.Ties, x.Id, x.Name, y.Id, y.Name, _placeTown, "brawl", Bridge.Now());
                    if (st != null && st.Count == 1)
                        st.Sore = true;   // strangers till now: a stake in the next fight among them, not a quarrel of its own (Reasons)
                    pairs++;
                }
            _soreTies += pairs;
            Log.Write("BRAWL after: " + _afterPeak + " were in it, " + _afterDowns + " down; " + pairs + " pairs of opposite sides sore at each other" + (_afterSaloon ? "; the barkeep counts his chairs" : ""));
        }

        /// The law tied him up for it (WestworldScript.Brawl.cs): locked up overnight, and his wife or mother at the door at dawn.
        private void AfterJailed(int t)
        {
            if (!BrawlAfterOn || _placeTown == null || _store == null)
                return;   // (the door needn't be known yet: she waits once it is; the review of items 1-13 found most arrests dropped for it)
            MindWho w = ENTITY.DOES_ENTITY_EXIST(t) ? MindKnown(t, Game.GameTime) : null;
            if (w != null && w.Id != null)
                VouchAgain(w.Id, "law");   // a man you vouched for, taken in again inside his week (WestworldScript.Vouch.cs; reserve R22)
            if (w == null || w.Id == null || w.Name == null || _jailed.Any(j => j.Id == w.Id))
                return;
            bool married = w.P != null && w.P.Family == "married";
            _jailed.Add(new Jailed { Id = w.Id, Name = w.Name, Town = _placeTown, Day = JailNight(), Waits = married ? "his wife" : "his mother", P = w.P, Mates = AfterMatesOf(t, w.Id) });
            if (_jailed.Count > 6)
                _jailed.RemoveAt(0);
            Log.Write("BRAWL after: " + w.Name + " is locked up overnight at " + _placeTown + "; " + (married ? "his wife" : "his mother") + " will be at the door in the morning" +
                      (_jailDoor.ContainsKey(_placeTown) ? "" : " (the jail door here isn't known yet: walk out of the sheriff's office once and she'll be there)"));
        }

        /// The day of the night he's locked up for: a man tied up after midnight (payday night runs to 2) is waited for that
        /// same morning, not the next day's (the review of items 1-13).
        private long JailNight()
        {
            long day = _store.LastGameMin / 1440;
            return CLOCK.GET_CLOCK_HOURS() < 4 ? day - 1 : day;
        }

        /// A saloon in this town: one he's come out of, or the town's own on the map (Places.TownSpots' second door).
        private bool SaloonInTown(string town)
        {
            Vector3?[] doors;
            return town != null && (_saloonDoor.ContainsKey(town) || (EnrageWestworld.InGame.Places.TownSpots.TryGetValue(town, out doors) && doors.Length > 1 && doors[1].HasValue));
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
                bool over = day > j.Day + 1 || (!j.Test && day == j.Day + 1 && hour >= BrawlAfter.ToHour);   // (the Testing row's waits at any hour)
                if (over && !j.Paid && !j.Coming && j.Waiter != null && j.Waiter.Exists() && _placeTown == j.Town && me.Position.DistanceTo(j.Waiter.Position) <= BrawlAfter.DoorMetres)
                {
                    j.Coming = true;   // noon, his fine unpaid, and she's still at the door with you near: he comes out to her (DEPTH item 6)
                    j.ReleaseAt = now;
                }
                if (j.Coming && j.ReleaseAt > 0 && now >= j.ReleaseAt)
                {
                    if (!JailComesOut(j, now))
                        continue;   // the door in view: tried again on the next look
                    if (over)
                    {
                        if (!j.Paid)
                        {
                            _jailSour++;
                            NewsPush(j.Town, j.Name + " " + BrawlAfter.OutSourLine(j.Id), "news");
                            Log.Write("BRAWL after: " + j.Name + " is out of the cells at noon, his fine unpaid");
                        }
                        _jailed.RemoveAt(i);
                    }
                    continue;
                }
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
                if (j.ReleaseAt > 0 && now >= j.ReleaseAt && !j.Coming)
                    JailWaiterGo(j, false);
                Vector3 door;
                if (day != j.Day + 1 || (!j.Test && hour < BrawlAfter.FromHour) || j.Paid || _placeTown != j.Town || !_jailDoor.TryGetValue(j.Town, out door) || me.Position.DistanceTo(door) > BrawlAfter.DoorMetres)
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
                if (j.Waiter != null && !j.Asked && j.Waiter.Exists() && !PED.IS_PED_IN_COMBAT(j.Waiter.Handle, 0) && me.Position.DistanceTo(j.Waiter.Position) <= BrawlAfter.AskMetres &&
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
            if (j != null && id == Vouch.RowId)
            {
                VouchDo(j);   // he's out on your word, bound over a week (WestworldScript.Vouch.cs; FOURTEENHOUR reserve R22)
                return;
            }
            if (j == null || id != "jaildoor:pay" || MONEY._MONEY_GET_CASH_BALANCE() < BrawlAfter.FineCents)
                return;
            Ped me = Game.Player.Character;
            MONEY._MONEY_DECREMENT_CASH_BALANCE(BrawlAfter.FineCents);
            j.Paid = true;
            _finesPaid++;
            Happening paid = null;
            if (j.Waiter != null && j.Waiter.Exists())
            {
                SpeechCaption(Grammar.UpperFirst(j.Waits.Replace("his ", Person.FirstOf(j.Name) + "'s ")), BrawlAfter.ThanksLine(j.Id), j.Waiter.Handle, 4500, true, false);
                paid = RecordEvent("paidfine", "player", "you", j.Id, j.Name, j.Waiter.Position, me.Handle, me.Handle, j.Waiter.Handle, "paid his fine");
            }
            else if (_cfg.FineOwes)
                paid = RecordEvent("paidfine", "player", "you", j.Id, j.Name, me.Position, me.Handle, me.Handle, me.Handle, "paid his fine");   // the debt never hangs on her being there (the advisor)
            if (paid != null && j.Test)
                paid.Test = true;   // a test jailing (the [ key's) leaves no debt
            if (_cfg.FineOwes && !j.Test)
                FineRemembered(j);
            j.ReleaseAt = Game.GameTime + 8000;
            j.Coming = true;   // he comes out to her, and she has her words with him (DEPTH item 6)
            NewsPush(j.Town, "A stranger paid " + j.Name + "'s fine at the jail this morning.", "news");
            Log.Write("BRAWL after: you paid " + j.Name + "'s fine (" + BrawlVerbs.Money(BrawlAfter.FineCents) + "); " + j.Waits + " thanks you");
        }

        /// The man whose fine you paid keeps it (SEAMS item 1; [Encounters] FineOwes): a memory in his own mind (warmth, Sub
        /// "fine", the saved key with how "fine"), so his thoughts come back to it with their own words and he comes over
        /// later to thank you (SecondActs' late thanks: "You paid my fine at the jail..."). Only someone who already has a
        /// saved state, as SavedByYou's; nobody is made for it.
        private void FineRemembered(Jailed j)
        {
            try
            {
                if (j == null || !OwedMemory(j.Id, j.Town, SecondActs.FineSub, "fine", 70))
                {
                    Log.Detail("FINE " + (j != null ? j.Name : "?") + " has no saved state: the debt is in the ledger, no memory written");
                    return;
                }
                Log.Write("FINE " + j.Name + " owes you for his fine: the ledger, his memory, and his thanks to come");
            }
            catch (Exception ex)
            {
                Log.Write("FINE " + (j != null ? j.Name : "?") + "'s memory couldn't be kept: " + ex.Message);
            }
        }

        /// The men who fought on his side in the fight just over (the join looks' sides), by id; null for none.
        private List<string> AfterMatesOf(int t, string id)
        {
            int side;
            if (!_afterSide.TryGetValue(t, out side))
                return null;
            List<string> mates = null;
            foreach (KeyValuePair<int, int> kv in _afterSide)
            {
                MindWho w;
                if (kv.Key == t || kv.Value != side || !_mindWho.TryGetValue(kv.Key, out w) || w.Id == null || w.Id == id)
                    continue;
                (mates ?? (mates = new List<string>())).Add(w.Id);
            }
            return mates;
        }

        /// He comes out of the cells (DEPTH item 6): made at the door while nobody looks at it, and she has her words with him in the
        /// street (a Director scene, kind jailwords), then they walk off together; the men who fought beside him, near, turn away.
        /// False: not yet (the door in view, or no room to make him); after 10 tries, or with no one waiting, she goes as before.
        private bool JailComesOut(Jailed j, int now)
        {
            Ped me = Game.Player.Character;
            Vector3 door;
            if (j.Waiter == null || !j.Waiter.Exists() || PED.IS_PED_IN_COMBAT(j.Waiter.Handle, 0) || !_jailDoor.TryGetValue(j.Town, out door))
            {
                JailWaiterGo(j, false);
                return true;
            }
            if (j.Him == null)
            {
                if (j.OutTries++ >= 10)
                {
                    JailWaiterGo(j, false);
                    return true;
                }
                if (Natives.Looking(door + new Vector3(0f, 0f, 1f), 50f) || !Spawner.SafeToSpawn())
                    return false;
                uint model = Spawner.ModelFor(j.Town, false, Voice.Hash(j.Id + "|out"));
                j.Him = Spawner.Spawn(model, door + new Vector3(-0.6f, 0.6f, 0f), Voice.Hash(j.Id) % 360);
                if (j.Him == null)
                    return false;
            }
            Ped her = j.Waiter, him = j.Him;
            j.Waiter = null;
            j.Him = null;
            j.ReleaseAt = 0;
            string label = Grammar.UpperFirst(j.Waits.Replace("his ", Person.FirstOf(j.Name) + "'s "));
            string seed = j.Id + "|out|" + _store.LastGameMin / 60;
            SceneCast cast = BuildCast(her, him, null, false, me, now, seed);
            cast.Names[0] = label;
            cast.Names[1] = j.Name;
            Scene s = BrawlAfter.ComesOut(cast, seed, j.Waits == "his wife", j.Paid, j.P != null ? j.P.Personality : null);
            Action done = () =>
            {
                foreach (Ped p in new[] { her, him })
                    if (p != null && p.Exists())
                        Spawner.Release(p, !ENTITY.IS_ENTITY_ON_SCREEN(p.Handle) && !PED.IS_PED_IN_COMBAT(p.Handle, 0));
            };
            if (!_director.Start(s, new List<Ped> { her, him }, new[] { label, j.Name }, seed, done, cast))
            {
                _jailWordsNoScene++;
                Log.Write("BRAWL after: " + j.Name + " came out of the cells, but a scene was already playing: " + j.Waits + " and he walk off without a word");
                foreach (Ped p in new[] { her, him })
                    if (!PED.IS_PED_IN_COMBAT(p.Handle, 0))
                        TASK.TASK_WANDER_STANDARD(p.Handle, 10f, 10);
                done();
                return true;
            }
            _jailWords++;
            Log.Write("BRAWL after: " + j.Name + " came out of the cells" + (j.Paid ? " (his fine paid)" : " at noon") + ", and " + j.Waits + " has her words with him in the street");
            int away = 0;
            if (j.Mates != null)
                foreach (int h in _mindNear)
                {
                    MindWho w;
                    if (!_mindWho.TryGetValue(h, out w) || w.Id == null || !j.Mates.Contains(w.Id) || !ENTITY.DOES_ENTITY_EXIST(h) || ENTITY.IS_ENTITY_DEAD(h))
                        continue;
                    Ped m = new Ped(h);
                    if (m.Position.DistanceTo(door) > BrawlAfter.MatesMetres || PED.IS_PED_USING_ANY_SCENARIO(h) || NotFree(h) || _director.Involves(h) || MayDirectWhyNot(m) != null || PED.IS_PED_IN_COMBAT(h, 0))
                        continue;
                    Vector3 awayDir = m.Position - door;
                    awayDir.Z = 0f;
                    awayDir = Math.Abs(awayDir.X) + Math.Abs(awayDir.Y) < 0.1f ? new Vector3(1f, 0f, 0f) : awayDir.Normalized;
                    TASK.TASK_TURN_PED_TO_FACE_COORD(h, m.Position.X + awayDir.X * 3f, m.Position.Y + awayDir.Y * 3f, m.Position.Z, 2500);   // turned away from it, as the street's own step-round does
                    _ticker.Push(Person.FirstOf(w.Name) + ", thinking", BrawlAfter.MateThought(w.Id + seed), "near", h);
                    _jailMatesAway++;
                    away++;
                    Log.Write("BRAWL after: " + w.Name + ", who fought beside " + Person.FirstOf(j.Name) + ", keeps his head down");
                }
            return true;
        }

        /// F7: the brawler's wife (DEPTH item 6).
        private string JailWordsStatus()
        {
            return BrawlAfterOn ? "words in the street " + _jailWords + " (no room for the scene " + _jailWordsNoScene + "), men who fought beside him turned away " + _jailMatesAway : "off ([Scenes] BrawlAfter)";
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
            bool fighting = PED.IS_PED_IN_COMBAT(p.Handle, 0);
            if (!fighting)
                TASK.TASK_WANDER_STANDARD(p.Handle, 10f, 10);   // (never an order to one in a fight: the reserve's review)
            Spawner.Release(p, !ENTITY.IS_ENTITY_ON_SCREEN(p.Handle) && !fighting);
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
            if (!Payday.Night(InnerLife.Weekday, CLOCK.GET_CLOCK_HOURS()) || !SaloonInTown(_placeTown))   // (the map's saloon too: the review of items 1-13)
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
