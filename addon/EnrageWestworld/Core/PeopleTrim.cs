// The kept people trimmed of the bare (SWEEP item 3; [World] PeopleTrim, on; settings version 460; once a load, in the load's long frame,
// from WestworldScript.LoadStore: about 60 ms on Lex's save, too long for a save's frame). Store.People holds a record for
// everyone the add-on has ever looked at (Store.Get makes one on the first look), and nothing ever removed one: Lex's save had
// 2952, of them 1044 with nothing in them but an id and a name, and the soak grew it a few a day for good. Once a load, a record
// is dropped only when it is bare and nobody's: every saved field at its default but the id, the name and the waking roll
// (nothing said, seen, felt, owed, marked, remembered, written, met or named; no family, no loan, no waking), and its id
// appears nowhere else in the store (no tie, link, plan, kin grudge, affair, debt, vouch, happening, death, rumour, reverie, worry
// seed, resident, letter, another's family or memory or marks). Such a record is made again, bare, the next time anyone looks
// at them, exactly as it was made the first time, with one exception kept by hand: the share of townsfolk who start already
// waking is rolled once a person (Engine.PersonOfFolk, on the engine's own dice), so a bare record dropped and made again would
// be rolled again, and over many saves most would wake. Each id goes into Store.Trimmed with its name and its roll, and
// Store.Find and Store.Get make the record again from it, exactly as it was, the first time anyone asks (code that asks only
// whether a state is kept sees what it saw before). Plain C#; reflection over the saved fields, the field lists found once.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace EnrageWestworld.Core
{
    public static class PeopleTrim
    {
        public static bool On = true;   // [World] PeopleTrim, from the settings
        public const int KeepAtLeast = 1000;   // a small save is left alone: the trim is for the long-played one

        private static readonly Dictionary<Type, FieldInfo[]> Saved = new Dictionary<Type, FieldInfo[]>();

        private static FieldInfo[] SavedFields(Type t)
        {
            FieldInfo[] f;
            if (!Saved.TryGetValue(t, out f))
            {
                f = t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(x => x.GetCustomAttributes(typeof(DataMemberAttribute), false).Length > 0).ToArray();
                Saved[t] = f;
            }
            return f;
        }

        /// Every saved field at its default but the id, the name and the waking roll: nothing of them is kept but who they are.
        public static bool Bare(PersonState st)
        {
            if (st == null || st.Id == null)
                return false;
            foreach (FieldInfo f in SavedFields(typeof(PersonState)))
            {
                if (f.Name == "Id" || f.Name == "Name" || f.Name == "WakeRolled")
                    continue;
                object v = f.GetValue(st);
                if (v == null)
                    continue;
                if (v is ICollection c)
                {
                    if (c.Count > 0)
                        return false;
                    continue;
                }
                if (v is string s)
                {
                    if (s.Length > 0)
                        return false;
                    continue;
                }
                Type t = v.GetType();
                if (t.IsValueType)
                {
                    if (!v.Equals(Activator.CreateInstance(t)))
                        return false;
                    continue;
                }
                return false;   // any other object (a loan): kept
            }
            return true;
        }

        /// Every id-like string anywhere in the store but the people's own keys and ids: whole strings, what follows a "kind:"
        /// (a mark's "grudge:folk:x"), and the parts of "a|b" keys.
        public static HashSet<string> Referenced(Store s)
        {
            var seen = new HashSet<string>();
            var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
            foreach (FieldInfo f in SavedFields(typeof(Store)))
            {
                object v = f.GetValue(s);
                if (f.Name == "People" && v is Dictionary<string, PersonState> ppl)
                {
                    foreach (PersonState st in ppl.Values)
                        if (st != null)
                            foreach (FieldInfo pf in SavedFields(typeof(PersonState)))
                                if (pf.Name != "Id" && pf.Name != "Name")
                                    Walk(pf.GetValue(st), seen, visited, 0);
                    continue;
                }
                if (f.Name == "Trimmed")
                    continue;   // the trim's own list holds only the trimmed: walked, it would keep each of them for good (the review of item 3)
                Walk(v, seen, visited, 0);
            }
            return seen;
        }

        private static void Walk(object v, HashSet<string> seen, HashSet<object> visited, int depth)
        {
            if (v == null || depth > 12)
                return;
            if (v is string str)
            {
                Add(str, seen);
                return;
            }
            Type t = v.GetType();
            if (t.IsPrimitive || t.IsEnum || v is decimal)
                return;
            if (!t.IsValueType && !visited.Add(v))
                return;
            if (v is IDictionary d)
            {
                foreach (DictionaryEntry e in d)
                {
                    Walk(e.Key, seen, visited, depth + 1);
                    Walk(e.Value, seen, visited, depth + 1);
                }
                return;
            }
            if (v is IEnumerable en)
            {
                foreach (object x in en)
                    Walk(x, seen, visited, depth + 1);
                return;
            }
            foreach (FieldInfo f in SavedFields(t))
                Walk(f.GetValue(v), seen, visited, depth + 1);
        }

        private static void Add(string s, HashSet<string> seen)
        {
            if (string.IsNullOrEmpty(s) || s.Length > 200)
                return;
            seen.Add(s);
            int c = s.IndexOf(':');
            if (c >= 0 && c + 1 < s.Length)
                seen.Add(s.Substring(c + 1));
            if (s.IndexOf('|') >= 0)
                foreach (string part in s.Split('|'))
                    if (part.Length > 0)
                    {
                        seen.Add(part);
                        int c2 = part.IndexOf(':');
                        if (c2 >= 0 && c2 + 1 < part.Length)
                            seen.Add(part.Substring(c2 + 1));
                    }
        }

        /// The bare records nobody refers to, taken out; each kept in Store.Trimmed (name and roll) to be made again. Returns how many went.
        public static int Prune(Store s)
        {
            if (!On || s == null || s.People == null || s.People.Count <= KeepAtLeast)
                return 0;
            List<string> bare = s.People.Where(kv => kv.Value != null && Bare(kv.Value)).Select(kv => kv.Key).ToList();
            if (bare.Count == 0)
                return 0;
            HashSet<string> refs = Referenced(s);
            int n = 0;
            foreach (string id in bare)
            {
                if (refs.Contains(id))
                    continue;
                s.TrimmedAdd(s.People[id]);   // its name and its roll kept: Find and Get make it again as it was (Store.Untrim)
                s.People.Remove(id);
                n++;
            }
            s.PeopleTrimmed += n;
            return n;
        }

        private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
        {
            public static readonly ReferenceEqualityComparer Instance = new ReferenceEqualityComparer();
            public new bool Equals(object a, object b) { return ReferenceEquals(a, b); }
            public int GetHashCode(object o) { return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(o); }
        }
    }
}
