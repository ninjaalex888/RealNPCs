// The fondness interactions, the game side (SIXDAY item 1; Core/Fondness.cs has the choosing and the words). Two things
// live here: a kindness seen (from RecordEvent: the named people who saw you do a kindness to someone else, from within
// 12 m, think better of you: enRAGE's fondness through its own method, our residents' affection and a memory of having
// seen you do right), and whiskey for the house (Talk, in a saloon: the room looks over and cheers as a round scene's
// room does, and each named person treated is fonder of you, the drunk most, with a "round" happening of their own so the
// thanks and the trust that follow a drink stood follow this one). [Encounters] FondnessActs, KindnessSeen,
// KindnessSeenLiking. F7 "TEST fondness:"; log "FOND".
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
        private readonly List<int> _kindSeenHandles = new List<int>();   // the witnesses of the happening being written, from near (RecordEvent)
        private bool _kindSeenOff;   // the house's own per-person happenings are no fresh kindness to be seen (one sighting for the round, not eight)
        private bool _kindSeenFailed;
        private int _kindSeenTimes, _kindSeenPeople, _houseRounds, _houseCheered, _houseTreated, _coverDebts, _campfireRemembered;
        private string _fondLast;

        /// From RecordEvent, once the witnesses are known: a kindness of the player's, seen from near by named people.
        private void KindnessSeenBy(Happening e, List<int> handles)
        {
            if (!_cfg.KindnessSeen || _kindSeenOff || e == null || e.Test || handles == null || handles.Count == 0 || _engine == null)
                return;
            try
            {
                long g = _store.LastGameMin;
                int liked = 0;
                string names = null;
                foreach (int h in handles)
                {
                    if (!ENTITY.DOES_ENTITY_EXIST(h) || ENTITY.IS_ENTITY_DEAD(h))
                        continue;
                    Ped p = new Ped(h);
                    string id, name;
                    if (!LastingId(p, out id, out name) || id == null || id == e.TargetId)
                        continue;
                    if (SightRegard(p, id) == "hostile")
                        continue;   // someone who hates him reads nothing kind into it
                    FolkRef ac = _bridge != null ? _bridge.AcquaintanceAt(h) : null;
                    if (ac != null && _cfg.KindnessSeenLiking > 0)
                        _bridge.LikeMore(ac.Id, _cfg.KindnessSeenLiking);   // enRAGE's own method for its own townsfolk (an audited write)
                    if (!_engine.KindnessSeen(id, name, e.Kind, e.TargetName, g))
                        continue;
                    liked++;
                    if (names == null)
                        names = Person.FirstOf(name);
                    else if (liked <= 3)
                        names += ", " + Person.FirstOf(name);
                }
                if (liked == 0)
                    return;
                _kindSeenTimes++;
                _kindSeenPeople += liked;
                _fondLast = liked + " saw you " + Fondness.SeenWord(e.Kind) + " " + (e.TargetName ?? "someone");
                Log.Write("FOND seen: " + liked + " saw you " + Fondness.SeenWord(e.Kind) + " " + (e.TargetName ?? "someone") + " and warm to you for it (" + names + (liked > 3 ? "..." : "") + ")");
            }
            catch (Exception ex)
            {
                if (!_kindSeenFailed)
                    Log.Write("FOND seen failed (once): " + ex.Message);
                _kindSeenFailed = true;
            }
        }

        /// How many other grown people stand within the bar's reach of this one: whether "whiskey for the house" is a round at all.
        private int HouseCompany(int target)
        {
            Ped me = Game.Player.Character;
            Vector3 at = ENTITY.GET_ENTITY_COORDS(target, true, true);
            int n = 0;
            foreach (Ped p in Peds.All())
            {
                if (p == null || !p.Exists())
                    continue;
                int h = p.Handle;
                if (h == target || h == me.Handle || p.Position.DistanceTo(at) > Fondness.HouseMetres || ENTITY.IS_ENTITY_DEAD(h) || !PED.IS_PED_HUMAN(h) || !IsGrownUp(p))
                    continue;
                n++;
            }
            return n;
        }

        /// He stood the house a drink and the one he said it to has answered for the room: the room looks over and
        /// cheers (the round scene's way), and each named person treated is fonder of him, with a "round" of their own
        /// in the world log (the same within the hour is said, not counted again: no buying a room's heart twice an hour).
        private void HouseRound(TalkMenu t, int now)
        {
            Ped me = Game.Player.Character;
            if (t == null || !ENTITY.DOES_ENTITY_EXIST(t.Handle))
                return;
            Vector3 bar = ENTITY.GET_ENTITY_COORDS(t.Handle, true, true);
            long g = _store.LastGameMin;
            string seed = t.Id + "|house|" + now / 1000;
            int cheered = 0, treated = 0;
            bool thought = false;
            _kindSeenOff = true;
            try
            {
                foreach (Ped p in Peds.All().Where(x => x != null && x.Exists() && !x.IsDead && x.Handle != me.Handle && x.Handle != t.Handle && x.Position.DistanceTo(bar) < Fondness.HouseMetres)
                                            .OrderBy(x => x.Position.DistanceTo(bar)).ToList())
                {
                    if (cheered >= Fondness.HouseMost)
                        break;
                    int h = p.Handle;
                    if (!PED.IS_PED_HUMAN(h) || !IsGrownUp(p) || !Natives.Bystander(p, me.Handle, IsRecord(h)) || NotFree(h) || MayDirectWhyNot(p) != null)
                        continue;
                    string pid, pname, temper;
                    bool lasting = LastingId(p, out pid, out pname, out temper);
                    if (lasting && SightRegard(p, pid) == "hostile")
                        continue;   // no cheer from someone who hates him, and nothing written over their grudge
                    TASK.TASK_LOOK_AT_ENTITY(h, me.Handle, 4000, 0, 51, 0);
                    bool woman = !PED.IS_PED_MALE(h);
                    string dict = woman ? Gestures.FemaleSpeaker : Gestures.MaleSpeaker;
                    if (!PED.IS_PED_USING_ANY_SCENARIO(h) && STREAMING.HAS_ANIM_DICT_LOADED(dict))
                    {
                        string[] clips = woman ? new[] { "happy_cheer_f_001", "happy_cheer_f_002", "happy_cheer_f_003", "happy_laugh_f_001" } : new[] { "happy_cheer_f_001", "happy_cheer_f_002", "happy_cheer_f_003", "happy_laugh_f_002" };
                        TASK.TASK_PLAY_ANIM(h, dict, clips[Voice.Hash(seed + h) % clips.Length], 4f, -4f, -1, 8 | 16, 0f, false, 0, false, "", false);
                    }
                    if (cheered < 2 && !Speech.Busy(h))
                        Speech.SayTag(h, "cheer", me.Handle);   // two voices are a cheer; eight are a din
                    cheered++;
                    if (!lasting || pid == null || Interact.Again(_store.WorldLog, pid, "round", g))
                        continue;
                    bool drunk = InHisCups(p, null);
                    RecordEvent("round", "player", "you", pid, pname, p.Position, me.Handle, me.Handle, h, "whiskey for the house");   // what late thanks reads (a drink)
                    FolkRef ac = _bridge != null ? _bridge.AcquaintanceAt(h) : null;
                    if (ac != null)
                        _bridge.LikeMore(ac.Id, drunk ? Fondness.HouseDrunkLiking : Fondness.HouseLiking);   // enRAGE's own method for its own townsfolk
                    _engine.StoodTheHouse(pid, pname, drunk, g);
                    treated++;
                    if (!thought)
                    {
                        thought = true;
                        _ticker.Push(Person.FirstOf(pname) + ", thinking", Fondness.HouseThought(drunk, temper, seed + "|" + h), "near", h);
                    }
                }
            }
            finally
            {
                _kindSeenOff = false;
            }
            _houseRounds++;
            _houseCheered += cheered;
            _houseTreated += treated;
            _fondLast = "whiskey for the house: " + cheered + " cheered";
            Log.Write("FOND round: you stood the house a drink" + (_placeTown != null ? " in " + _placeTown : "") + "; " + cheered + " cheered, " + treated + " of them people you know" + (cheered == 0 ? " (nobody free at the bar)" : ""));
        }

        /// The creditor of the debt scene playing (its first, pressing its second, who is alive and still in it): what the
        /// stranger can cover, and for whom. Nothing once the scene is rewritten (a step-in, a cover already paid).
        private bool DebtCoverable(int target, out int dollars, out int debtor)
        {
            dollars = 0;
            debtor = 0;
            if (!_cfg.FondnessActs || _director == null || _director.CurrentKind != "debt" || _director.CurrentIntruded)
                return false;
            Scene s = _director.CurrentScene;
            Ped a = _director.CastAt(0), b = _director.CastAt(1);
            if (s == null || s.Amount <= 0 || a == null || b == null || !a.Exists() || !b.Exists() || a.Handle != target || b.IsDead)
                return false;
            dollars = s.Amount;
            debtor = b.Handle;
            return true;
        }

        /// Whether the creditor's scene is still the one the list opened on.
        private bool DebtStillPressed(TalkMenu t)
        {
            int dollars, debtor;
            return DebtCoverable(t.Handle, out dollars, out debtor) && debtor == t.Debtor;
        }

        /// He covered what the man owed; the creditor has answered and been paid: the debtor's kindness (a
        /// settled debt for the thanks, the trust and the mind; their own debt worry, when that's what it is, settled for
        /// the rest of its run), the creditor a little fonder, and the scene's end: the creditor walks off, the debtor
        /// turns to you with their thanks and goes.
        private void CoverDebt(TalkMenu t, int now)
        {
            Ped me = Game.Player.Character;
            long g = _store.LastGameMin;
            string seed = t.Id + "|cover|" + now / 1000;
            string dId = null, dName = null, dTemper = null;
            bool settled = false, lasting = false;
            if (t.Debtor != 0 && ENTITY.DOES_ENTITY_EXIST(t.Debtor) && !ENTITY.IS_ENTITY_DEAD(t.Debtor))
            {
                Ped d = new Ped(t.Debtor);
                lasting = LastingId(d, out dId, out dName, out dTemper) && dId != null;
                if (lasting)
                {
                    RecordEvent("help", "player", "you", dId, dName, d.Position, me.Handle, me.Handle, t.Debtor, Thanks.DebtOutcome);   // what late thanks reads: a debt settled
                    FolkRef ac = _bridge != null ? _bridge.AcquaintanceAt(t.Debtor) : null;
                    if (ac != null)
                        _bridge.LikeMore(ac.Id, Fondness.CoverDebtorLiking);   // enRAGE's own method for its own townsfolk
                    _engine.DebtCovered(dId, dName, g);
                    MindWho w = MindKnown(t.Debtor, now);
                    string kind;
                    int stage;
                    bool good;
                    if (w != null && w.P != null && InnerLife.WorryNow(w.P, g / 1440, out kind, out stage, out good) && kind == "debt" && stage <= 2)
                    {
                        _engine.SettleWorry(dId);   // their own debt worry: the rest of its run reads as ended well (as the menu's settle)
                        settled = true;
                    }
                }
            }
            FolkRef ca = _bridge != null ? _bridge.AcquaintanceAt(t.Handle) : null;
            if (ca != null)
                _bridge.LikeMore(ca.Id, Fondness.CoverLiking);
            _engine.PaidByYou(t.Id, t.Name);
            string thanks = Fondness.CoverThanks(dTemper, t.Debtor != 0 && ENTITY.DOES_ENTITY_EXIST(t.Debtor) && !PED.IS_PED_MALE(t.Debtor), seed);
            bool rewritten = _director.Rewrite(Scenes.DebtCovered(_director.CurrentInfo ?? new SceneCast(), _director.CurrentSeed ?? seed, thanks), "the stranger covered the debt ($" + t.CoverDollars + "): the creditor goes paid, the debtor thanks him");
            t.KeepOrders = true;   // the scene's walk-off stands
            t.CloseAt = now + 1500;
            _coverDebts++;
            _fondLast = "a debt of $" + t.CoverDollars + " covered";
            Log.Write("FOND debt covered: you paid " + t.Name + " the $" + t.CoverDollars + (dName != null ? " " + dName + " owed" : " owed") + (lasting ? "" : " (the debtor is nobody RealNPCs knows: no thanks to come)") +
                      (settled ? "; their debt worry is settled" : "") + (rewritten ? "" : "; the scene had already turned"));
        }

        private string FondnessStatus()
        {
            return (_cfg.FondnessActs ? "Talk's extra acts on (whiskey for the house, a gift, condolences)" : "Talk's extra acts off ([Encounters] FondnessActs)") +
                   "; kindness seen " + (_cfg.KindnessSeen ? "on (+" + _cfg.KindnessSeenLiking + " fondness, within " + (int)Fondness.SeenMetres + " m)" : "off") +
                   "; this session: " + _kindSeenTimes + " kindnesses seen by " + _kindSeenPeople + " people, " + _houseRounds + " rounds for the house (" + _houseCheered + " cheered, " + _houseTreated + " treated), " + _coverDebts + " debts covered, " + _campfireRemembered + " fires remembered; " + EscortStatus() +
                   (_fondLast != null ? "; last: " + _fondLast : "");
        }
    }
}
