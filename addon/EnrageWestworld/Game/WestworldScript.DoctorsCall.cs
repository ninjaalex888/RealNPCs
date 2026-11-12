// The doctor's call, game side (LIVES item 4a; [Scenes] DoctorsCall; Core/DoctorsCall.cs): within a few steps of the town's
// doctor (a resident keeping the doctor's post), once a town a game day, he names a house with a fever its people can't pay
// his call for, and J for twelve seconds lets you pay it: the worry settled, a kindness he saw.
using System.Collections.Generic;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private int _doctorsCallNext, _doctorsCallsPaid;
        private readonly HashSet<string> _doctorsCallDays = new HashSet<string>();
        private Ped _doctorsCallDoctor;

        private void DoctorsCallStep(int now)
        {
            if (!DoctorsCall.On || !_cfg.DoctorsCall || _store == null || _engine == null || _placeTown == null || now < _doctorsCallNext || _eventPrompt != null)
                return;
            _doctorsCallNext = now + DoctorsCall.LookMs;
            long day = _store.LastGameMin / 1440;
            if (_doctorsCallDays.Contains(_placeTown + "|" + day))
                return;
            Ped me = Game.Player.Character;
            if (me == null || !me.Exists())
                return;
            Ped doctor = null;
            Resident docR = null;
            foreach (int h in _mindNear)
            {
                MindWho w;
                if (!_mindWho.TryGetValue(h, out w) || w.Id == null || !ENTITY.DOES_ENTITY_EXIST(h) || ENTITY.IS_ENTITY_DEAD(h))
                    continue;
                Ped p = new Ped(h);
                if (p.Position.DistanceTo(me.Position) > DoctorsCall.DoctorMetres)
                    continue;
                Resident r = _store.FindResident(w.Id);
                Post post = r != null ? _engine.PostOf(r) : null;
                PostKind k = post != null ? Posts.Of(post.Model) : null;
                if (k != null && k.Kind == "doctor")
                {
                    doctor = p;
                    docR = r;
                    break;
                }
            }
            if (doctor == null)
                return;
            Resident sick = null;
            Person sp = null;
            foreach (Resident r in _store.Residents)
            {
                if (r == null || r.Town != _placeTown || !r.Regular || r.Id == docR.Id || _engine.HasDied(r.Id))
                    continue;
                Person p = _engine.PersonOfResident(r);
                if (DoctorsCall.Fits(p, day))
                {
                    sick = r;
                    sp = p;
                    break;
                }
            }
            _doctorsCallDays.Add(_placeTown + "|" + day);   // once a town a day, a house or none
            if (sick == null)
                return;
            string child = DoctorsCall.Sick(sp, day);
            string surname = sick.Name != null && sick.Name.Contains(" ") ? sick.Name.Substring(sick.Name.LastIndexOf(' ') + 1) : null;
            string seed = sick.Id + "|doc|" + day;
            SpeechCaption(Person.FirstOf(docR.Name) ?? "The doctor", DoctorsCall.Mention(child, surname != null ? "the " + surname + "s" : null, seed), doctor.Handle, 5500);
            _doctorsCallDoctor = doctor;
            PromptSet(DoctorsCall.Kind, new TownEvent { Kind = DoctorsCall.Kind, Town = _placeTown, AId = sick.Id, AName = sick.Name, Extra = child, BName = docR.Name, Count = DoctorsCall.Cents }, DoctorsCall.PromptMs);
            Log.Write("DOCTOR'S CALL: " + docR.Name + " names " + sick.Name + "'s house (" + child + ", fever) they can't pay his call for; J to pay it");
        }

        /// J, "Pay for his call": the two dollars, the worry settled, the kindness seen.
        private void DoctorsCallPay(TownEvent e)
        {
            if (e == null || e.Kind != DoctorsCall.Kind)
                return;
            int cash = MONEY._MONEY_GET_CASH_BALANCE();
            if (cash < e.Count)
            {
                Hint("You haven't the money.", 2500);
                return;
            }
            MONEY._MONEY_DECREMENT_CASH_BALANCE(e.Count);
            _engine.SettleWorry(e.AId);   // the house's thoughts: a stranger paid the doctor; the fever breaks (the fever worry's Settled lines)
            Ped me = Game.Player.Character, doc = _doctorsCallDoctor;
            int dh = doc != null && doc.Exists() ? doc.Handle : 0;
            string seed = e.AId + "|docpaid|" + _store.LastGameMin / 1440;
            if (dh != 0)
            {
                SpeechCaption(Person.FirstOf(e.BName) ?? "The doctor", DoctorsCall.Paid(seed), dh, 4500, true);
                _ticker.Push((Person.FirstOf(e.BName) ?? "The doctor") + ", thinking", DoctorsCall.Thought(seed), "near", dh);
            }
            RecordEvent("charity", "player", "you", e.AId, e.AName, me.Position, me.Handle, me.Handle, dh, "paid the doctor's call to a feverish child");   // a kindness the doctor saw
            _doctorsCallsPaid++;
            Log.Write("DOCTOR'S CALL paid: $2 for " + e.AName + "'s house (" + e.Extra + "); the worry settled");
        }
    }
}
