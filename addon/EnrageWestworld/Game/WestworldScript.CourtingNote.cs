// Carry the note, game side (LIVES item 4b; [Scenes] CourtingNote; Core/CourtingNote.cs): a few steps from a man whose courting
// worry (getting worse or at its worst) has a real woman of his town at the other end (a courting WorryLink), he asks; J takes
// his note. Then within a few steps of her (two game days), J gives it: her answer as his worry ends, a warm tie and his worry
// settled when it's yes, both warmer to you. One note at a time, in memory only.
using System.Collections.Generic;
using System.Linq;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private sealed class NoteInHand
        {
            public string FromId, FromName, ToId, ToName, Town;
            public bool Good;
            public long Until;
        }

        private NoteInHand _courtNote;
        private TownEvent _courtAsk;
        private int _courtNext, _courtNotesGiven;
        private readonly HashSet<string> _courtAsked = new HashSet<string>();   // a man asked at most once a day
        private bool _courtGiveOffered;   // offered while she's near: not again till you step away

        private void CourtingNoteStep(int now)
        {
            if (!CourtingNote.On || !_cfg.CourtingNote || _store == null || _engine == null || now < _courtNext || !PromptFree())
                return;
            _courtNext = now + (_courtNote != null ? 2500 : CourtingNote.LookMs);   // a note in hand: looked for her every 2.5 s (the whole-round review)
            long g = _store.LastGameMin, day = g / 1440;
            Ped me = Game.Player.Character;
            if (me == null || !me.Exists())
                return;
            if (_courtNote != null && g > _courtNote.Until)
            {
                Log.Write("COURT note: " + _courtNote.FromName + "'s note for " + _courtNote.ToName + " never given; he'll give it himself, or not");
                _courtNote = null;
            }
            bool herNear = false;
            foreach (int h in _mindNear)
            {
                MindWho w;
                if (!_mindWho.TryGetValue(h, out w) || w.Id == null || w.P == null || !ENTITY.DOES_ENTITY_EXIST(h) || ENTITY.IS_ENTITY_DEAD(h))
                    continue;
                if (new Ped(h).Position.DistanceTo(me.Position) > CourtingNote.Metres)
                    continue;
                if (_courtNote != null && (w.Id == _courtNote.ToId || _engine.WorryIdOf(w.Id) == _courtNote.ToId))
                    herNear = true;
                if (_courtNote != null)
                {
                    if (w.Id != _courtNote.ToId && _engine.WorryIdOf(w.Id) != _courtNote.ToId)
                        continue;
                    if (_courtGiveOffered)
                        return;   // once an approach (the review): stepped away, it's offered again
                    _courtGiveOffered = true;
                    _courtAsk = new TownEvent { Kind = CourtingNote.GiveKind, Town = _placeTown, AId = _courtNote.ToId, AName = _courtNote.ToName, BName = _courtNote.FromName };
                    PromptSet(CourtingNote.GiveKind, _courtAsk, CourtingNote.PromptMs);
                    return;
                }
                if (w.P.Woman || _courtAsked.Contains(w.Id + "|" + day))
                    continue;
                string kind; int stage; bool good;
                if (!InnerLife.WorryNow(w.P, day, out kind, out stage, out good) || kind != "courting" || stage < 1 || stage > 2)
                    continue;
                string wid = _engine.WorryIdOf(w.Id);
                WorryLink link = _store.Links.FirstOrDefault(l => l != null && l.Kind == "courting" && (l.Id == w.Id || l.Id == wid) && l.OtherId != null && l.OtherName != null
                                                                    && InnerLife.ArcFor(l.Id, day) == l.Arc && !_engine.HasDied(l.OtherId));
                if (link == null)
                    continue;
                _courtAsked.Add(w.Id + "|" + day);
                string her = Person.FirstOf(link.OtherName);
                SpeechCaption(Person.FirstOf(w.Name) ?? "He", CourtingNote.Asks(her, w.Id + "|" + day), h, 5500);
                _courtAsk = new TownEvent { Kind = CourtingNote.Kind, Town = _placeTown, AId = w.Id, AName = w.Name, Extra = link.OtherId, BName = link.OtherName, Count = good ? 1 : 0 };
                PromptSet(CourtingNote.Kind, _courtAsk, CourtingNote.PromptMs);
                Log.Write("COURT note: " + w.Name + " has a note for " + link.OtherName + "; J to carry it");
                return;
            }
            if (!herNear)
                _courtGiveOffered = false;   // stepped away from her: offered again next time
        }

        /// J, "Carry his note": yours for two game days.
        private void CourtingNoteTake(TownEvent e)
        {
            if (e == null || e.Kind != CourtingNote.Kind || e.Extra == null)
                return;
            _courtNote = new NoteInHand { FromId = e.AId, FromName = e.AName, ToId = e.Extra, ToName = e.BName, Town = e.Town, Good = e.Count == 1, Until = _store.LastGameMin + CourtingNote.KeepDays * 1440L };
            Hint(Person.FirstOf(e.AName) + "'s note for " + Person.FirstOf(e.BName) + " is in your pocket (two days). Find her in " + (e.Town ?? "town") + ".", 5000);
            Log.Write("COURT note taken: " + e.AName + " -> " + e.BName);
        }

        /// J, "Give her his note": she reads it; her answer as his worry ends.
        private void CourtingNoteGive(TownEvent e)
        {
            NoteInHand n = _courtNote;
            if (e == null || e.Kind != CourtingNote.GiveKind || n == null || n.ToId != e.AId)
                return;
            _courtNote = null;
            long g = _store.LastGameMin;
            string seed = n.FromId + "|" + n.ToId + "|" + g / 1440;
            string answer = CourtingNote.Answer(n.Good, seed);
            int h = 0;
            foreach (int x in _mindNear)
            {
                MindWho w;
                if (_mindWho.TryGetValue(x, out w) && (w.Id == n.ToId || _engine.WorryIdOf(w.Id) == n.ToId) && ENTITY.DOES_ENTITY_EXIST(x))
                {
                    h = x;
                    break;
                }
            }
            if (h != 0)
                SpeechCaption(Person.FirstOf(n.ToName) ?? "She", CourtingNote.Reads(answer, Person.FirstOf(n.FromName), seed), h, 5500, true);
            if (answer == "yes")
            {
                Ties.Note(_store.Ties, n.FromId, n.FromName, n.ToId, n.ToName, n.Town, "flirt", g);   // walking out now
                _engine.SettleWorry(n.FromId);   // his courting comes good
            }
            foreach (string id in new[] { n.FromId, n.ToId })
            {
                PersonState st = _store.Find(id);
                if (st != null)
                    st.Affection = Romance.Clamp(st.Affection + 8, -100, 100);   // both warmer to the one who carried it
            }
            _courtNotesGiven++;
            Log.Write("COURT note given: " + n.ToName + " reads " + n.FromName + "'s note: " + answer);
        }
    }
}
