// Who enRAGE has already looked at: the people it has named (_named) or read the look of (_fpOf, its fingerprints of
// face and clothes, which it uses to know a person again). RealNPCs only reads these, so that it never dresses someone
// enRAGE has already fingerprinted (enRAGE could take the new clothes for a new person, or link the wrong name). If
// they can't be found, the town regulars aren't dressed at all, and the log says so.
using System;
using System.Collections;
using System.Reflection;

namespace EnrageWestworld.InGame
{
    internal sealed partial class Bridge
    {
        private bool _looksResolved;
        private FieldInfo _namedField, _fpOfField;

        /// Both of enRAGE's lists were found, so it can be asked whether it has looked at someone.
        public bool CanTellLooked
        {
            get
            {
                if (!Present)
                    return true;   // standing alone the answer is always no: nobody of enRAGE's has looked at anyone
                ResolveLooks();
                return _namedField != null && _fpOfField != null;
            }
        }

        /// enRAGE has named this person or read their look; yes, to be safe, if it can't be asked.
        public bool Looked(int handle)
        {
            ResolveLooks();
            if (_namedField == null || _fpOfField == null)
                return Present;   // standing alone nobody has: the bodies are ours to clean up
            try
            {
                IDictionary named = _namedField.GetValue(Mod) as IDictionary, prints = _fpOfField.GetValue(Mod) as IDictionary;
                return named == null || prints == null || named.Contains(handle) || prints.Contains(handle);
            }
            catch
            {
                return true;
            }
        }

        /// enRAGE's own print of this body, "MODEL-HASH" (the probe row sets it beside its read; the same form at depth 4), or
        /// null: none, or its read failed (it keeps those as "": the recognition plan's critic, point 6). enRAGE forgets its
        /// prints at times, so one may go and come back.
        public string PrintAt(int handle)
        {
            ResolveLooks();
            if (_fpOfField == null)
                return null;
            try
            {
                IDictionary prints = _fpOfField.GetValue(Mod) as IDictionary;
                string s = prints != null && prints.Contains(handle) ? prints[handle] as string : null;
                return string.IsNullOrEmpty(s) ? null : s;
            }
            catch
            {
                return null;
            }
        }

        /// How many bodies enRAGE holds a print of, and how many of them `ours` says are RealNPCs' (bound to its residents):
        /// whether enRAGE's reads could ever tell RealNPCs' own people again (the critic's point 11).
        public int PrintedAmong(Func<int, bool> ours, out int printed)
        {
            printed = 0;
            ResolveLooks();
            if (_fpOfField == null)
                return 0;
            int n = 0;
            try
            {
                IDictionary prints = _fpOfField.GetValue(Mod) as IDictionary;
                if (prints == null)
                    return 0;
                foreach (DictionaryEntry e in prints)
                {
                    if (string.IsNullOrEmpty(e.Value as string))
                        continue;
                    printed++;
                    if (e.Key is int && ours((int)e.Key))
                        n++;
                }
            }
            catch
            {
            }
            return n;
        }

        private void ResolveLooks()
        {
            if (_looksResolved)
                return;
            _looksResolved = true;
            if (Mod == null)
                return;   // standing alone: nothing of enRAGE's to look at
            try
            {
                _namedField = Mod.GetType().GetField("_named", Any);
                _fpOfField = Mod.GetType().GetField("_fpOf", Any);
            }
            catch (Exception ex)
            {
                Log.Write("enRAGE's list of the people it has looked at couldn't be read: " + ex.Message);
            }
            Log.Write(_namedField != null && _fpOfField != null
                ? "enRAGE's list of the people it has named and fingerprinted found: town regulars are only dressed before enRAGE has looked at them."
                : "enRAGE's list of the people it has looked at wasn't found: town regulars keep their names and lives, but aren't dressed in their own clothes.");
        }
    }
}
