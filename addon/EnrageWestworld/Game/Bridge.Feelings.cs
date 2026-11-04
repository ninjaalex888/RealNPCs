// What enRAGE keeps on how people feel about you, by name and only read: a nemesis's hate and his fear (enRAGE's own
// EffectiveFear), one of its townsfolk's fear, liking and grudge, and for any stranger the grudge he is building
// (its _provoked scores) and his unease (its own UneaseOf). Also enRAGE's nemesis line on that 0-100 scale
// (Balance.GsThreshold), the nemesis after you now, and whether enRAGE's own debug overlay is up (it draws bars of
// its own then). For the bars and the meter (WestworldScript.Feelings.cs); if any of it can't be found, they stay off
// and the log says so.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using EnrageWestworld.Core;
using RDR2.Math;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    internal sealed partial class Bridge
    {
        private bool _feelResolved;
        private MethodInfo _effectiveFear, _uneaseOf;
        private FieldInfo _provokedField, _overlayField, _lineField;
        private PropertyInfo _activeRecord;
        private FieldInfo _provScore, _provRecord, _provSettled, _provAt;   // on enRAGE's Provocation, found from the first one seen

        /// Everything the bars and the meter read was found.
        public bool FeelingsKnown
        {
            get
            {
                ResolveFeelings();
                return _provokedField != null && _effectiveFear != null && _uneaseOf != null;
            }
        }

        /// enRAGE's own debug overlay is up, with bars of its own.
        public bool EnrageOverlayOn()
        {
            ResolveFeelings();
            try
            {
                return _overlayField != null && (bool)_overlayField.GetValue(Mod);
            }
            catch
            {
                return false;
            }
        }

        /// enRAGE's nemesis line on the 0-100 grudge scale (0 if unknown).
        public int GrudgeLine()
        {
            ResolveFeelings();
            try
            {
                return _lineField != null ? Convert.ToInt32(_lineField.GetValue(null)) : 0;
            }
            catch
            {
                return 0;
            }
        }

        /// How this person feels about you, 0..100 each; false if enRAGE has nothing on them.
        public bool FeelingsOf(int handle, out float fear, out float like, out float anger)
        {
            fear = like = anger = 0f;
            if (!FeelingsKnown)
                return false;
            try
            {
                RecordRef rec = RecordAt(handle);
                if (rec != null)
                {
                    fear = Convert.ToSingle(_effectiveFear.Invoke(Mod, new object[] { rec.Raw }));
                    anger = rec.Hate;
                }
                else
                {
                    FolkRef a = AcquaintanceAt(handle);
                    object p = ProvocationOf(handle);
                    fear = Math.Max(a != null ? a.Fear : 0, Convert.ToSingle(_uneaseOf.Invoke(Mod, new object[] { handle })));
                    like = a != null ? a.Like : 0;
                    anger = Math.Max(a != null ? a.Grudge : 0, p != null ? ProvScore(p) : 0f);
                }
                return fear > 0f || like > 0f || anger > 0f;
            }
            catch
            {
                return false;
            }
        }

        /// Strangers near you whose grudge against you is still building (no nemesis yet, not let go), and the nemesis
        /// after you now. Names are left for the game side.
        public List<GrudgeRow> GrudgesAgainstYou(Vector3 me, float range)
        {
            List<GrudgeRow> rows = new List<GrudgeRow>();
            if (!FeelingsKnown)
                return rows;
            try
            {
                IDictionary all = _provokedField.GetValue(Mod) as IDictionary;
                if (all != null)
                    foreach (DictionaryEntry e in all)
                    {
                        int h = (int)e.Key;
                        object p = e.Value;
                        if (p == null || !ProvFields(p) || _provRecord.GetValue(p) != null || (bool)_provSettled.GetValue(p))
                            continue;
                        float score = ProvScore(p);
                        if (score <= 0f || !ENTITY.DOES_ENTITY_EXIST(h) || ENTITY.IS_ENTITY_DEAD(h) ||
                            ENTITY.GET_ENTITY_COORDS(h, true, true).DistanceTo(me) > range)
                            continue;
                        rows.Add(new GrudgeRow { Handle = h, Score = score, At = (int)_provAt.GetValue(p) });
                    }
                RecordRef active = _activeRecord != null ? Wrap(_activeRecord.GetValue(Mod, null)) : null;
                if (active != null && active.IsAlive)
                    rows.Add(new GrudgeRow { Who = active.Name, Score = active.Hate, Nemesis = true });
            }
            catch (Exception ex)
            {
                Fail("GrudgesAgainstYou", ex);
            }
            return rows;
        }

        /// The name enRAGE has given this person, if it has (its _named list, the one Bridge.Looks reads).
        public string EnrageNameAt(int handle, bool evenCast = false)
        {
            if (!evenCast && Spawner.IsCast(handle))
                return null;   // the test cast go by their own names (AcquaintanceAt)
            ResolveLooks();
            try
            {
                IDictionary named = _namedField != null ? _namedField.GetValue(Mod) as IDictionary : null;
                object n = named != null && named.Contains(handle) ? named[handle] : null;
                if (n == null)
                    return null;
                if (n.GetType() != _namedType)
                {
                    _namedType = n.GetType();   // the field looked up once, not on every read (the names are read often since 0.7.24)
                    _namedName = _namedType.GetField("Name", Any);
                }
                return _namedName != null ? _namedName.GetValue(n) as string : null;
            }
            catch
            {
                return null;
            }
        }

        private Type _namedType;
        private FieldInfo _namedName;

        /// The name enRAGE shows for one of its acquaintances (Run 3, item 1; Lex's /goal: "enRAGE's visible name (its card and
        /// prompt) everywhere, its acquaintances included"): its card and its lock-on prompt both read its _named list for the
        /// body, and that can differ from the acquaintance record's Name (a record renamed by this add-on before 0.7.25, whose
        /// look's print brought enRAGE's own name back after a reload: Friedrich Keller, Walter Odell, Tip Pierce in 22 s in
        /// Lex's first session with 0.7.24's fixes). Read for the body they were last found in (FolkRef.LastHandle), only while
        /// that body is still theirs; null otherwise (their record's name then stands).
        public string ShownNameOf(FolkRef f)
        {
            if (f == null || f.LastHandle == 0 || _acqOf == null || Mod == null)
                return null;
            try
            {
                IDictionary acq = _acqOf.GetValue(Mod) as IDictionary;
                if (acq == null || !acq.Contains(f.LastHandle) || !ReferenceEquals(acq[f.LastHandle], f.Raw))
                    return null;   // the game gave that handle to someone else, or enRAGE let them go
            }
            catch
            {
                return null;
            }
            string shown = EnrageNameAt(f.LastHandle, true);
            return string.IsNullOrWhiteSpace(shown) ? null : shown;
        }

        /// The prompt label enRAGE gives a body, set to a name of ours: since 0.7.25 (Run 3, item 1: "nothing of ours written
        /// into enRAGE's records") used only for a body this add-on made for a part (the relative walking up,
        /// KinNameToEnrage), whose identity is ours: the label lives in enRAGE's _named list for the session, never in its
        /// records or its looks' prints. Until 0.7.25 it also kept our name on someone enRAGE had just taken up (Lex,
        /// 2026-10-31: "Jacob Shelby" over his head, "Austin Colby" on the prompt). A direct write of that entry's Name, then
        /// enRAGE's own SetPromptName. True when the entry was there to rename.
        public bool RenameNamedAt(int handle, string name)
        {
            if (Mod == null || string.IsNullOrEmpty(name))
                return false;
            ResolveLooks();
            try
            {
                IDictionary named = _namedField != null ? _namedField.GetValue(Mod) as IDictionary : null;
                object n = named != null && named.Contains(handle) ? named[handle] : null;
                FieldInfo f = n != null ? n.GetType().GetField("Name", Any) : null;
                if (f == null)
                    return false;
                f.SetValue(n, name);
                MethodInfo label = Mod.GetType().GetMethod("SetPromptName", Any, null, new[] { typeof(int), typeof(string) }, null);
                if (label != null)
                    label.Invoke(null, new object[] { handle, name });
                return true;
            }
            catch (Exception ex)
            {
                Fail("RenameNamedAt", ex);
                return false;
            }
        }

        private object ProvocationOf(int handle)
        {
            IDictionary all = _provokedField.GetValue(Mod) as IDictionary;
            return all != null && all.Contains(handle) ? all[handle] : null;
        }

        private float ProvScore(object p)
        {
            return ProvFields(p) ? Convert.ToSingle(_provScore.GetValue(p)) : 0f;
        }

        /// "Forget you" (Lex, 2026-10-30; WestworldScript.Forget.cs): enRAGE's townsfolk forget the player (Typed.AcqForgetYouAll)
        /// and the strangers' grudges building against him go (its _provoked, all but those on a record: its nemeses and
        /// their hate are its own story). Direct writes (CLAUDE.md, "Writing to enRAGE"), then its people marked to be saved.
        /// What was done, or null when the townsfolk couldn't be reached.
        public string ForgetYou()
        {
            if (!Present || _people == null)
                return null;
            int folk, grudges = 0;
            try
            {
                folk = Typed.AcqForgetYouAll(_people.GetValue(Mod, null));
                if (folk < 0)
                    return null;
                if (_peopleDirty != null)
                    _peopleDirty.SetValue(Mod, true);
            }
            catch (Exception ex)
            {
                Fail("forgetting you (the townsfolk)", ex);
                return null;
            }
            try
            {
                ResolveFeelings();
                IDictionary prov = _provokedField != null ? _provokedField.GetValue(Mod) as IDictionary : null;
                if (prov != null)
                {
                    var drop = new List<object>();
                    foreach (DictionaryEntry e in prov)
                        if (e.Value != null && ProvFields(e.Value) && _provRecord.GetValue(e.Value) == null)
                            drop.Add(e.Key);
                    foreach (object k in drop)
                        prov.Remove(k);
                    grudges = drop.Count;
                }
            }
            catch (Exception ex)
            {
                Fail("forgetting you (the strangers' grudges)", ex);
            }
            int towns = 0;
            try
            {
                // the word of you in each town, which frightens the newly met (enRAGE's own full reset clears it too)
                object chron = Mod.GetType().GetProperty("Chronicle", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)?.GetValue(Mod, null);
                IDictionary word = chron != null ? chron.GetType().GetProperty("TownWord")?.GetValue(chron, null) as IDictionary : null;
                if (word != null)
                {
                    towns = word.Count;
                    word.Clear();
                    Mod.GetType().GetField("_worldDirty", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(Mod, true);
                }
            }
            catch (Exception ex)
            {
                Fail("forgetting you (the towns' word)", ex);
            }
            return folk + " townsfolk who knew you, " + grudges + " strangers' grudges and the word of you in " + towns + " towns";
        }

        private bool ProvFields(object p)
        {
            if (_provScore == null)
            {
                Type t = p.GetType();
                _provScore = t.GetField("Score", Any);
                _provRecord = t.GetField("Record", Any);
                _provSettled = t.GetField("Settled", Any);
                _provAt = t.GetField("LastActionAt", Any);
            }
            return _provScore != null && _provRecord != null && _provSettled != null && _provAt != null;
        }

        private void ResolveFeelings()
        {
            if (_feelResolved)
                return;
            _feelResolved = true;
            if (Mod == null)
                return;   // standing alone: nothing of enRAGE's to look at
            try
            {
                Type t = Mod.GetType();
                Type recordType = t.Assembly.GetType("NemesisSystem.NemesisRecord", false);
                _effectiveFear = recordType != null ? t.GetMethod("EffectiveFear", Any, null, new[] { recordType }, null) : null;
                _uneaseOf = t.GetMethod("UneaseOf", Any, null, new[] { typeof(int) }, null);
                _provokedField = t.GetField("_provoked", Any);
                _overlayField = t.GetField("_debugOn", Any);
                _activeRecord = t.GetProperty("ActiveRecord", Any);
                Type balance = t.Assembly.GetType("NemesisSystem.Balance", false);
                _lineField = balance != null ? balance.GetField("GsThreshold", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) : null;
            }
            catch (Exception ex)
            {
                Log.Write("enRAGE's record of how people feel about you couldn't be looked at: " + ex.Message);
            }
            Log.Write(_provokedField != null && _effectiveFear != null && _uneaseOf != null
                ? "enRAGE's record of how people feel about you found: the bars and the grudge meter can show" + (_lineField != null ? "." : " (without the nemesis line on the meter).")
                : "enRAGE's record of how people feel about you wasn't found: the bars and the grudge meter stay off.");
        }
    }
}
