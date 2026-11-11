// Send word with him, the game side (DEPTH item 10; Core/SendWord.cs; with [Scenes] HorseWatch). The horse watcher's offer (J) gains a
// row while a friend of yours lives in this town: "Take a word to <name> for me instead", the same dime. He says he'll go and walks off
// (40 m, off along the street: once out of sight he is the game's; nobody walked over to a spot), and he's let go. Twenty to fifty game minutes
// on, the friend has it: their remembered talks with you go up one (Allies.Warmth counts them), they're a little fonder of you, and when next near and in view
// they think of it. Then, if he's still about (within 200 m, free), he walks back to you and says it's done, and the friend's thanks,
// and goes his way. A look a second (the frame pass's scheduler; Tick04's own call with it off), in its own guard. Runners: the
// horse watcher's walk-up and J (FOURTEENHOUR R21, built, not yet seen), the J menu (seen). In memory only: nothing new in the save.
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
        private sealed class WordRun
        {
            public Ped Man;
            public string Id, Name, FriendId, FriendName, Seed, Town, Phase = "going";   // going, back
            public long DueMin;
            public int BackUntil;
        }

        private WordRun _word;
        private Action _aSendWord;
        private readonly Dictionary<string, string> _wordThoughts = new Dictionary<string, string>();   // friend id -> their thought of it, when next near
        private int _wordNextLook, _wordsSent, _wordsDelivered, _wordsBack;
        private string _wordLast;

        /// The friend in this town you'd send word to, or null.
        private SendWord.Friend SendWordFriend(string notId = null)
        {
            if (_store == null || _placeTown == null || _cfg == null)
                return null;
            long g = _store.LastGameMin;
            var town = new List<SendWord.Friend>();
            foreach (Resident r in _store.Residents)
            {
                if (r == null || r.Id == null || r.Town != _placeTown || r.Dead || r.Left || r.Fled)
                    continue;
                PersonState st = _store.Find(r.Id);
                bool kind = _store.WorldLog != null && Interact.KindLately(_store.WorldLog, r.Id, g);
                bool mended = _store.WorrySettled != null && _store.WorrySettled.ContainsKey(r.Id);
                bool saved = _store.WorldLog != null && Interact.SavedLately(_store.WorldLog, r.Id, g);
                int warmth = Allies.Warmth(0f, _cfg.AlliesLiking, kind, mended, st != null ? st.Talks : 0, st != null && st.KnowsName, false, saved);
                if (warmth >= 2)
                    town.Add(new SendWord.Friend { Id = r.Id, Name = r.Name, Warmth = warmth });
            }
            return SendWord.Pick(town, notId, _placeTown + "|" + g / 1440);
        }

        /// The horse watcher's offer menu gains the word's row while a friend of yours lives here (WestworldScript.Trials.cs).
        private void SendWordRow(List<InteractOption> rows)
        {
            if (_word != null || rows == null)
                return;
            SendWord.Friend f = SendWordFriend(_watch != null ? _watch.Id : null);
            if (f != null)
                rows.Insert(1, SendWord.Row(Person.FirstOf(f.Name), MONEY._MONEY_GET_CASH_BALANCE()));
        }

        /// J's row: the dime, and he goes with the word.
        private void SendWordGo(WatchRun w)
        {
            SendWord.Friend f = SendWordFriend(w != null ? w.Id : null);
            Ped me = Game.Player.Character;
            if (w == null || w.Man == null || !w.Man.Exists() || f == null || MONEY._MONEY_GET_CASH_BALANCE() < HorseWatch.Cents || PED.IS_PED_IN_COMBAT(w.Man.Handle, 0))
            {
                WatchEnd("no word to carry after all", false);
                return;
            }
            MONEY._MONEY_DECREMENT_CASH_BALANCE(HorseWatch.Cents);
            int h = w.Man.Handle;
            string seed = w.Seed + "|word";
            Feed.Say(Person.FirstOf(w.Name), SendWord.GoesLine(Person.FirstOf(f.Name), seed), Speech.When(h, "thanks", me.Handle), h, toYou: true);
            Vector3 dir = w.Man.Position - me.Position;
            dir.Z = 0f;
            dir = Math.Abs(dir.X) + Math.Abs(dir.Y) < 0.1f ? new Vector3(1f, 0f, 0f) : dir.Normalized;
            Vector3 spot = w.Man.Position + dir * 40f;
            PED.SET_PED_KEEP_TASK(h, false);
            TASK.TASK_FOLLOW_NAV_MESH_TO_COORD(h, spot.X, spot.Y, spot.Z, 1.2f, -1, 1f, 0, 0f);   // off along the street, away from you
            _watch = null;   // the watch is off: he carries the word instead
            if (_eventPrompt == HorseWatch.Kind)
                PromptClear();
            _word = new WordRun { Man = w.Man, Id = w.Id, Name = w.Name, FriendId = f.Id, FriendName = f.Name, Seed = seed, Town = _placeTown, DueMin = _store.LastGameMin + SendWord.Minutes(seed) };
            _wordsSent++;
            _wordLast = w.Name + " carries your word to " + f.Name;
            Log.Write("SENDWORD " + _wordLast + " for a dime (there in " + (_word.DueMin - _store.LastGameMin) + " game minutes)");
        }

        private void SendWordStep(int now)
        {
            if (!HorseWatch.On || now < _wordNextLook || (_word == null && _wordThoughts.Count == 0))
                return;
            _wordNextLook = now + 1000;
            Safe("send word", _aSendWord ?? (_aSendWord = () => SendWordLook(_now)));   // a kept delegate: no closure a frame (review 1)
        }

        private void SendWordLook(int now)
        {
            WordRun r = _word;
            Ped me = Game.Player.Character;
            if (r != null)
            {
                bool gone = r.Man == null || !r.Man.Exists() || r.Man.IsDead;
                bool atYou = !gone && PED.IS_PED_IN_COMBAT(r.Man.Handle, 0);   // fighting anyone: the game's (review 1)
                if (r.Phase == "going" && _store.LastGameMin >= r.DueMin)
                {
                    PersonState st = _store.Find(r.FriendId);
                    if (st != null)
                    {
                        st.Talks++;   // as good as a word with you: what Allies.Warmth counts
                        st.Affection = Romance.Clamp(st.Affection + 3, -100, 100);   // and fonder of the stranger who thought of them (as the watcher's +5 for work)
                    }
                    if (_wordThoughts.Count > 50)
                        _wordThoughts.Clear();
                    _wordThoughts[r.FriendId] = SendWord.Thought(Person.FirstOf(r.Name), r.Seed);
                    _wordsDelivered++;
                    Log.Write("SENDWORD " + r.FriendName + " has your word, carried by " + r.Name);
                    r.Phase = "back";
                    r.BackUntil = now + SendWord.BackMs;
                    if (!gone && !atYou && !_director.Involves(r.Man.Handle) && !NotFree(r.Man.Handle) && MayDirectWhyNot(r.Man) == null && r.Man.Position.DistanceTo(me.Position) < 200f && !PED.IS_PED_ON_MOUNT(me.Handle))
                        TASK.TASK_GO_TO_ENTITY(r.Man.Handle, me.Handle, SendWord.BackMs, 2.0f, 1.0f, 0f, 0);   // back to you for his thanks
                    else
                        _word = null;   // he's gone his way, or something else has him: the word got there all the same
                }
                else if (r.Phase == "back")
                {
                    if (gone || atYou || _director.Involves(r.Man.Handle) || now > r.BackUntil)
                        _word = null;   // let go at once (one at you is the game's)
                    else if (r.Man.Position.DistanceTo(me.Position) < 2.6f)
                    {
                        TASK.TASK_TURN_PED_TO_FACE_ENTITY(r.Man.Handle, me.Handle, 3000, 0f, 0f, 0f);
                        SpeechCaption(Person.FirstOf(r.Name), SendWord.BackLine(Person.FirstOf(r.FriendName), r.Seed), r.Man.Handle, 3500, true, true);
                        WanderLater(r.Man.Handle, Game.GameTime + 3500);
                        _wordsBack++;
                        Log.Write("SENDWORD " + r.Name + " is back: your word is with " + r.FriendName);
                        _word = null;
                    }
                }
                else if (r.Phase == "going" && (gone || atYou))
                    Log.Detail("SENDWORD " + r.Name + " is out of our hands on his way (the word still gets there)");
            }
            // the friend's thought of it, when they're near and in view
            if (_wordThoughts.Count > 0)
                foreach (int h in _mindNear)
                {
                    MindWho w;
                    string t;
                    if (!_mindWho.TryGetValue(h, out w) || w.Id == null || !_wordThoughts.TryGetValue(w.Id, out t) || !ENTITY.DOES_ENTITY_EXIST(h) || !NodInView(me, new Ped(h)))
                        continue;
                    _ticker.Push(Person.FirstOf(w.Name) + ", thinking", t, "near", h);
                    _wordThoughts.Remove(w.Id);
                    break;
                }
        }

        private string SendWordStatus()
        {
            if (!HorseWatch.On)
                return "off ([Scenes] HorseWatch)";
            SendWord.Friend f = SendWordFriend();
            return "sent " + _wordsSent + ", delivered " + _wordsDelivered + ", back with the thanks " + _wordsBack + (_word != null ? "; " + _word.Name + " is " + _word.Phase : "") +
                   "; a friend to send to here: " + (f != null ? f.Name + " (warmth " + f.Warmth + ")" : "none") + (_wordLast != null ? "; the last: " + _wordLast : "");
        }
    }
}
