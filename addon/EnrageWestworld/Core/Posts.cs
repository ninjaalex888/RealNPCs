// Town posts: the people who stand at the same counter every day (the general store, the gunsmith, the butcher,
// the doctor, the barber, the barkeep, the station agent...). The game plays each of them with a model of its own,
// so each post keeps one person, with a name, a life and a memory of the player. The game brings a shot-down
// counter keeper back bandaged, so by default (TownPostsAfterDeath = survive) they survived: the same person, who
// remembers the wound and who gave it, and whom those who saw them fall find uncanny. Set to successor, the post
// passes on instead: to a relative of the same sex as the model (a son, a brother, a nephew...), or else to a
// newcomer who bought the business. Sheriffs, preachers and anyone a mission may name are left alone. All lines
// are original to this add-on.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace EnrageWestworld.Core
{
    [DataContract]
    public sealed class Post
    {
        [DataMember(EmitDefaultValue = false)] public string Id;            // "post:<model>"
        [DataMember(EmitDefaultValue = false)] public string Model;         // the model name the game plays the post with
        [DataMember(EmitDefaultValue = false)] public string Town;
        [DataMember(EmitDefaultValue = false)] public string Role;          // "the gunsmith"
        [DataMember(EmitDefaultValue = false)] public string HolderId;      // the resident at the post now (null: nobody yet)
        [DataMember(EmitDefaultValue = false)] public List<string> Former = new List<string>();   // earlier holders, oldest first (they may be forgotten)
        [DataMember(EmitDefaultValue = false)] public string LastName;      // the holder before, by name, for whoever comes next
        [DataMember(EmitDefaultValue = false)] public long VacantAt;        // game minute the last holder died, 0 if nobody has
        [DataMember(EmitDefaultValue = false)] public bool KilledByPlayer;
        [DataMember(EmitDefaultValue = false)] public bool KillerKnown;     // somebody saw him do it
        [DataMember(EmitDefaultValue = false)] public int Outfit = -1;      // the outfit preset the holder is dressed in, -1 = as the game made them
        [DataMember(EmitDefaultValue = false)] public long FellAt;          // game minute the holder was last shot down (the game brings them back bandaged)
        [DataMember(EmitDefaultValue = false)] public long BackAt;          // ...and when they were seen back at the counter
        [DataMember(EmitDefaultValue = false)] public List<string> SawFall = new List<string>();   // who saw them fall, to find it uncanny
    }

    public sealed class PostKind
    {
        public string Model, Town, Role, Kind;
        public bool Woman, Old;
    }

    public static class Posts
    {
        private static PostKind K(string model, string town, string role, string kind)
        {
            return new PostKind { Model = model, Town = town, Role = role, Kind = kind, Woman = model.StartsWith("u_f_"), Old = model.StartsWith("u_m_o_") };
        }

        /// Counter trades only. Sheriffs, deputies, preachers, auctioneers and anyone named in a mission are left out.
        public static readonly PostKind[] All =
        {
            K("u_m_m_valgenstoreowner_01", "Valentine", "the general store keeper", "store"),
            K("u_m_m_valgunsmith_01", "Valentine", "the gunsmith", "gunsmith"),
            K("u_m_m_valbutcher_01", "Valentine", "the butcher", "butcher"),
            K("u_m_m_valdoctor_01", "Valentine", "the doctor", "doctor"),
            K("u_m_m_valbarber_01", "Valentine", "the barber", "barber"),
            K("u_m_m_valhotelowner_01", "Valentine", "the hotel keeper", "hotel"),
            K("u_m_m_valbartender_01", "Valentine", "the barkeep", "barkeep"),
            K("u_m_o_valbartender_01", "Valentine", "the barkeep", "barkeep"),
            K("u_m_m_rhdgenstoreowner_01", "Rhodes", "the general store keeper", "store"),
            K("u_m_m_rhdgenstoreowner_02", "Rhodes", "the general store keeper", "store"),
            K("u_m_m_rhdgunsmith_01", "Rhodes", "the gunsmith", "gunsmith"),
            K("u_m_m_rhddoctor_01", "Rhodes", "the doctor", "doctor"),
            K("u_m_m_rhdbartender_01", "Rhodes", "the barkeep", "barkeep"),
            K("u_m_m_rhdundertaker_01", "Rhodes", "the undertaker", "undertaker"),
            K("u_m_m_rhdtrainstationworker_01", "Rhodes", "the station agent", "station"),
            K("u_m_m_nbxgeneralstoreowner_01", "Saint Denis", "the general store keeper", "store"),
            K("u_m_m_nbxgunsmith_01", "Saint Denis", "the gunsmith", "gunsmith"),
            K("u_m_m_nbxbartender_01", "Saint Denis", "the barkeep", "barkeep"),
            K("u_m_m_nbxbartender_02", "Saint Denis", "the barkeep", "barkeep"),
            K("u_m_m_sdphotographer_01", "Saint Denis", "the photographer", "photographer"),
            K("u_m_m_sdexoticsshopkeeper_01", "Saint Denis", "the trader", "trader"),
            K("u_m_m_sdtrapper_01", "Saint Denis", "the trapper", "trader"),
            K("u_m_m_nbxliveryworker_01", "Saint Denis", "the stable hand", "livery"),
            K("u_m_m_nbxboatticketseller_01", "Saint Denis", "the ticket seller", "station"),
            K("u_m_m_sdcustomvendor_01", "Saint Denis", "the tailor", "tailor"),
            K("u_m_o_blwgeneralstoreowner_01", "Blackwater", "the general store keeper", "store"),
            K("u_m_o_blwbartender_01", "Blackwater", "the barkeep", "barkeep"),
            K("u_m_o_blwphotographer_01", "Blackwater", "the photographer", "photographer"),
            K("u_m_m_blwtrainstationworker_01", "Blackwater", "the station agent", "station"),
            K("u_m_m_strgenstoreowner_01", "Strawberry", "the general store keeper", "store"),
            K("u_m_m_strfreightstationowner_01", "Strawberry", "the freight agent", "station"),
            K("u_m_m_armgeneralstoreowner_01", "Armadillo", "the general store keeper", "store"),
            K("u_m_m_armtrainstationworker_01", "Armadillo", "the station agent", "station"),
            K("u_m_m_armundertaker_01", "Armadillo", "the undertaker", "undertaker"),
            K("u_m_o_armbartender_01", "Armadillo", "the barkeep", "barkeep"),
            K("u_f_m_tumgeneralstoreowner_01", "Tumbleweed", "the general store keeper", "store"),
            K("u_m_m_tumbartender_01", "Tumbleweed", "the barkeep", "barkeep"),
            K("u_m_m_tumbutcher_01", "Tumbleweed", "the butcher", "butcher"),
            K("u_m_m_tumgunsmith_01", "Tumbleweed", "the gunsmith", "gunsmith"),
            K("u_m_m_tumtrainstationworker_01", "Tumbleweed", "the station agent", "station"),
            K("u_m_m_asbgunsmith_01", "Annesburg", "the gunsmith", "gunsmith"),
            K("u_f_m_vhtbartender_01", "Van Horn", "the barkeep", "barkeep"),
            K("u_m_m_vhtstationclerk_01", "Van Horn", "the station clerk", "station"),
            K("u_m_o_vhtexoticshopkeeper_01", "Van Horn", "the trader", "trader"),
            K("u_m_m_walgeneralstoreowner_01", "Wallace Station", "the general store keeper", "store"),
            K("u_m_o_rigtrainstationworker_01", "Riggs Station", "the station agent", "station")
        };

        private static readonly Dictionary<string, PostKind> ByModel = All.ToDictionary(k => k.Model, StringComparer.OrdinalIgnoreCase);

        /// The post a model plays, or null.
        public static PostKind Of(string modelName)
        {
            PostKind k;
            return modelName != null && ByModel.TryGetValue(modelName, out k) ? k : null;
        }

        // ---- who holds a post --------------------------------------------------------------------------

        private static readonly string[] MenKin = { "son", "son", "son", "son", "brother", "brother", "brother", "nephew", "nephew", "cousin", "son-in-law" };
        private static readonly string[] WomenKin = { "daughter", "daughter", "daughter", "daughter", "sister", "sister", "sister", "niece", "niece", "daughter-in-law" };
        private static readonly string[] FromTowns = { "Saint Denis", "Valentine", "Rhodes", "Blackwater", "Kansas City", "St. Louis", "Philadelphia", "Chicago", "Baltimore", "Ohio" };

        /// The first person at a post, made the same way for the same post.
        public static Resident First(Post post, PostKind kind, uint modelHash, string id, long now, int year)
        {
            Resident r = Residents.Make(id, post.Id + "|1", modelHash, kind.Woman, post.Town, Trade(kind), now, year);
            r.PostId = post.Id;
            r.BornYear = year - (kind.Old ? 58 + Voice.Hash(post.Id + "a") % 15 : 30 + Voice.Hash(post.Id + "a") % 22);
            return r;
        }

        /// Whoever takes the post after the last holder died: a relative (most of the time), else a newcomer who
        /// bought the business. The same sex as the model the game plays the post with.
        public static Resident Successor(Post post, PostKind kind, Resident before, uint modelHash, string id, long now, int year)
        {
            string seed = post.Id + "|" + (post.Former.Count + 1);
            Resident r = Residents.Make(id, seed, modelHash, kind.Woman, post.Town, Trade(kind), now, year);
            for (int i = 0; i < 6 && First(r.Name) == First(before != null ? before.Name : post.LastName); i++)
                r.Name = Residents.NameFor(kind.Woman, seed + "|again" + i, r.Origin);   // never the same first name as the one before
            r.PostId = post.Id;
            r.KinOf = before != null ? before.Name : post.LastName;
            r.TookOverAt = now;
            string surname = Surname(r.KinOf);
            if (Voice.Hash(seed + "kin") % 100 < 65 && r.KinOf != null)
            {
                string[] kin = kind.Woman ? WomenKin : MenKin;
                r.Kin = kin[Voice.Hash(seed + "rel") % kin.Length];
                bool shares = r.Kin == "son" || r.Kin == "brother" || r.Kin == "nephew" || r.Kin == "daughter" || r.Kin == "sister" || r.Kin == "niece" ||
                              (r.Kin == "cousin" && Voice.Hash(seed + "c") % 2 == 0);
                // a daughter or niece who married keeps her husband's name, a daughter-in-law has it by marriage
                if (kind.Woman && (r.Kin == "daughter" || r.Kin == "niece") && Voice.Hash(seed + "wed") % 100 < 40)
                    shares = false;
                if (r.Kin == "daughter-in-law")
                    shares = true;
                if (shares && surname != null)
                {
                    // kin by blood are of the one before's people: a first name of theirs (one named before 2026-11-02 has no
                    // people noted, and the plain lists stand)
                    if (before != null && before.Origin != r.Origin)
                    {
                        r.Origin = before.Origin;
                        string was = First(before.Name);
                        string first = Residents.FirstName(kind.Woman, seed, r.Origin);
                        for (int i = 0; i < 6 && first == was; i++)
                            first = Residents.FirstName(kind.Woman, seed + "|again" + i, r.Origin);
                        r.Name = first + " " + surname;
                    }
                    else
                        r.Name = First(r.Name) + " " + surname;
                }
                int beforeBorn = before != null ? before.BornYear : year - 50;
                r.BornYear = r.Kin == "son" || r.Kin == "daughter" || r.Kin == "son-in-law" || r.Kin == "daughter-in-law" ? beforeBorn + 22 + Voice.Hash(seed + "y") % 10
                           : r.Kin == "nephew" || r.Kin == "niece" ? beforeBorn + 18 + Voice.Hash(seed + "y") % 12
                           : beforeBorn - 6 + Voice.Hash(seed + "y") % 13;
                r.BornYear = Math.Min(year - 18, r.BornYear);
            }
            else
            {
                r.Kin = null;   // a newcomer
                r.From = FromTowns[Voice.Hash(seed + "from") % FromTowns.Length];
                if (r.From == post.Town)
                    r.From = "Kansas City";
            }
            return r;
        }

        /// TownPostsAfterDeath = survive: a body with a post's model is lying there. Is it the person keeping the post,
        /// shot down just now (the game will bring them back bandaged)? Once per fall: not while they're still down,
        /// and an old body (after a reload, or from before they came back) doesn't count.
        public static bool BodyFells(Resident holder, Post post, string bodyWas, bool livingOneThere, long now)
        {
            if (holder == null)
                return true;   // shot before anyone knew their name
            if (post.FellAt > post.BackAt)
                return false;  // already down: the same fall
            if (bodyWas != null)
                return bodyWas == holder.Id;
            if (livingOneThere)
                return false;
            return !(post.BackAt > 0 && now - post.BackAt < 1440);
        }

        /// A body with a post's model is lying there: does it mean the person keeping the post now has died?
        /// bodyWas: who that body was bound to this session (null if nobody knew it); livingOneThere: someone of the
        /// model is alive right now (one counter, one living person at a time). An old body (left after a reload, or
        /// still lying there when the game staffed the counter again) never takes the successor with it.
        public static bool BodyVacates(Resident holder, Post post, string bodyWas, bool livingOneThere, long now)
        {
            if (holder == null)
                return true;   // killed before anyone knew their name
            if (holder.Dead)
                return false;  // already noted
            if (bodyWas != null)
                return bodyWas == holder.Id;
            if (livingOneThere)
                return false;
            return !(holder.TookOverAt > post.VacantAt && now - holder.TookOverAt < 1440);
        }

        private static string Trade(PostKind k)
        {
            return k.Role.StartsWith("the ") ? k.Role.Substring(4) : k.Role;
        }

        private static string First(string name)
        {
            return string.IsNullOrEmpty(name) ? name : name.Split(' ')[0];
        }

        private static string Surname(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;
            return name.Trim().IndexOf(' ') > 0 ? Residents.SurnameOf(name) : null;   // "Gray Elk" whole
        }

        /// How the successor thinks of the one before: "Pa", "my brother", "Uncle Amos", or the old owner's name.
        public static string Before(Resident r)
        {
            // the one before was played by the same model, so the same sex as the one who took over
            string first = First(r.KinOf) ?? "the old owner";
            bool woman = r.Woman;
            switch (r.Kin)
            {
                case "son":
                case "daughter":
                    return woman ? Voice.Pick(r.Id + "pa", "Ma", "Mother", "Ma") : Voice.Pick(r.Id + "pa", "Pa", "Father", "Pa");
                case "son-in-law":
                case "daughter-in-law":
                    return Voice.Pick(r.Id + "pa", woman ? "the old woman" : "the old man", first);
                case "brother":
                case "sister":
                    return woman ? "my sister" : "my brother";
                case "nephew":
                case "niece":
                    return (woman ? "Aunt " : "Uncle ") + first;
                case "cousin":
                    return first;
                default:
                    return r.KinOf ?? "the old owner";
            }
        }

        /// What the post's person says of how they came to it, for the log and the summary ("son of the late Amos Pryor").
        public static string Story(Resident r)
        {
            if (r.KinOf == null)
                return null;
            return r.Kin != null ? r.Kin + " of the late " + r.KinOf : "bought the business after " + r.KinOf + " died" + (r.From != null ? ", up from " + r.From : "");
        }

        // ---- what they think ---------------------------------------------------------------------------

        private static readonly Dictionary<string, string[]> ByKind = new Dictionary<string, string[]>
        {
            { "store", new[] {
                "Flour's gone damp at the bottom of the barrel. Sell from the top first.",
                "That widow wants credit again. I'll say yes. I always say yes.",
                "Canned peaches won't move at forty cents. Won't move at thirty. Nobody here has ever seen a peach.",
                "Bell over the door. Smile. Hands flat on the counter, where they can see I'm not reaching.",
                "Railroad brings the stock late and the bill early.",
                "Count the till twice. Once for the money, once for my nerves.",
                "Mice in the oat sack again. The cat sleeps on the cracker barrel and calls that work.",
                "Chalked the coffee price up again. They glare at me, never at the railroad.",
                "A boy with one penny takes ten minutes at the candy jars. I don't hurry him. Nobody hurried me.",
                "Farmer's wife came to look at my stoves, then sent off for one from the catalogue. I sold her the stamp.",
                "Ladies finger the calico for an hour and buy a spool of thread.",
                "Rat poison sits two shelves from the sugar. Been meaning to move it since spring.",
                "Had the scale checked by the county man. Customers still watch my thumb.",
                "One lamp chimney broke in the crate. There goes the profit on the other eleven." } },
            { "gunsmith", new[] {
                "Oil and brass and cold iron. Only honest smell in town.",
                "Man wants a repeater and won't say what for. I don't ask. I never ask.",
                "Sold a pistol Tuesday. Saw a coffin go by Friday. Not my business. Is it?",
                "Thumb's still numb from the spring that slipped. Forty years and the spring still wins.",
                "Every customer's a friend till he's loaded.",
                "Keep the good rifle under the counter. That one's for me.",
                "Young fellow stands at the case with his hands behind his back, like it's church. Never buys.",
                "Bore's pitted like a dry creek bed. He asks will it shoot straight. It'll shoot.",
                "Filed a new sear by lamplight. Eyes ache. Trigger breaks clean as a twig now.",
                "Sold six boxes of forty-fours before noon. Nobody hunts deer with that many.",
                "Widow sold me her husband's rifle. Paid her more than it's worth. Nobody needs to know.",
                "Black flecks of powder in my knuckles. Thirty years of them. Soap won't shift them.",
                "Test-fire into the sandbank out back. The whole street jumps, every time.",
                "A man who handles a gun gently is the one to watch. The rough ones mostly hurt themselves." } },
            { "butcher", new[] {
                "Flies are early. Flies know the heat before I do.",
                "Cleaver wants an edge. So do I, before the regulars come in.",
                "Folks want the loin and pay for the neck.",
                "Smell's in my beard, my bed, my wife's hair. She says she don't mind. She minds.",
                "Hunter brought in a buck shot clean through the heart. Respect a man who don't make a mess.",
                "Ice wagon's late. The ice wagon is always late in August.",
                "Pig's head in the window brings the children. Mothers drag them off, then come back for chops.",
                "Apron's stiff by noon. Could stand it in the corner and it'd wait for me.",
                "Sharpened the knives before sunup. Steel on stone is the only quiet I get all day.",
                "Lady wants the tenderest cut and turns her face from the block while I cut it.",
                "Sausage is where the odds and ends go. Nobody asks. Everybody buys.",
                "Man with yellow eyes buys liver every morning. Thinks it'll mend him. I sell it to him.",
                "Paper, string, and the knot my father tied. Forty years of parcels, one knot.",
                "Hands red to the wrist all day. Scrub them for supper. Still pink at bedtime." } },
            { "doctor", new[] {
                "Third cough this week that sounds like my father's did. I never told him either.",
                "They want laudanum. They always want laudanum. Some days I want it myself.",
                "Wash the hands. Wash them again. They laugh about it in the saloon. Nobody laughs in here.",
                "Man paid me in chickens. The chickens are in better health than he is.",
                "Set a leg crooked last spring. He limps past my window every morning to remind me.",
                "I can stitch a bullet hole. I can't stitch whatever sent him looking for one.",
                "Bag packed by the door, boots by the bed. Babies and bullets keep the same hours.",
                "Drover swore he wasn't afraid of a little tooth. Fainted before I'd picked up the pliers.",
                "Journal from Philadelphia, two months stale. I read it like a letter from home.",
                "Told him to give up whiskey. He thanked me kindly and went straight next door.",
                "Mothers want a promise. I don't make promises. I hold a hand instead.",
                "Quinine's nearly gone. Fever season, and the freight's late again.",
                "My handwriting's so bad the druggist has to guess. He mostly guesses right.",
                "Warm the stethoscope in my palm first. They still jump. Everybody jumps." } },
            { "barber", new[] {
                "Razor at a man's throat and he tells me everything. Who owes. Who's sleeping where.",
                "Hot towel, cold water, warm gossip. Two bits.",
                "Hands are steady in here. Always steady in here. It's at home they shake.",
                "That one tips in pennies and talks like a banker.",
                "Same chairs, same jokes, same faces in the mirror. Some days I can't say which side of the glass I'm on.",
                "Strop the blade. Smile. Don't ask about the scar.",
                "Pulled a tooth at nine, trimmed a beard at ten. Same chair, different hollering.",
                "Man asked for it short, then looked in the mirror like I'd robbed him.",
                "Every regular's mug on the shelf with his name on it. Two won't be back. I leave the mugs.",
                "Never talk politics with the razor out. Wait for the towel.",
                "Three bottles of hair tonic gone to men with nothing left to tonic. Hope's a fine customer.",
                "Shaved a dead man for the undertaker this morning. Talked to him the whole time. Habit.",
                "Hand on a man's jaw, you feel the lie before he's done telling it.",
                "Saturday line out the door. Everybody wants a clean face for a sermon they'll sleep through." } },
            { "barkeep", new[] {
                "Wipe the glass. Wipe it again. Gives a man somewhere to put his eyes.",
                "Watered the rye a touch. Nobody tastes it after the third.",
                "That one's had enough. Telling him so is the dangerous part.",
                "Three confessions tonight and not one of them sober.",
                "Piano's out of tune. The player's out of tune. The customers are out of coin.",
                "Scattergun under the bar. Never fired it. Oil it every Sunday all the same.",
                "Man wanted ice in his whiskey. Told him to come back in January.",
                "Swept up a tooth with the cigar ends this morning. Third this month. They go in a jar.",
                "Free lunch with a nickel beer, and that one's been nursing the same nickel since noon.",
                "He says his wife doesn't understand him. I understand him fine. That's his trouble.",
                "Slate's full of tabs. Three of the names are in the churchyard. Never rubbed them out. Don't mean to.",
                "Never drink on my side of the bar. Every barkeep I knew who did ended up on the other.",
                "Big mirror behind me. I've learned to read the backs of men's heads.",
                "Fights come at closing. Always at closing. Some nights I'd shut at nine to spite them." } },
            { "hotel", new[] {
                "Room four paid for a week and left in the night. Took the good blanket with him.",
                "Bedbugs again. Don't say it out loud. Never say it out loud.",
                "Strangers sign the book with names they made up on the stairs.",
                "Mattresses want restuffing. Guests want quiet. Neither comes cheap.",
                "A lady came in under one name and left under another. I keep the book. I don't read it.",
                "Drummer in two snores loud enough to rattle the water jug in three.",
                "Count the keys still on the hooks. Each one's a dollar I'm not getting tonight.",
                "Walk the hall at midnight sniffing at doors. Somebody always falls asleep with a lamp lit.",
                "Newlyweds in the front room haven't come down to breakfast in two days. I charge for it regardless.",
                "He wanted the room by the back stairs. Gave it him. Been wondering why ever since.",
                "Bell on the desk rang nine times today. Eight were the old lady in five wanting hot water.",
                "Sheets so thin you could read the paper through them. One more boiling and they're lace.",
                "Everybody wants a front room, then complains about the street all night." } },
            { "undertaker", new[] {
                "Business is good. Lord forgive me, business is good.",
                "Pine for the poor, oak for the proud. The same worms either way.",
                "Measured a man today who stood me a drink last week.",
                "Families pay best in the first hour. Grief doesn't haggle.",
                "Hands smell of the lye. Nobody shakes them twice.",
                "Every stranger who rides in, I guess his length. Can't help it.",
                "Ordered a dozen pine boards on credit. The lumberman never asks when I'll pay. He knows I will.",
                "Dead men never complain of the fit. Their widows do.",
                "Children dare each other to knock on my door. One did today. I opened it. He's still running.",
                "Dressed a cowhand in the only suit he ever wore. Looked like a stranger. They mostly do.",
                "Folks cross the street to miss me, then send for me by name.",
                "Small box today. Left the brass handles off the bill. Couldn't write them in.",
                "Dug three graves ahead in October, before the ground went hard. Somebody'll fill them by March.",
                "Hum on the drive home from a burying. The horse never tells." } },
            { "photographer", new[] {
                "Hold still. Nobody holds still. Only the dead hold still, and they're my best sitters.",
                "Silver under the fingernails. Black as a coal man's.",
                "They want to look rich in the picture. Lend them the good chair. Lend them the good hat.",
                "Glass plates cost more than the sitting. Drop one and I've paid them to come.",
                "Nobody smiles for the camera. Smiling is for fools and children.",
                "Flash powder took my eyebrows in the spring. They grew back crooked.",
                "Painted backcloth of an English garden. Every cattleman in the county has stood in front of it.",
                "Mother sits under a shawl holding the baby still. On the card she's a lump of shawl.",
                "Red lamp, the chemicals, the door shut. Nobody knocks on a darkroom. I could live in here.",
                "Old couple came in their wedding clothes. Tight in places. Stood like it was the first day.",
                "Took a mole off the banker's wife with a pencil on the plate. She says the camera finally got her right.",
                "Sold a print of the hanging to a man passing through. A dollar. I don't look at that plate any more.",
                "Best light through the skylight comes at eleven. Eleven goes to whoever pays most." } },
            { "station", new[] {
                "Twelve-ten is late. The twelve-ten is a rumour.",
                "Telegraph's been clicking all morning. Nothing good ever comes that fast.",
                "Everybody's leaving. Nobody arrives but drummers and trouble.",
                "Company pays me to know the timetable and blame the weather.",
                "Freight for the store, freight for the saloon, nothing for me. Never anything for me.",
                "One way to Saint Denis. They always want one way.",
                "Girl travelling alone, ticket to the end of the line. Wired the next agent to look out for her.",
                "Coffin in the freight shed waiting on the eastbound. Two days now. I wish it good morning.",
                "Talk to twenty stations a day on the key, and not a soul in this one.",
                "Noon signal comes down the wire and I set my watch. Half the town sets theirs by mine.",
                "Bought his ticket, sat on the platform all night, let two trains go. Not my place to ask.",
                "Cinder in my eye from the westbound. Second this week. The railroad marks its own.",
                "Mailbags off, mailbags on. Love letters and dunning notices in the same sack.",
                "Stove in the waiting room eats coal faster than the company sends it." } },
            { "trader", new[] {
                "Pelt's got a hole in it. Hunter swears it don't. Hunter's a liar.",
                "City folk pay a fortune for feathers. The birds give them away.",
                "Smell of hides on everything I own. Even the money.",
                "Man brought a skin I'd never seen. Didn't ask where from. Paid him fair. Fair enough.",
                "Ledger says I'm ahead. Feels like I'm behind.",
                "He wants three dollars for the wolf. I say two. We both knew it'd be two and a half before he came in.",
                "Salt the hides, stack the hides, count the hides. Hands are half leather themselves now.",
                "Old trapper paid me in gold dust once and never came back. Still wonder about him.",
                "Bear grease outsells the bearskin. Men comb it into their hair for church.",
                "County pays the bounty on the ears. I pay on everything else.",
                "Moths got into the beaver over summer. A season's work, eaten by something I can't shoot.",
                "Every trapper swears this pelt's his best yet. I nod. I keep a nod for each of them.",
                "Buyer from back east took six sets of antlers to hang his hats on." } },
            { "livery", new[] {
                "A horse knows when a man's mean to it. So do I.",
                "Muck out, feed, water, muck out. The horses get three square meals. I get one.",
                "That bay's favouring the near fore. Owner won't hear it.",
                "Hay's gone up again. Horses don't care about the price. Lucky horses.",
                "Stranger's horse is better kept than he is. Tells you something.",
                "Rented the grey to a bank clerk. Grey came home alone at dusk. Clerk limped in after ten.",
                "Smith's late with the shoes. The horse stands on three legs and blames me.",
                "Sleep up in the loft. Hear every horse breathing. Better than a clock.",
                "That sorrel's board is a month behind. If the owner's not by Friday, the sorrel's mine.",
                "The mule bites everybody but me. Took two years and most of a sack of apples.",
                "Extra scoop of oats for the old mare. She won't see another winter. Owner needn't know.",
                "Liniment on my hands smells of pine. Horses come to it. People step away.",
                "Hay fork's got a cracked handle. Bound it with wire. Half this barn is held up with wire." } },
            { "tailor", new[] {
                "Pins in my mouth all day. Swallowed one in '91. Still waiting to hear where it went.",
                "Rich men want it tight. Working men want room to swing a pick.",
                "Cloth from Manchester, thread from Philadelphia, and a customer who won't pay till Christmas.",
                "Measure twice, cut once, apologise forever.",
                "Eyes are going. The needle finds my thumb more than the cloth.",
                "Let out the banker's waistcoat for the third time this year. Said nothing. Charged a little more.",
                "Took in a widow's dress an inch at the waist. Second time since the funeral. She isn't eating.",
                "Treadle squeaks. Oiled it twice. Still squeaks. Some things are just complainers.",
                "Gun on the hip pulls a man's shoulder down. I cut for it without being asked.",
                "Same bolt of black serge made a wedding coat Monday and a burying coat Thursday.",
                "Wants the coat from a Chicago paper, by Saturday, at a country price.",
                "Forty buttonholes to a coat, all by lamplight. I dream in buttonholes.",
                "Irons heating on the stove all day. July comes twice in here." } }
        };

        // a relative who took over: in the first days, and once they've settled
        private static readonly string[] KinFresh =
        {
            "Still smells of {before}'s tobacco back here. Can't bring myself to move the chair.",
            "Folks keep saying sorry. Sorry don't open the shutters in the morning.",
            "{Before} kept the ledger in a hand I can barely read. Now it's my ledger.",
            "Put the black band on my sleeve and the price cards in the window. Same morning.",
            "Every time the bell goes, I look up for {before}."
        };

        private static readonly string[] KinSettled =
        {
            "{Before} would've charged that man double. I'm softer. The ledger shows it.",
            "Some mornings I still wait for {before} to unlock the door.",
            "Customers say I've got {before}'s way of counting change. I didn't know I was watching.",
            "Never wanted this counter. Now I can't picture my hands anywhere else."
        };

        private static readonly string[] NewcomerLines =
        {
            "Bought the place cheap. Didn't ask why it was cheap. Now I know.",
            "Folks here measure me against a dead man. The dead man always wins.",
            "Locals still call it {before}'s place. I paid good money for it to be mine.",
            "Came west for a fresh start. Got a dead man's counter and his debts."
        };

        // they know the player killed the one before
        private static readonly string[] KnowsKiller =
        {
            "There he is. The man who killed {before}. Keep your hands where he can see them.",
            "My hands won't stop. Count his change. Don't look at the gun.",
            "Sheriff says nothing's to be done. The sheriff never stood across a counter from him.",
            "Smile at him. Serve him. Pray he goes, and pray he never comes back."
        };

        // nobody says who did it
        private static readonly string[] NobodySaw =
        {
            "Nobody saw who did it. Or nobody will say.",
            "Every stranger who comes through that door, I wonder. Him? Him?",
            "Somebody in this town knows. Somebody always knows.",
            "I look at hands now. Whoever did it had hands like anybody's."
        };

        // shot down at the counter and back (the game's bandages): in the first days, and later
        private static readonly string[] WoundFresh =
        {
            "Doc dug the ball out with a spoon and a prayer. Says I'm lucky. Doesn't feel lucky.",
            "Every breath pulls the stitches. Smile for the customers anyway.",
            "Bandage wants changing. Can't reach it. Won't ask.",
            "Should be dead. Lay on those boards and heard them say so. Then I wasn't.",
            "Laudanum takes the edge off the arm and puts it on everything else."
        };

        private static readonly string[] WoundSettled =
        {
            "Scar itches when it rains. Reminds me I'm still here.",
            "Jump at every bell now. Every single bell.",
            "Scrubbed my own blood off these boards. You never get it all out of the grain.",
            "Folk stare as if a ghost were minding the shop.",
            "Dream of that floor most nights. Wake up counting the change."
        };

        // the one who shot them is standing at the counter: by temperament
        private static readonly string[] ShooterAfraid =
        {
            "That's the man who shot me. Don't look at him. Don't look at the gun.",
            "Hands shaking on the counter. Serve him. Let him go. Let him go.",
            "If he does it again, no doctor will be quick enough.",
            "The scar pulls when he walks in. The body knows him before I do."
        };

        private static readonly string[] ShooterBitter =
        {
            "Shot me, and walks back in to buy. Some men have no shame. I have a long memory.",
            "Keep smiling. One day the law catches up with him, and I'll be there to watch.",
            "He stands where he stood when he shot me. I'll not step back. Not this time.",
            "He put me on these boards once. He'll not see me flinch at my own counter."
        };

        private static readonly string[] ShooterCold =
        {
            "He shot me once. Still a paying customer. The business holds no grudges. I do.",
            "Took a bullet off that one. Took his money since, too. We're square, near enough.",
            "Shot me, bought a hat, tipped it. There's a kind of honesty in that.",
            "His coin spends the same as any. The hole in me cost more than he's spent. So far."
        };

        /// The town hears that someone shot down at their counter is back at it. Null if nothing to say.
        public static string BackNews(Resident r, Post post)
        {
            return r == null ? null : r.Name + " is back behind the counter in " + post.Town + ", bandaged. Those who saw " + (r.Woman ? "her" : "him") + " fall don't like to look.";
        }

        // how well they know the player by now
        private static readonly string[] NewFace =
        {
            "New face. Watch the hands, then the boots. Boots tell you where a man's been.",
            "Never seen him before. He looks at the door like he's already measured it.",
            "Stranger. Trail dust on him and no hurry in him. Those are the ones to watch.",
            "Don't know him. Don't know his horse. I'll know his money soon enough."
        };

        private static readonly string[] Regular =
        {
            "Him again. Third time this week. Either he likes the place or he's learning the exits.",
            "Knows where everything is now. Walks in like he owns it.",
            "Regular, now. Pays in coin. Never says where it came from. Not my business.",
            "Back again. I've started setting his usual on the counter before he asks."
        };

        /// What the person at a post is thinking. near: the player is close. seen: how often they've seen him.
        /// Null if nothing of the post's own comes up (then they think like anyone else).
        public static string Thought(Resident r, Post post, PostKind kind, bool near, int seen, long now, string seed)
        {
            if (r.Wounds > 0)
            {
                // shot down here and brought back: the wound, and the one who gave it
                if (near && r.WoundedByPlayer && r.KnowsShooter && Voice.Chance(seed + "sh", 70))
                {
                    string[] by = r.Personality == "Hothead" || r.Personality == "Proud" ? ShooterBitter
                                : r.Personality == "Cold" || r.Personality == "Greedy" ? ShooterCold : ShooterAfraid;
                    return Voice.Pick(seed + "shp", by);
                }
                bool raw = now - r.WoundedAt < 3 * 1440;
                if (Voice.Chance(seed + "w", raw ? 55 : 20))
                    return Voice.Pick(seed + "wp", raw ? WoundFresh : WoundSettled);
            }
            string before = r.KinOf != null ? Before(r) : null;
            Func<string, string> fill = s => before == null ? s : s.Replace("{before}", before).Replace("{Before}", Upper(before));
            bool fresh = r.TookOverAt > 0 && now - r.TookOverAt < 3 * 1440;
            if (before != null && near && post.KilledByPlayer && post.KillerKnown && Voice.Chance(seed + "k", 70))
                return fill(Voice.Pick(seed + "kk", KnowsKiller));
            if (before != null && Voice.Chance(seed + "b", fresh ? 60 : 25))
            {
                if (r.Kin == null)
                    return fill(Voice.Pick(seed + "n", NewcomerLines));
                if (post.KilledByPlayer && !post.KillerKnown && Voice.Chance(seed + "ns", 30))
                    return Voice.Pick(seed + "nn", NobodySaw);
                return fill(Voice.Pick(seed + "kin", fresh ? KinFresh : KinSettled));
            }
            if (near && Voice.Chance(seed + "seen", 30))
                return Voice.Pick(seed + "s", seen <= 1 ? NewFace : Regular);
            string[] pool;
            if (kind != null && ByKind.TryGetValue(kind.Kind, out pool) && Voice.Chance(seed + "role", 65))
                return Voice.Pick(seed + "r", pool);
            return null;
        }

        /// Every line of the posts, for the tests.
        public static IEnumerable<string> AllLines()
        {
            return ByKind.Values.SelectMany(x => x).Concat(WoundFresh).Concat(WoundSettled).Concat(ShooterAfraid).Concat(ShooterBitter).Concat(ShooterCold).Concat(KinFresh).Concat(KinSettled).Concat(NewcomerLines).Concat(KnowsKiller).Concat(NobodySaw).Concat(NewFace).Concat(Regular);
        }

        public static bool HasPool(string kind)
        {
            return ByKind.ContainsKey(kind);
        }

        private static string Upper(string s)
        {
            return string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);
        }
    }
}
