// The catalogue (the mind design, step 5; daytime run 2026-10-27, Core only): one table of what the game is proven to
// do, so mapping a feeling to what shows never invents a behaviour. Each entry: its channel (gesture, emote, scenario,
// clip, voice line, movement task), the game's name for it, the feelings and urges it can show, the least strength it
// fits, who can do it (men, women, anyone; standing or not) and how it was proven (played in game, its set found in
// game, the voice banks that carry it). Entries come only from what a log or Lex's play has shown: the standing gesture
// sets and their clips the add-on plays, the Online emotes Story Mode has, the scenarios that started, the clips the
// scenes and despair play, the line names the banks carry (counts from amb_takes.txt), the tasks the scenes use. A
// feeling or urge nothing here can show is shown by the thought alone, and goes on the list of animations to look for
// (Gaps). Plain C#, no game.
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public sealed class Expression
    {
        public string Channel, Name, Who, Posture, Proof;
        public string Words;   // what it looks like, in plain words: all the model is ever offered (FIVEDAY2 item 9)
        public string[] Feelings, Urges;
        public string Least;   // mild, moderate or strong: the least strength it fits

        /// Proven: played in game, or a line the voice banks carry. Last night's clips no log has shown playing yet are
        /// listed, marked, and left out until one does.
        public bool Proven
        {
            get { return Proof == null || !Proof.StartsWith("NOT YET SEEN"); }
        }

        /// A gesture or clip written "set / clip": something Perform can play. A row that names only a set, or says what it
        /// is in words ("head shake"), isn't.
        public bool HasClip
        {
            get { return (Channel == "gesture" || Channel == "clip") && Clip != null; }
        }

        /// The clip's name, or null.
        public string Clip
        {
            get
            {
                int k = Name.IndexOf(" / ", StringComparison.Ordinal);
                string c = k < 0 ? null : Name.Substring(k + 3).Trim();
                return c == null || c.Length == 0 || c.IndexOf(' ') >= 0 ? null : c;
            }
        }

        /// The animation set, for a man or a woman where the row has both.
        public string Dict(bool woman)
        {
            int k = Name.IndexOf(" / ", StringComparison.Ordinal);
            return (k < 0 ? Name : Name.Substring(0, k)).Trim().Replace("{sex}", woman ? "female" : "male");
        }

        public bool Fits(string feeling, string urge, string strength, bool woman, bool standing)
        {
            if (Who == "men" && woman || Who == "women" && !woman || Posture == "standing" && !standing)
                return false;
            if (Array.IndexOf(Feelings, feeling) < 0 && Array.IndexOf(Urges, urge) < 0)
                return false;
            return Rank(strength) >= Rank(Least);
        }

        private static int Rank(string s)
        {
            return s == "strong" ? 3 : s == "moderate" ? 2 : s == "mild" ? 1 : 0;
        }
    }

    public static class Catalogue
    {
        private static Expression E(string channel, string name, string feelings, string urges, string least, string who, string posture, string proof, string words = null)
        {
            return new Expression
            {
                Channel = channel, Name = name, Feelings = feelings.Length == 0 ? new string[0] : feelings.Split(','), Urges = urges.Length == 0 ? new string[0] : urges.Split(','),
                Least = least, Who = who, Posture = posture, Proof = proof, Words = words
            };
        }

        private const string Silent = "ai_gestures@gen_{sex}@standing@silent", Speaker = "ai_gestures@gen_{sex}@standing@speaker";
        public const string TurnMove = "TASK_TURN_PED_TO_FACE_ENTITY", LookMove = "TASK_LOOK_AT_ENTITY", StillMove = "TASK_STAND_STILL", WanderMove = "TASK_WANDER_STANDARD";   // the moves Perform makes
        private const string Candidate = "NOT YET SEEN: a candidate from the game's own sets for the things to come across (FIVEDAY2 item 7); the clip walker proves it";
        private const string SetsFound = "set found in game (0.6.1 probe)", Played = "played in game", Walker = "played in game: the clip walker, 2026-11-09";

        public static readonly Expression[] All =
        {
            // gestures (upper body, standing; the four standing sets found in game, 0.6.1)
            E("gesture", Silent + " / silent_neutral_hat_tip_l_001", "gratitude,joy,pride", "share,repay,display", "mild", "men", "standing", Walker, "tips their hat"),
            E("gesture", Silent + " / silent_headnod_greet_f_001", "gratitude,joy,relief", "share,repay", "mild", "any", "standing", Walker, "gives a nod"),
            E("gesture", Silent + " / silent_neutral_wave_l_001", "joy,gratitude", "share", "mild", "any", "standing", "NOT YET SEEN: didn't start for the clip walker in game, 2026-11-09 (status 8, twice)", "lifts a hand"),
            E("gesture", Silent + " / silent_neutral_spit_f_001", "contempt,anger", "dismiss,disparage", "moderate", "men", "standing", Walker, "spits on the ground"),
            E("gesture", Silent + " / head shake", "contempt,anger,shame", "dismiss,disparage", "mild", "any", "standing", Played + " (0.6.1, the stalker's head-shake on the upper body)"),
            E("gesture", "ai_gestures@gen_female@standing@speaker / surprised_gasp_f_001", "fear,pity", "freeze,avoid,help", "mild", "women", "standing", Walker, "catches their breath"),
            E("gesture", "ai_gestures@gen_male@standing@speaker / negative_disbelief_l_001", "contempt,anger,fear", "dismiss,watch", "mild", "men", "standing", Walker, "shakes their head, not believing it"),
            E("gesture", Speaker + " / happy_cheer_f_001", "joy,gratitude,pride", "share,display", "moderate", "any", "standing", Walker, "throws up an arm"),
            E("gesture", Speaker + " / happy_laugh_f_001", "joy,contempt", "share,dismiss", "mild", "any", "standing", Walker, "laughs out loud"),
            E("gesture", Speaker + " / neutral_point_l_001", "anger,contempt", "confront,disparage", "moderate", "any", "standing", Walker, "points at them"),
            // the reactions' clips (Core/Reactions.cs): the sets were found in game (0.6.1 probe) and a head-shake was seen playing in a Shift+F9 scene (2026-10-26); the rest are listed in anims.lua and not yet seen played (night 2026-10-28, item 13: the mind's reactions play them through Reactions.KindOf)
            E("gesture", Silent + " / aknwoledge_timid_look_down_f_001", "shame,guilt", "hide,withdraw,amends,avoid", "mild", "any", "standing", SetsFound + "; the reactions' look-down, not yet seen played", "looks down at their boots"),
            E("gesture", Silent + " / silent_might_flee_nervous_f_001", "fear", "avoid,freeze", "mild", "any", "standing", SetsFound + "; the reactions' flinch, not yet seen played", "flinches back"),
            E("gesture", Silent + " / silent_aggressive_taunt_f_001", "anger", "confront", "mild", "men", "standing", SetsFound + "; the reactions' bristle, not yet seen played", "squares up"),
            E("gesture", Silent + " / silent_negative_dismiss_f_001", "anger,contempt", "confront,dismiss", "mild", "women", "standing", SetsFound + "; the reactions' bristle for women, not yet seen played", "waves it away"),
            E("gesture", Silent + " / silent_confused_look_f_001", "fear,pity", "watch", "mild", "any", "standing", SetsFound + "; the reactions' puzzled look, not yet seen played", "looks puzzled"),
            E("gesture", Silent + " / silent_neutral_shrug_f_001", "relief,contempt,envy", "dismiss,rest", "mild", "any", "standing", SetsFound + "; the reactions' shrug, not yet seen played", "shrugs"),
            E("gesture", Silent + " / silent_wince_l_004", "fear", "freeze", "mild", "any", "standing", SetsFound + "; the reactions' wince (the hurt seen playing is the street fight's reel), not yet seen played", "winces"),
            E("gesture", Silent + " / silent_positive_headonod_f_001", "gratitude,joy,relief", "repay,share,rest", "mild", "any", "standing", SetsFound + "; the reactions' nod, not yet seen played", "nods slowly"),
            // the few Online emotes Story Mode has (0.6.1 probe: 3 of 28 sets)
            E("emote", "script_mp@emotes@respectful_bow@male@unarmed@full", "gratitude,pride", "repay,display", "moderate", "any", "standing", Played + " (0.6.1, the stalker's acts)"),
            E("emote", "script_mp@emotes@pointlaugh@male@unarmed@full", "contempt,joy", "dismiss,disparage", "moderate", "any", "standing", Played + " (0.6.1)"),
            E("emote", "script_mp@emotes@prospector_jig@male@unarmed@full", "joy,pride", "share,display", "strong", "any", "standing", Played + " (0.6.1)"),
            // scenarios (started in game; gravedig, hammer and scrub failed and are left out)
            E("scenario", "WORLD_HUMAN_GRAVE_MOURNING_KNEEL", "grief,guilt,shame", "withdraw,amends", "moderate", "any", "any", Played + " (the Sabbath's kneel, the despair kneel)", "goes down on one knee"),
            E("scenario", "WORLD_HUMAN_DRUNK_BAR_SLUMPED", "grief,shame", "withdraw,hide", "moderate", "men", "any", Played + " (the bar)", "slumps where they are"),
            E("scenario", "WORLD_HUMAN_CROUCH_INSPECT", "pity,grief", "help", "mild", "any", "any", "NOT YET SEEN: the visitor's crouch (last night)", "crouches down"),
            E("scenario", "WORLD_HUMAN_SIT_GROUND", "grief,shame,fear", "withdraw,rest", "moderate", "any", "any", "NOT YET SEEN: the fall's sit (last night)", "sits on the ground"),
            // clips the scenes and despair play
            E("clip", "mech_grapple / table_smash", "anger", "confront", "strong", "men", "standing", Played + " (faces into the bar)"),
            E("clip", "script_re@street_fight@v3", "anger", "confront,intervene", "strong", "men", "standing", Played + " (street fights)"),
            E("clip", "script_re@drunk_dueler", "anger,contempt", "confront", "moderate", "men", "standing", Played + " (the drunk's challenge)"),
            E("clip", "script_re@rally@cross_accident / sobbing_idle_a_leader", "grief,shame,fear", "withdraw", "strong", "any", "standing", Played + " (despair, 2026-10-27; sobs on a man and a woman)", "breaks down sobbing"),
            E("clip", "script_story@mry1@ig@ig6_jamie_attempts_suicide", "grief,shame", "withdraw", "strong", "any", "standing", Played + " (despair, 2026-10-27)"),
            E("clip", "script_re@town_confront@male@barfight", "anger,contempt", "confront", "moderate", "men", "standing", "NOT YET SEEN: the street callout's set (NIGHT4 item 4; callover, confront, to_crowd, taunt, threat, ignore, positive, negative; the log's ENCOUNTER clips line proves each)"),
            E("clip", "script_re@town_confront@female@drunk_duel", "anger,contempt", "confront", "moderate", "women", "standing", "NOT YET SEEN: the street callout's set for a woman (NIGHT4 item 4)"),
            E("clip", "script_re@domestic_dispute@rhodes", "anger,contempt", "confront", "moderate", "men", "standing", "NOT YET SEEN: a husband's temper, his rant (NIGHT4 item 4; head_in_clouds, live_on_moon, thats_rhodes, grass_greener _husband)"),
            E("clip", "script_re@domestic_dispute@annesburg", "anger,contempt", "confront", "moderate", "men", "standing", "NOT YET SEEN: a husband's temper, his rant (you_married_husband, shoulda_husband)"),
            E("move", "TASK_KNOCKED_OUT_AND_HOGTIED", "fear", "freeze", "strong", "any", "any", "NOT YET SEEN: hogtied by the road (NIGHT4 item 4; the log says whether IS_PED_HOGTIED held at 2 s)"),
            E("clip", "script_re@kidnap_victim / thank_you_a_victim", "gratitude,relief", "repay", "moderate", "any", "standing", Walker),
            E("clip", "ai_combat@damage@writhe@base / writhe_loop", "fear,grief", "freeze", "strong", "any", "any", Walker),
            // movement tasks the scenes use, proven in play
            E("move", "TASK_SMART_FLEE_PED", "fear", "avoid", "moderate", "any", "any", "NOT YET SEEN: last night's visitor and robbery"),
            E("move", "TASK_GO_TO_ENTITY", "anger,pity,grief", "confront,intervene,help", "moderate", "any", "any", "NOT YET SEEN: last night's grudge walk"),
            E("move", TurnMove, "anger,contempt,fear,pity", "confront,watch", "mild", "any", "any", Played + " (scenes, sights)", "turns to face them"),
            E("move", LookMove, "fear,anger,contempt,envy,pity,joy", "watch,freeze", "mild", "any", "any", Played + " (nods, sights, wake acts)", "looks at them"),
            E("move", "TASK_HANDS_UP", "fear", "freeze", "strong", "any", "any", "NOT YET SEEN: last night's robbery"),
            E("move", StillMove, "fear,shame", "freeze", "moderate", "any", "any", Played + " (the animation rows hold people with it between clips, 2026-10-27)", "stands stock still"),
            E("move", WanderMove, "contempt,shame,envy,guilt", "dismiss,avoid,withdraw,hide", "mild", "any", "any", Played + " (every hand-back)", "walks away"),
            // the game's own recorded lines (how many of the 332 voices carry each, from amb_takes.txt)
            // the game's own encounters' sets, for the wild events and the composed acts: only names ship, and each stays out of use until a log has shown it playing
            E("clip", "script_re@execution / base_main", "anger,contempt", "confront", "moderate", "men", "standing", Walker),
            E("clip", "script_re@execution / warn_forward_guard", "anger,contempt", "confront", "moderate", "men", "standing", Walker),
            E("clip", "script_re@execution / base_vic01", "fear,grief", "freeze", "strong", "men", "any", Walker),
            E("clip", "script_re@execution / action_vic01", "fear", "freeze", "strong", "men", "any", Walker),
            E("clip", "script_re@execution / exit_quick_vic01", "relief,fear", "avoid", "strong", "men", "any", Walker),
            E("clip", "script_re@chain_gang / action_pick01", "shame,grief", "withdraw", "moderate", "men", "standing", Walker),
            E("clip", "script_re@chain_gang / action_pris01", "shame,grief", "withdraw", "moderate", "men", "standing", Walker),
            E("clip", "script_re@chain_gang / action_guard01", "contempt", "watch", "mild", "men", "standing", Walker),
            E("clip", "script_re@injured_rider / idle_01", "fear,grief", "freeze", "strong", "any", "any", Walker),
            E("clip", "script_re@injured_rider / fall_left_01", "fear", "freeze", "strong", "any", "standing", Walker),
            E("clip", "script_re@torch_procession / idle_male_a", "grief,pride", "display", "moderate", "men", "standing", Walker),
            E("clip", "script_re@torch_procession / enter_male_a", "grief,pride", "display", "moderate", "men", "standing", Walker),
            E("clip", "script_re@town_burial@grave_digger / ilo_med_n", "grief", "withdraw", "mild", "men", "standing", Walker),
            E("clip", "script_re@town_burial@preacher@pose_a@base / base", "grief,pity", "help", "mild", "men", "standing", Walker),
            E("clip", "script_re@stalking_shadows@walks@with_torch / idle_a", "fear", "watch", "mild", "men", "standing", Walker),
            E("clip", "script_re@stalking_shadows@walks@with_torch / walk_a", "fear", "watch", "mild", "men", "standing", Walker),
            E("clip", "script_re@duel_winner@taunt / idle_ped01", "contempt,pride", "disparage,display", "mild", "men", "standing", Candidate),
            E("clip", "script_re@duel_winner@taunt / you_scared_ped01", "contempt", "disparage", "moderate", "men", "standing", Candidate),
            E("clip", "script_re@duel_winner@taunt / laughing_ped01", "contempt,joy", "disparage", "moderate", "men", "standing", Candidate),
            E("clip", "script_re@duel_winner@taunt / chicken_shit_ped01", "contempt", "disparage", "strong", "men", "standing", Candidate),
            E("clip", "script_re@snake_bite@ig_bitten / cower_idle_victim", "fear", "freeze", "strong", "any", "any", Candidate),
            E("clip", "script_re@snake_bite@ig_bitten / i_feel_so_cold_victim", "fear,grief", "freeze", "strong", "any", "any", Candidate),
            E("clip", "script_re@stranded_rider / anxious_idle01", "fear", "watch", "mild", "any", "standing", Candidate),
            E("clip", "script_re@stranded_rider / idle", "relief,fear", "watch", "mild", "any", "standing", Candidate),
            E("clip", "script_re@lost_friend / man_pay_respects", "grief,pity", "withdraw", "moderate", "men", "standing", Candidate),
            E("clip", "script_re@sharp_shooter / idle_base", "pride", "display", "mild", "men", "standing", Candidate),
            E("clip", "script_re@sharp_shooter / callover_idle", "pride,joy", "display", "mild", "men", "standing", Candidate),
            E("clip", "script_re@sharp_shooter / draw_your_gun", "anger,pride", "confront", "moderate", "men", "standing", Candidate),
            E("clip", "script_re@rifle_practice / action_01_male01", "pride", "display", "mild", "men", "standing", Candidate),
            E("clip", "script_re@labor_strike@3peds / dia_01_ped01", "anger", "confront", "moderate", "men", "standing", Candidate),
            E("clip", "script_re@labor_strike@3peds / aggro_01_ped01", "anger", "confront", "strong", "men", "standing", Candidate),
            E("clip", "script_re@public_hanging@crowds@generic / loop_group_am01", "fear,grief", "watch", "moderate", "men", "standing", Candidate),
            E("clip", "script_re@public_hanging@std_crowds / ig2_action_crowd_m01", "fear,contempt", "watch", "moderate", "men", "standing", Candidate),
            E("clip", "script_re@public_hanging@std_crowds / ig2_action_crowd_f01", "fear,pity", "watch", "moderate", "women", "standing", Candidate),
            E("voice", "GENERIC_ANGRY_SHOUT", "anger", "confront", "strong", "any", "any", "voice banks carry it (Speech.Angry)", "shouts in anger"),
            E("voice", "CHALLENGE_THREATEN", "anger,contempt", "confront", "moderate", "any", "any", "297 of 332 voices", "says something threatening"),
            E("voice", "GET_OUT", "anger,contempt", "confront,dismiss", "moderate", "any", "any", "317 of 332 voices", "tells them to get out"),
            E("voice", "SHAME_ON_YOU", "contempt,anger", "disparage,confront", "moderate", "any", "any", "302 of 332 voices", "tells them shame on them"),
            E("voice", "DISMISSIVE_REACT", "contempt", "dismiss", "mild", "any", "any", "voice banks carry it (Speech.Mild)", "says something dismissive"),
            E("voice", "VOCAL_FX_DISAPPROVE", "contempt,envy", "dismiss,disparage", "mild", "any", "any", "179 of 332 voices (wordless)", "makes a sound of disapproval"),
            E("voice", "GENERIC_FRIGHTENED_HIGH", "fear", "avoid,freeze", "strong", "any", "any", "voice banks carry it (Speech.Scared)", "cries out"),
            E("voice", "GENERIC_FRIGHTENED_MED", "fear", "avoid", "moderate", "any", "any", "voice banks carry it (Speech.Scared)", "says something frightened"),
            E("voice", "SCARED_HELP", "fear", "avoid,freeze", "strong", "any", "any", "284 of 332 voices", "calls for help"),
            E("voice", "PLEAD", "fear", "freeze", "strong", "any", "any", "330 of 332 voices", "pleads"),
            E("voice", "GENERIC_SHOCKED_DISBELIEF", "fear,grief", "freeze,watch", "moderate", "any", "any", "voice banks carry it; played (despair's disbelief)", "says something shocked"),
            E("voice", "SORRY_TO_HEAR_THAT", "pity,grief", "help", "mild", "any", "any", "329 of 332 voices", "says how sorry they are"),
            E("voice", "GENERIC_THANKS", "gratitude,relief", "repay", "mild", "any", "any", "329 of 332 voices", "thanks them"),
            E("voice", "RELIEVED_REACT", "relief", "rest,share", "moderate", "any", "any", "295 of 332 voices", "says how relieved they are"),
            E("voice", "GENERIC_CHEER", "joy,pride", "share,display", "moderate", "any", "any", "276 of 332 voices", "gives a cheer"),
            E("voice", "COOL_IT", "pity,anger", "intervene", "moderate", "any", "any", "296 of 332 voices", "tells them to simmer down"),
            E("voice", "DISPUTE_INTERVENE_HARASSMENT", "anger,pity", "intervene", "moderate", "any", "any", "223 of 332 voices", "tells them to leave off"),
            E("voice", "STORM_OFF", "shame,anger", "withdraw,avoid", "moderate", "any", "any", "216 of 332 voices", "says something sharp"),
            E("voice", "IM_OUTTA_HERE", "fear,shame", "avoid", "moderate", "any", "any", "246 of 332 voices", "says they're going"),
            E("voice", "GENERIC_MOCK", "contempt,envy", "disparage,dismiss", "mild", "any", "any", "voice banks carry it; played (the waking voices)", "mocks them"),
            E("voice", "GOING_BADLY", "grief,fear", "withdraw", "moderate", "any", "any", "329 of 332 voices", "says it's going badly"),
            E("voice", "LEAVE_ME", "grief,shame", "withdraw", "moderate", "any", "any", "210 of 332 voices", "says to leave them be"),
            E("voice", "GREET_GENERAL_FAMILIAR", "joy,gratitude", "share", "mild", "any", "any", "328 of 332 voices", "greets them"),
        };

        /// What could show this feeling with this urge, at this strength, for this person (standing or not).
        public static IEnumerable<Expression> For(string feeling, string urge, string strength, bool woman, bool standing)
        {
            return All.Where(x => x.Proven && x.Fits(feeling, urge, strength, woman, standing));
        }

        /// The feelings and urges nothing in the catalogue can show yet: the animations to look for.
        public static List<string> Gaps(IEnumerable<string> feelings, IEnumerable<string> urges)
        {
            var gaps = new List<string>();
            foreach (string f in feelings)
                if (f != "none" && !All.Any(x => x.Proven && Array.IndexOf(x.Feelings, f) >= 0 && x.Channel != "voice" && x.Channel != "move"))
                    gaps.Add("feeling " + f + " (no gesture, clip or scenario)");
            foreach (string u in urges)
                if (u != "none" && !All.Any(x => x.Proven && Array.IndexOf(x.Urges, u) >= 0))
                    gaps.Add("urge " + u);
            return gaps;
        }
    }
}
