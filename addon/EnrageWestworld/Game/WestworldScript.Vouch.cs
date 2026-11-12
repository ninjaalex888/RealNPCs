// Vouch for a prisoner, the game side (FOURTEENHOUR reserve R22; Core/Vouch.cs; [Scenes] Vouch). At item 12's jail door
// (WestworldScript.BrawlAfter.cs: the morning after a brawl, his wife or his mother waits there) J has a third row: vouch for
// him. He's out now, bound over to keep the peace a week, you his surety for five dollars. While a week runs, a look every 5 s:
// seen in a fight near you on the side that began it (the side set upon is excused), or tied up by the law again, and his word
// is broken; else the week's own word, fixed when you stood for him (Vouch.BreaksOn), or the week out and kept. Its end is told
// the next time you're in his town: broken, your five dollars to the county, the town's word, and that sheriff won't take your
// word for a month (the row greyed); kept, he's fonder of you and the town hears it. Runners: the J menu (seen), the jail door
// (built, item 12, not yet seen), the news box and hints (seen). Nobody walks anywhere new; nothing runs a frame.
using System.Collections.Generic;
using System.Linq;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private int _vouchLookAt, _vouchStood, _vouchBroke, _vouchKept;
        private string _vouchLast;

        private bool VouchOn { get { return Vouch.On && BrawlAfterOn; } }

        /// The vouch row's grey reason in this town, or null when the sheriff takes your word.
        private string VouchWhy(string town)
        {
            if (_store == null || town == null)
                return "nowhere near a sheriff";
            int kills, kind;
            string standing = _engine.StandingOf(town, _store.LastGameMin, out kills, out kind);
            return Vouch.Why(standing, Vouch.NoWord(_store.Vouches, town, _store.LastGameMin));
        }

        /// J at the jail door: "Vouch for him."
        private void VouchDo(Jailed j)
        {
            if (j == null || !VouchOn || j.Paid || VouchWhy(j.Town) != null || _store.Vouches == null)
                return;
            Ped me = Game.Player.Character;
            long g = _store.LastGameMin;
            Person p = j.P;
            float chance = Vouch.BreakChance(p != null ? p.Personality : null, p != null ? p.Quirks : null, p != null && p.Family == "married");
            var v = new Vouched { Id = j.Id, Name = j.Name, Town = j.Town, At = g, BreaksOn = Vouch.BreaksOn(j.Id, g / 1440, chance), Test = j.Test };
            _store.Vouches.Add(v);
            j.Paid = true;   // he's out on your word: no fine, and no sore walk out at noon
            j.ReleaseAt = Game.GameTime + 8000;
            _vouchStood++;
            if (j.Waiter != null && j.Waiter.Exists())
            {
                SpeechCaption(Grammar.UpperFirst(j.Waits.Replace("his ", Person.FirstOf(j.Name) + "'s ")), Vouch.ThanksLine(j.Id), j.Waiter.Handle, 4500, true, false);
                if (!j.Test)
                    RecordEvent("surety", "player", "you", j.Id, j.Name, j.Waiter.Position, me.Handle, me.Handle, j.Waiter.Handle, Interact.SuretyOutcome);   // its own kind: a kindness, not a debt (SEAMS item 1; always, the review: under FineOwes off it was a "paidfine" that later owed)
            }
            Hint(Vouch.BoundHint(Person.FirstOf(j.Name)), 6500);
            NewsPush(j.Town, Vouch.StoodNews(j.Name, j.Id), "news");
            _vouchLast = "you stood surety for " + j.Name + " at " + j.Town;
            Log.Write("VOUCH you stood surety for " + j.Name + " at " + j.Town + ": bound over a week, " + BrawlVerbs.Money(Vouch.SuretyCents) + " if he breaks the peace; the week's own word: " +
                      (v.BreaksOn > 0 ? "a fight on day " + v.BreaksOn : "he keeps it") + " (a " + (int)(chance * 100) + "% man)" + (j.Test ? " (a test)" : ""));
        }

        /// Every 5 s while a week runs or an end waits to be told.
        private void VouchStep(int now)
        {
            if (!VouchOn || now < _vouchLookAt || _store == null || _store.Vouches == null || _store.Vouches.Count == 0)
                return;
            _vouchLookAt = now + Vouch.LookMs;
            long g = _store.LastGameMin;
            List<Vouched> vs = _store.Vouches;
            for (int i = vs.Count - 1; i >= 0; i--)
            {
                Vouched v = vs[i];
                if (v == null)
                {
                    vs.RemoveAt(i);
                    continue;
                }
                if (v.Ended == null)
                {
                    Resident r = Resident.IsResident(v.Id) ? _store.FindResident(v.Id) : null;
                    if (r != null && r.Dead)
                    {
                        v.Ended = "dead";   // nothing to answer for
                        v.EndedAt = g;
                        v.Settled = true;
                        Log.Write("VOUCH " + v.Name + " died inside his week; the bond is nothing now");
                    }
                    else
                    {
                        string end = Vouch.WeekEnd(v, g);
                        if (end == "broke")
                            VouchBroke(v, "word", g);
                        else if (end == "kept")
                        {
                            v.Ended = "kept";
                            v.EndedAt = g;
                            Log.Write("VOUCH " + v.Name + " kept the peace his week out");
                        }
                    }
                }
                if (v.Ended != null && !v.Settled && _placeTown == v.Town && _eventMenu == null && !MISC.GET_MISSION_FLAG())
                    VouchSettle(v);
                if (Vouch.Gone(v, g))
                    vs.RemoveAt(i);
            }
            Vouch.Trim(vs);   // the settled oldest first; past twice the cap, the oldest never told (Core/Vouch.cs; SWEEP item 4)
        }

        private void VouchBroke(Vouched v, string how, long g)
        {
            if (v == null || v.Ended != null)
                return;
            v.Ended = "broke";
            v.How = how;
            v.EndedAt = g;
            _vouchBroke++;
            Log.Write("VOUCH " + v.Name + " broke the peace inside his week (" + how + "); it's told when you're next in " + v.Town);
        }

        /// How his week went, told now you're in his town.
        private void VouchSettle(Vouched v)
        {
            string first = Person.FirstOf(v.Name), seed = v.Id + "|" + v.At;
            v.Settled = true;
            if (v.Ended == "broke")
            {
                v.Paid = MONEY._MONEY_GET_CASH_BALANCE() >= Vouch.SuretyCents;
                if (v.Paid)
                    MONEY._MONEY_DECREMENT_CASH_BALANCE(Vouch.SuretyCents);
                Hint(Vouch.BrokeHint(first, v.How, v.Paid, seed), 9000);
                VouchBrokeRemembered(v);
                NewsPush(v.Town, Vouch.BrokeNews(v.Name, seed), "news");
                _vouchLast = v.Name + " broke his word (" + v.How + ")" + (v.Paid ? "; your " + BrawlVerbs.Money(Vouch.SuretyCents) + " went to the county" : "; you hadn't the money");
            }
            else if (v.Ended == "kept")
            {
                Hint(Vouch.KeptHint(first, seed), 7000);
                NewsPush(v.Town, Vouch.KeptNews(v.Name, seed), "news");
                PersonState st = !v.Test ? _store.Find(v.Id) : null;
                if (st != null)
                    st.Affection = Romance.Clamp(st.Affection + Vouch.KeptFondness, -100, 100);   // the stranger who stood for him
                if (_cfg.SuretyKeptOwes && !v.Test && _engine != null)
                {
                    // kept on your word, he owes you (SEAMS item 10; [Encounters] SuretyKeptOwes): the debt the surety held back while
                    // he might yet break it, the ledger's owing (Interact.Owing: a "paidfine" with you the doer) and the saved memory
                    // with how "surety", so he comes to thank you ("You spoke for me at the jail...")
                    _engine.NoteEvent(new Happening { Kind = Interact.SuretyKept, DoerId = "player", DoerName = "you", TargetId = v.Id, TargetName = v.Name, Town = v.Town, Outcome = "kept his week on your word" });
                    if (OwedMemory(v.Id, v.Town, SecondActs.SuretySub, "surety", 60))   // its own words, never "stepped in for me" (the review)
                        Log.Write("VOUCH " + v.Name + " owes you now, his week kept on your word: the ledger, his memory, and his thanks to come");
                }
                _vouchKept++;
                _vouchLast = v.Name + " kept the peace his week out";
            }
            Log.Write("VOUCH told in " + v.Town + ": " + _vouchLast);
        }

        /// The man who broke his week on your word keeps it (SEAMS item 20; [Encounters] SuretyBrokeRemembered): a memory of his
        /// own doing, with you the one let down (quarrel, Sub "suretybroke", Role doer), so his thoughts come back to it ("Gave my
        /// word to {who} and broke it inside the week"). Before, his week's end was told to you, and his own mind kept nothing.
        /// Guilt toward the one who trusted you is the strongest kind (the broken promise); no scene comes of it.
        private void VouchBrokeRemembered(Vouched v)
        {
            if (!_cfg.SuretyBrokeRemembered || v == null || v.Test || v.Id == null || _store == null || _engine == null)
                return;
            PersonState st = _store.Find(v.Id);
            if (st == null)
                return;
            if (st.Mind == null)
                st.Mind = new List<MindMemory>();
            long g = _store.LastGameMin;
            string you = _engine.KnowsYourName(v.Id) && YourFullName() != null ? Person.FirstOf(YourFullName()) : "the stranger";
            Minds.Note(st.Mind, new MindMemory
            {
                Kind = "quarrel", Sub = "suretybroke", Who = new List<string> { "player" }, Names = new List<string> { you }, Town = v.Town, At = g,
                How = "did", Role = "doer", Feel = -2, Strength = 55, Key = "suretybroke:" + v.Id + ":" + g / 1440
            }, g);
            Log.Write("VOUCH " + v.Name + " won't forget breaking his word to you");
        }

        /// He's at it again where you can see, or the law took him (WestworldScript.BrawlAfter.cs, AfterJailed): his week is broken.
        private void VouchAgain(string id, string how)
        {
            if (!VouchOn || _store == null)
                return;
            Vouched v = Vouch.OpenFor(_store.Vouches, id);
            if (v != null)
                VouchBroke(v, how, _store.LastGameMin);
        }

        /// From BrawlWeekAfter, once a fight: any man in it you vouched for, on the side that began it (side 0: AfterNote puts the
        /// starter's side there). The man set upon and those who stood with him are excused: defending isn't breaking the peace.
        private void VouchSeenFight(int now)
        {
            if (!VouchOn || _store == null || !Vouch.AnyOpen(_store.Vouches))
                return;
            foreach (KeyValuePair<int, int> kv in _afterSide)
            {
                if (kv.Value != 0 || !ENTITY.DOES_ENTITY_EXIST(kv.Key))
                    continue;
                MindWho w = MindKnown(kv.Key, now);
                if (w != null && w.Id != null)
                    VouchAgain(w.Id, "seen");
            }
        }

        /// The Testing row: a man of this town in the cells, his wife (or mother) at the jail door at any hour; pressed again with a
        /// week running, that week ends now by its own word (broken or kept), told at once if you're in his town.
        private string VouchFire(int now, out string said)
        {
            said = null;
            if (!Vouch.On)
                return "off ([Scenes] Vouch)";
            if (!BrawlAfterOn)
                return "off ([Scenes] BrawlAfter, the jail door)";
            if (_placeTown == null || _store == null || _store.Vouches == null)
                return "this wants a town with a sheriff";
            long g = _store.LastGameMin;
            Vouched open = null;
            foreach (Vouched v in _store.Vouches)
                if (v != null && v.Ended == null)
                    open = v;   // the newest
            if (open != null)
            {
                if (open.BreaksOn > 0)
                    VouchBroke(open, "word", g);
                else
                {
                    open.Ended = "kept";
                    open.EndedAt = g;
                }
                _vouchLookAt = 0;
                said = open.Name + "'s week ends now: " + (open.Ended == "broke" ? "he broke the peace" : "he kept it") + (open.Town == _placeTown ? "" : "; it's told when you're in " + open.Town);
                return null;
            }
            if (!_jailDoor.ContainsKey(_placeTown))
                return "the jail door here isn't known yet: walk out of the sheriff's office once";
            if (_jailed.Any(j => j.Town == _placeTown && !j.Paid))
                return "someone waits at the jail door here already: walk up to her";
            Resident pick = null;
            foreach (Resident r in _store.Residents)
            {
                if (r == null || r.Woman || r.Dead || r.Fled || r.Held != null || r.Name == null || _jailed.Any(j => j.Id == r.Id))
                    continue;
                if (pick == null || (r.Town == _placeTown && pick.Town != _placeTown))
                    pick = r;   // a man of this town if there is one
                if (pick.Town == _placeTown)
                    break;
            }
            if (pick == null)
                return "nobody the add-on knows to put in the cells";
            Person p = _engine.PersonOfResident(pick);
            bool married = p != null && p.Family == "married";
            _jailed.Add(new Jailed { Id = pick.Id, Name = pick.Name, Town = _placeTown, Day = g / 1440 - 1, Waits = married ? "his wife" : "his mother", P = p, Test = true });
            said = pick.Name + " is in the cells (a test): " + (married ? "his wife" : "his mother") + " waits at the jail door. Look away from the door and she's there; walk up to her";
            Log.Write("VOUCH test: " + said);
            return null;
        }

        /// F7.
        private string VouchStatus()
        {
            if (!Vouch.On)
                return "off ([Scenes] Vouch)";
            if (!BrawlAfterOn)
                return "off with [Scenes] BrawlAfter (the jail door)";
            int open = 0;
            if (_store != null && _store.Vouches != null)
                foreach (Vouched v in _store.Vouches)
                    if (v != null && v.Ended == null)
                        open++;
            return _vouchStood + " vouched for this session, " + open + " weeks running, " + _vouchBroke + " broken, " + _vouchKept + " kept" +
                   (_placeTown != null && VouchWhy(_placeTown) != null ? "; here: " + VouchWhy(_placeTown) : "") + (_vouchLast != null ? "; last: " + _vouchLast : "");
        }
    }
}
