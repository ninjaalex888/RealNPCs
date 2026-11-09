// People the add-on puts into the world (stalkers, revenants, wedding parties). Never blocks: the model is
// requested and the spawn simply returns null until it has streamed in. Never during missions or fades.
// Everything spawned is remembered and removed when the add-on stops. People already in the world who are taken
// over for a while (a townsperson turned stalker) count as ours too, but are only ever handed back, never deleted.
// The Testing page's test cast are spawned but never "ours": the game keeps them (mission entities), nobody holds
// them, and any feature may take them as it would a townsperson (WestworldScript.TestCast.cs).
using System.Collections.Generic;
using System.Linq;
using RDR2;
using RDR2.Math;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    internal static class Spawner
    {
        private static readonly HashSet<int> Ours = new HashSet<int>();
        private static readonly HashSet<int> Borrowed = new HashSet<int>();
        private static readonly HashSet<int> Cast = new HashSet<int>();   // the test cast's bodies

        /// Takes over someone already in the world, so the game keeps them and nothing else of ours directs them.
        /// True if the game now treats them as ours (a mission entity).
        public static bool Borrow(Ped p)
        {
            int h = p.Handle;
            Light.Remove(h);   // a handle the game reused: whatever it was before, it's taken over fully now
            Quiet.Remove(h);
            ENTITY.SET_ENTITY_AS_MISSION_ENTITY(h, true, true);
            Ours.Add(h);
            Borrowed.Add(h);
            return ENTITY.IS_ENTITY_A_MISSION_ENTITY(h);
        }

        private static readonly HashSet<int> Light = new HashSet<int>();
        private static readonly HashSet<int> Quiet = new HashSet<int>();   // handed back to orders enRAGE has just given them

        /// The next hand-back of this one leaves their tasks alone (enRAGE has just given them orders of its own: a
        /// fight, an interrogation): only our hold on them goes.
        public static void LeaveTasks(int handle)
        {
            if (Borrowed.Contains(handle))
                Quiet.Add(handle);
        }

        /// One of enRAGE's records, taken over lightly: ours to direct, but its mission-entity state is enRAGE's, and on
        /// hand-back it is never marked "no longer needed" (that could let the game clear a ped enRAGE spawned).
        public static void BorrowLight(Ped p)
        {
            int h = p.Handle;
            Ours.Add(h);
            Borrowed.Add(h);
            Light.Add(h);
        }

        public static bool IsBorrowed(int handle)
        {
            return Borrowed.Contains(handle);
        }

        /// Hands someone borrowed back with no orders of ours (aimed at: the game's own answer to the gun takes over):
        /// unblocked, KEEP_TASK off, nothing cleared and no wander given.
        public static void GiveToGame(int h)
        {
            if (!Borrowed.Contains(h))
                return;
            Quiet.Add(h);
            GiveBack(h);   // out of ours, stood up, unblocked, KEEP_TASK off, no longer needed; quiet: no tasks cleared, no wander
        }

        /// Hands someone borrowed back to the game, going about their day again (quiet: with whatever orders they have now;
        /// still KEEP_TASK off and no longer needed, so one fully borrowed is never left a mission entity nobody holds).
        private static unsafe void GiveBack(int h)
        {
            Ours.Remove(h);
            Borrowed.Remove(h);
            bool light = Light.Remove(h), quiet = Quiet.Remove(h);   // cleared first, whether or not the ped is still there
            if (!ENTITY.DOES_ENTITY_EXIST(h))
                return;
            StandUp(h);
            PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(h, false);
            if (!quiet)
                TASK.CLEAR_PED_TASKS(h, true, false);   // quiet: enRAGE's own orders stand (a fight, an interrogation)
            PED.SET_PED_KEEP_TASK(h, false);
            if (!quiet && !ENTITY.IS_ENTITY_DEAD(h))
                TASK.TASK_WANDER_STANDARD(h, 10f, 10);   // going about their day, not left standing where we put them
            if (light || Cast.Contains(h))
                return;   // one of enRAGE's records: never "no longer needed" (that could let the game clear one enRAGE spawned); one of the test cast stays ours to keep
            ENTITY.SET_PED_AS_NO_LONGER_NEEDED(&h);
        }

        public static bool SafeToSpawn()
        {
            return !MISC.GET_MISSION_FLAG() && !CAM.IS_SCREEN_FADED_OUT() && !CAM.IS_SCREEN_FADING_OUT() && !Game.IsLoading;
        }

        /// Starts streaming a model; true once it is ready.
        public static bool Ready(uint model)
        {
            if (!STREAMING.IS_MODEL_VALID(model) || !STREAMING.IS_MODEL_A_PED(model))
                return false;
            Loads.ModelAsked(model, Game.GameTime);   // noted either way: a body asked for and never used is let go (Game/Loads.cs)
            if (STREAMING.HAS_MODEL_LOADED(model))
                return true;
            STREAMING.REQUEST_MODEL(model, false);
            return false;
        }

        /// Why the last Spawn gave nothing (for the log): "a mission or a fade", "their body isn't streamed in yet" (the ped model, which
        /// Ready asks for; not the AI model, as Lex read it on 2026-11-03), "the game made nobody there".
        public static string LastSpawnWhy;

        public static Ped Spawn(uint model, Vector3 pos, float heading)
        {
            LastSpawnWhy = null;
            if (!SafeToSpawn())
            {
                LastSpawnWhy = "a mission or a fade";
                return null;
            }
            if (!Ready(model))
            {
                LastSpawnWhy = "their body isn't streamed in yet (ped model " + model.ToString("x8") + ", asked for now)";
                return null;
            }
            STREAMING.REQUEST_COLLISION_AT_COORD(pos.X, pos.Y, pos.Z);
            Ped p = World.CreatePed((PedHash)model, pos, heading);
            STREAMING.SET_MODEL_AS_NO_LONGER_NEEDED(model);
            Loads.ModelUsed(model);
            if (p == null || !p.Exists())
            {
                LastSpawnWhy = "the game made nobody at " + pos.X.ToString("0") + ", " + pos.Y.ToString("0") + ", " + pos.Z.ToString("0") + " (model " + model.ToString("x8") + ")";
                return null;
            }
            ENTITY.SET_ENTITY_AS_MISSION_ENTITY(p.Handle, true, true);
            Ours.Add(p.Handle);
            return p;
        }

        public static bool IsOurs(int handle)
        {
            return Ours.Contains(handle);
        }

        /// One of the test cast: spawned and kept as a mission entity, so the game never clears them while you're about,
        /// but not ours: nobody holds them.
        public static Ped SpawnCast(uint model, Vector3 pos, float heading)
        {
            Ped p = Spawn(model, pos, heading);
            if (p == null)
                return null;
            Ours.Remove(p.Handle);
            Cast.Add(p.Handle);
            return p;
        }

        public static bool IsCast(int handle)
        {
            return Cast.Contains(handle);
        }

        /// A test cast member's body taken away: deleted unseen, or in sight handed over to the game's own people; their
        /// identity stays.
        public static unsafe void ReleaseCast(Ped p, bool delete)
        {
            if (p == null)
                return;
            int h = p.Handle;
            Cast.Remove(h);
            if (Borrowed.Contains(h) || !ENTITY.DOES_ENTITY_EXIST(h))
                return;   // a feature holds them just now: its own hand-back lets them go
            if (delete)
            {
                p.Delete();
                return;
            }
            PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(h, false);
            Sociable(h);   // the game's greet, antagonize and rob, as for anyone it made itself
            ENTITY.SET_PED_AS_NO_LONGER_NEEDED(&h);
        }

        /// Deletes the ped (when nobody can see it vanish) or hands it back to the game. Someone borrowed is always
        /// handed back.
        public static unsafe void Release(Ped p, bool delete)
        {
            if (p == null)
                return;
            int h = p.Handle;
            if (Borrowed.Contains(h))
            {
                GiveBack(h);
                return;
            }
            Ours.Remove(h);
            if (!ENTITY.DOES_ENTITY_EXIST(h))
                return;
            if (delete)
            {
                p.Delete();
            }
            else
            {
                StandUp(h);
                PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(h, false);
                Sociable(h);   // the game's greet, antagonize and rob, as for anyone it made itself (before the handle is let go)
                ENTITY.SET_PED_AS_NO_LONGER_NEEDED(&h);
            }
        }

        // Made people handed back are the game's in full (Lex, 2026-11-04, testing the relative who walks up: "I can now lock on
        // to the person walking up but no option to greet them"). A ped a script makes has no interaction personality (0), and
        // the game offers its Greet, Antagonize and Rob only to a ped that has one. enRAGE's MakeSociable does this for its own
        // made people, and the game's prompts come up on them after: the personality of a
        // townsperson about is copied onto them, and config flag 297 is set on, 301 and 130 off (the lock-on interaction
        // flags, as enRAGE sets them; this add-on had never set them). Only on the way back to the game (Release, ReleaseCast),
        // never while one of our scenes holds them, and only for the living and the human.
        private static uint _donorPersonality;   // the last townsperson's personality copied, kept for a road with nobody about to copy

        /// The last hand-back's note for the log: the personality before and after.
        public static string LastSociable;

        /// The game's own greet, antagonize and rob for one of ours handed back alive (enRAGE's MakeSociable, as above).
        public static void Sociable(int h)
        {
            if (h == 0 || !ENTITY.DOES_ENTITY_EXIST(h) || ENTITY.IS_ENTITY_DEAD(h) || !PED.IS_PED_HUMAN(h))
                return;
            uint was = PED._GET_PED_INTERACTION_PERSONALITY(h), donor = _donorPersonality;
            string from = donor != 0 ? "remembered" : "nobody about to copy";
            Ped me = Game.Player.Character;
            Vector3 at = me != null && me.Exists() ? me.Position : ENTITY.GET_ENTITY_COORDS(h, true, true);
            foreach (Ped p in Peds.All())
            {
                if (p == null || !p.Exists() || p.Handle == h || p.IsDead || PED.IS_PED_A_PLAYER(p.Handle) || !PED.IS_PED_HUMAN(p.Handle) ||
                    Ours.Contains(p.Handle) || Cast.Contains(p.Handle) || ENTITY.IS_ENTITY_A_MISSION_ENTITY(p.Handle) || Natives.IsChild(p) ||
                    p.Position.DistanceTo(at) > 80f)
                    continue;
                uint pers = PED._GET_PED_INTERACTION_PERSONALITY(p.Handle);
                if (pers == 0)
                    continue;
                donor = pers;
                _donorPersonality = pers;
                from = "a townsperson about";
                break;
            }
            if (donor != 0 && donor != was)
                PED._SET_PED_INTERACTION_PERSONALITY(h, donor);
            PED.SET_PED_CONFIG_FLAG(h, 297, true);
            PED.SET_PED_CONFIG_FLAG(h, 301, false);
            PED.SET_PED_CONFIG_FLAG(h, 130, false);
            LastSociable = "interaction personality 0x" + was.ToString("X8") + " -> 0x" + (donor != 0 ? donor : was).ToString("X8") + " (" + from + ")";
        }

        /// Out of a crouch or a stealth walk (the stalker's creep), so nobody goes back to the world still crouching.
        public static void StandUp(int h)
        {
            if (PED.GET_PED_CROUCH_MOVEMENT(h))
                PED._SET_PED_CROUCH_MOVEMENT(h, false, 0, false);
            if (PED.GET_PED_STEALTH_MOVEMENT(h))
                PED.SET_PED_STEALTH_MOVEMENT(h, false, 0, 0);
        }

        public static void CleanupAll()
        {
            foreach (int h in Ours.ToList())
            {
                try
                {
                    if (Borrowed.Contains(h))
                        GiveBack(h);   // someone who was already in the world: never deleted
                    else if (ENTITY.DOES_ENTITY_EXIST(h))
                        new Ped(h).Delete();
                }
                catch
                {
                }
            }
            Ours.Clear();
            Borrowed.Clear();
            foreach (int h in Cast.ToList())
            {
                try
                {
                    if (ENTITY.DOES_ENTITY_EXIST(h))
                        new Ped(h).Delete();   // the test cast's bodies; their identities stay in the save
                }
                catch
                {
                }
            }
            Cast.Clear();
        }

        // ---- choosing a model ----------------------------------------------------------------------

        private static readonly Dictionary<string, string[]> TownMen = new Dictionary<string, string[]>
        {
            { "Valentine", new[] { "a_m_m_valtownfolk_01", "a_m_m_valtownfolk_02", "a_m_m_valfarmer_01", "a_m_m_vallaborer_01" } },
            { "Rhodes", new[] { "a_m_m_rhdtownfolk_01", "a_m_m_rhdtownfolk_02", "a_m_m_rhdupperclass_01" } },
            { "Saint Denis", new[] { "a_m_m_middlesdtownfolk_01", "a_m_m_middlesdtownfolk_02", "a_m_m_lowersdtownfolk_01", "a_m_m_nbxupperclass_01" } },
            { "Blackwater", new[] { "a_m_m_blwtownfolk_01", "a_m_m_blwupperclass_01", "a_m_m_blwlaborer_01" } },
            { "Strawberry", new[] { "a_m_m_strtownfolk_01", "a_m_m_strlaborer_01" } },
            { "Annesburg", new[] { "a_m_m_asbtownfolk_01", "a_m_m_asbminer_01" } },
            { "Armadillo", new[] { "a_m_m_armtownfolk_01", "a_m_m_armtownfolk_02" } },
            { "Tumbleweed", new[] { "a_m_m_tumtownfolk_01", "a_m_m_tumtownfolk_02" } },
            { "Van Horn", new[] { "a_m_m_vhttownfolk_01" } }
        };

        private static readonly Dictionary<string, string[]> TownWomen = new Dictionary<string, string[]>
        {
            { "Valentine", new[] { "a_f_m_valtownfolk_01" } },
            { "Rhodes", new[] { "a_f_m_rhdtownfolk_01", "a_f_m_rhdtownfolk_02", "a_f_m_rhdupperclass_01" } },
            { "Saint Denis", new[] { "a_f_m_middlesdtownfolk_01", "a_f_m_middlesdtownfolk_02", "a_f_m_lowersdtownfolk_01", "a_f_m_nbxupperclass_01" } },
            { "Blackwater", new[] { "a_f_m_blwtownfolk_01", "a_f_m_blwtownfolk_02", "a_f_m_blwupperclass_01" } },
            { "Strawberry", new[] { "a_f_m_strtownfolk_01" } },
            { "Annesburg", new[] { "a_f_m_asbtownfolk_01" } },
            { "Armadillo", new[] { "a_f_m_armtownfolk_01", "a_f_m_armtownfolk_02" } },
            { "Tumbleweed", new[] { "a_f_m_tumtownfolk_01", "a_f_m_tumtownfolk_02" } },
            { "Van Horn", new[] { "a_f_m_vhttownfolk_01" } }
        };

        private static readonly string[] RoughMen = { "a_m_m_griroughtravellers_01", "a_m_m_bynroughtravellers_01", "a_m_m_htlroughtravellers_01", "a_m_m_huntertravelers_cool_01", "a_m_m_rkrroughtravellers_01" };
        private static readonly string[] RoughWomen = { "a_f_m_roughtravellers_01" };

        public static uint ModelFor(string town, bool woman, int seed)
        {
            string[] pool;
            if (town != null && (woman ? TownWomen : TownMen).TryGetValue(town, out pool))
                return Natives.Hash(pool[(seed & 0x7fffffff) % pool.Length]);
            pool = woman ? RoughWomen : RoughMen;
            return Natives.Hash(pool[(seed & 0x7fffffff) % pool.Length]);
        }
    }
}
