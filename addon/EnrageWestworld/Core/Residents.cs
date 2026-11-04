// Residents: everyone who isn't one of enRAGE's own people. A resident is a small identity the add-on owns (a
// name, a trade, a town, a temperament) given to an ordinary person the moment something asks who they are.
// They are never added to enRAGE's roster, and nothing is ever written back to enRAGE about them.
// Only residents that something has happened to are saved (woken, talked to, remembered something); the rest
// are made up again if met again. The names are the add-on's own lists of common period names.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace EnrageWestworld.Core
{
    [DataContract]
    public sealed class Resident
    {
        public const string Prefix = "res:";

        [DataMember(EmitDefaultValue = false)] public string Id;
        [DataMember(EmitDefaultValue = false)] public string Name;
        [DataMember(EmitDefaultValue = false)] public bool Woman;
        [DataMember(EmitDefaultValue = false)] public uint Model;            // the ped model they were met in; a returning resident is cast on a matching passer-by
        [DataMember(EmitDefaultValue = false)] public string Town;
        [DataMember(EmitDefaultValue = false)] public string Trade;          // "a farmhand", "a townswoman"
        [DataMember(EmitDefaultValue = false)] public string Personality;    // Hothead, Coward, Proud, Cold, Greedy, Loyal, Drifter
        [DataMember(EmitDefaultValue = false)] public string Disposition;    // Friendly, Neutral, Surly
        [DataMember(EmitDefaultValue = false)] public string Sociability;    // Introvert, Outgoing
        [DataMember(EmitDefaultValue = false)] public string Devotion;       // Devout, Churchgoer, Lapsed, Godless
        [DataMember(EmitDefaultValue = false)] public string Smarts;         // Sharp, Average, Dim
        [DataMember(EmitDefaultValue = false)] public int BornYear;
        [DataMember(EmitDefaultValue = false)] public long MetAt;            // game minute they were made
        [DataMember(EmitDefaultValue = false)] public long TouchedAt;        // game minute something last happened to them
        [DataMember(EmitDefaultValue = false)] public bool Dead;
        [DataMember(EmitDefaultValue = false)] public long DiedAt;          // game minute they died (Core/Ghosts.cs: a save loaded from before it brings them back); 0 on old saves means leave them dead
        [DataMember(EmitDefaultValue = false)] public string PostId;         // the counter they keep (see Posts.cs), if any
        [DataMember(EmitDefaultValue = false)] public string Kin;            // how they're related to the one who kept it before ("son"), null for a newcomer
        [DataMember(EmitDefaultValue = false)] public string KinOf;          // the one who kept it before, by name
        [DataMember(EmitDefaultValue = false)] public string From;           // a newcomer's old town
        [DataMember(EmitDefaultValue = false)] public bool ByTrain;          // stepped down from a train (Core/Engine.Trains.cs): the ledger's arrivals; a post's successor has Kin too, but came no such way
        [DataMember(EmitDefaultValue = false)] public string Since1899;      // the epilogue (Core/Epilogue.cs): what the eight years did to one of Arthur's people carried into John's world (stayed, married, widowed, moved, died)
        [DataMember(EmitDefaultValue = false)] public long TookOverAt;       // game minute they took the post over, 0 for the first
        [DataMember(EmitDefaultValue = false)] public int Wounds;            // times they were shot down at their counter and the game brought them back
        [DataMember(EmitDefaultValue = false)] public long WoundedAt;
        [DataMember(EmitDefaultValue = false)] public bool WoundedByPlayer;
        [DataMember(EmitDefaultValue = false)] public bool KnowsShooter;     // they saw, or someone told them, that it was the player
        [DataMember(EmitDefaultValue = false)] public bool Fled;             // woke and walked out of their town (the wake acts' escape): lives rough now, never recast on a passer-by
        [DataMember(EmitDefaultValue = false)] public long FledAt;
        [DataMember(EmitDefaultValue = false)] public string Held;           // the trial's or the hanging's event id while the law holds them at the jail (Core/Trials.cs); null when free
        [DataMember(EmitDefaultValue = false)] public bool Left;             // took the train out of their town for good (Core/Trains.cs); Fled is set with it, so no cast finds them
        [DataMember(EmitDefaultValue = false)] public long LeftAt;
        [DataMember(EmitDefaultValue = false)] public bool HasCamp;          // where they live in the wilds, chosen once
        [DataMember(EmitDefaultValue = false)] public float CampX, CampY, CampZ;
        [DataMember(EmitDefaultValue = false)] public bool Regular;          // one of their town's regulars (Regulars.cs): met again and again, kept
        [DataMember(EmitDefaultValue = false)] public int Outfit;            // a regular's outfit preset, put on whenever they're cast (read only when Regular or Cast)
        [DataMember(EmitDefaultValue = false)] public string Cast;           // one of the Testing page's test cast, by part (Core/TestCast.cs); null for everyone else
        [DataMember(EmitDefaultValue = false)] public string Origin;         // their people, as their name was chosen for (Core/Origins.cs); null for the plain American lists, and for everyone named before 2026-11-02 (they keep their names)
        [DataMember(EmitDefaultValue = false)] public string Look;          // the look of the body they were met in (Game/Looks.cs), so standing alone the same body is known for them again ([World] OwnLooks; night 2026-10-29, item 5); null for everyone met before, or with it off

        public static bool IsResident(string id)
        {
            return id != null && id.StartsWith(Prefix);
        }
    }

    public static class Residents
    {
        private static readonly string[] MenFirst =
        {
            "Abel", "Albert", "Alfred", "Amos", "Andrew", "Asa", "August", "Barnaby", "Benjamin", "Calvin", "Carl", "Charles", "Clarence", "Cyrus",
            "Daniel", "David", "Dennis", "Edgar", "Edmund", "Edward", "Eli", "Ephraim", "Ezra", "Frank", "Frederick", "George", "Gideon", "Grover",
            "Harlan", "Harvey", "Henry", "Herman", "Horace", "Hugh", "Isaac", "Jacob", "James", "Jasper", "Jedediah", "Jesse", "John", "Joseph",
            "Josiah", "Lars", "Lemuel", "Levi", "Lewis", "Louis", "Luther", "Martin", "Matthew", "Milo", "Nathan", "Nelson", "Obadiah", "Oliver",
            "Orville", "Oscar", "Owen", "Patrick", "Pierce", "Rufus", "Samuel", "Seth", "Silas", "Simon", "Stephen", "Thaddeus", "Thomas", "Tobias",
            "Virgil", "Walter", "Warren", "Wilbur", "William", "Zachariah"
        };

        private static readonly string[] WomenFirst =
        {
            "Abigail", "Adelaide", "Agnes", "Alice", "Amelia", "Anna", "Beatrice", "Bertha", "Carrie", "Catherine", "Cecilia", "Charlotte", "Clara",
            "Cora", "Dorothy", "Edith", "Eleanor", "Eliza", "Ella", "Emma", "Esther", "Ethel", "Eunice", "Florence", "Frances", "Georgia", "Grace",
            "Hannah", "Harriet", "Hattie", "Helen", "Henrietta", "Ida", "Irene", "Isabel", "Jane", "Josephine", "Judith", "Julia", "Katherine",
            "Lavinia", "Lena", "Lillian", "Lottie", "Louisa", "Lucy", "Mabel", "Margaret", "Martha", "Mary", "Matilda", "Minnie", "Nellie", "Olive",
            "Opal", "Pearl", "Phoebe", "Rachel", "Rebecca", "Rosa", "Ruth", "Sadie", "Sarah", "Susannah", "Sylvia", "Theodora", "Viola", "Winifred"
        };

        private static readonly string[] Surnames =
        {
            "Abbott", "Ackerman", "Adair", "Alder", "Ames", "Archer", "Ashby", "Bailey", "Baxter", "Beckett", "Bellamy", "Bishop", "Blackwood",
            "Boone", "Bowman", "Brannigan", "Briggs", "Brockway", "Calloway", "Carmichael", "Carver", "Chandler", "Clayton", "Coleman", "Cobb",
            "Crandall", "Cutler", "Dalton", "Davenport", "Dawes", "Delaney", "Dillard", "Donovan", "Drummond", "Eastman", "Eldridge", "Ellison",
            "Emerson", "Fairchild", "Farrow", "Fenton", "Fitch", "Fletcher", "Foster", "Gallagher", "Garrity", "Gentry", "Gilbert", "Goodwin",
            "Granger", "Gunderson", "Haggerty", "Halloran", "Hammond", "Hartley", "Hastings", "Hatfield", "Hawkins", "Hendry", "Holloway",
            "Hubbard", "Ingram", "Jennings", "Kearney", "Kimball", "Kirby", "Lambert", "Lacey", "Larkin", "Lathrop", "Lindqvist", "Lowry",
            "Mabry", "Maddox", "Mallory", "Marsh", "Maxwell", "McAllister", "McBride", "McKenna", "Merritt", "Mercer", "Mulligan", "Munro",
            "Nash", "Nolan", "Norwood", "Oakes", "Odell", "Parrish", "Pemberton", "Pickett", "Pruitt", "Quigley", "Radford", "Rafferty",
            "Redmond", "Rhodes", "Ridley", "Rowland", "Rutledge", "Sanderson", "Sawyer", "Schuyler", "Shelby", "Sinclair", "Slater", "Spencer",
            "Stanton", "Stockdale", "Sullivan", "Talbot", "Tanner", "Thackeray", "Thorne", "Tolliver", "Tucker", "Underhill", "Vance", "Vickers",
            "Wadsworth", "Waller", "Walsh", "Wheeler", "Whitaker", "Whitlow", "Wickham", "Winslow", "Wolcott", "Wyatt", "Yardley", "Yates"
        };

        // ---- names by a person's people (Core/Origins.cs; a player, 2026-11-02: names "sometimes aren't accurate to the ethnic
        // background of the NPC"). Plain letters only: the card's width table and the game's fonts are sure of nothing else, and
        // the period's records mostly dropped the accents anyway.
        private sealed class NameSet
        {
            public readonly string[] Men, Women, Surnames;
            public NameSet(string[] men, string[] women, string[] surnames) { Men = men; Women = women; Surnames = surnames; }
        }

        private static readonly Dictionary<string, NameSet> Sets = new Dictionary<string, NameSet>
        {
            // freedmen's children and grandchildren mostly had the same English names as their neighbours; the mix leans to the
            // names of the time's Black South, and the surnames to those taken at emancipation
            [Origins.Black] = new NameSet(
                new[] { "George", "John", "William", "James", "Henry", "Charles", "Moses", "Isaiah", "Jesse", "Willis", "Lucius", "Elijah",
                        "Rufus", "Ezekiel", "Abraham", "Silas", "Ulysses", "Hiram", "Prince", "Solomon", "Jeremiah", "Israel", "Nathaniel",
                        "Cyrus", "Joshua", "Isaac", "Peter", "Amos", "Clement", "Sampson" },
                new[] { "Mary", "Hattie", "Fannie", "Mattie", "Lula", "Rosetta", "Delia", "Phyllis", "Minerva", "Cora", "Ida", "Lizzie",
                        "Hester", "Josephine", "Sallie", "Ella", "Louvenia", "Easter", "Patience", "Mahala", "Viola", "Martha", "Harriet",
                        "Amanda", "Charity", "Ophelia", "Rhoda" },
                new[] { "Washington", "Jefferson", "Jackson", "Johnson", "Freeman", "Brown", "Williams", "Jones", "Thomas", "Robinson",
                        "Harris", "Lewis", "Walker", "Green", "Gaines", "Battle", "Grant", "Hall", "Banks", "Moore", "Scott", "Simmons",
                        "Dabney", "Mayfield", "Barnes", "Crawford", "Booker", "Pettiford", "Toussaint", "Duplessis", "Coleman", "Holmes",
                        "Fields", "Bell", "Terrell", "Hollins" }),
            [Origins.Hispanic] = new NameSet(
                new[] { "Jose", "Juan", "Manuel", "Francisco", "Antonio", "Jesus", "Pedro", "Miguel", "Ramon", "Luis", "Rafael", "Joaquin",
                        "Santiago", "Alejandro", "Ignacio", "Esteban", "Tomas", "Andres", "Diego", "Felipe", "Emilio", "Rodrigo", "Agustin",
                        "Cipriano", "Eusebio", "Teodoro", "Severo", "Anastacio", "Lorenzo", "Hilario" },
                new[] { "Maria", "Guadalupe", "Josefa", "Juana", "Carmen", "Dolores", "Refugio", "Rosa", "Francisca", "Teresa", "Ana",
                        "Concepcion", "Luz", "Isabel", "Soledad", "Pilar", "Mercedes", "Ramona", "Ines", "Catalina", "Petra", "Manuela",
                        "Encarnacion", "Rosario", "Trinidad" },
                new[] { "Garcia", "Martinez", "Hernandez", "Lopez", "Gonzalez", "Rodriguez", "Perez", "Sanchez", "Ramirez", "Flores", "Torres",
                        "Vasquez", "Morales", "Castillo", "Ortiz", "Reyes", "Gutierrez", "Chavez", "Mendoza", "Romero", "Herrera", "Medina",
                        "Aguilar", "Salazar", "Delgado", "Navarro", "Rios", "Contreras", "Vega", "Ochoa", "Escobar", "Villanueva", "Cordero",
                        "Montoya", "Baca", "Lujan", "Archuleta", "Trujillo", "Sandoval", "Padilla" }),
            // as Chinatown's men and women were written down in America: the given name first, the family name after
            [Origins.Chinese] = new NameSet(
                new[] { "Sing", "Kee", "Hing", "Wah", "Fook", "Lung", "Yick", "Gim", "Poy", "Hop", "Chung", "Wing", "Tong", "Ming", "Toy",
                        "Quong", "Lim", "Yat", "Bow", "Jip", "Sam", "Foon", "Gong", "Hoy", "Kum", "Tai" },
                new[] { "Mei", "Lin", "Ying", "Sue", "Gum", "Choy", "Yuk", "Lan", "Oi", "Ho", "Kim", "Lai", "Suey", "Yee", "Moy", "Fung" },
                new[] { "Wong", "Lee", "Chin", "Chan", "Lum", "Yee", "Fong", "Chew", "Moy", "Leong", "Jung", "Quan", "Ng", "Tom", "Louie",
                        "Gee", "Hom", "Dong", "Mah", "Yuen", "Lau", "Kwong", "Eng", "Low", "Fung", "Jow" }),
            // as the agency rolls of the time wrote the people of the plains: a mission name, and their own name in English after
            [Origins.Native] = new NameSet(
                new[] { "Joseph", "John", "Thomas", "Samuel", "Daniel", "Paul", "James", "Peter", "David", "Jacob", "Andrew", "Simon",
                        "Charles", "Moses", "Philip", "Luke", "Mark", "Isaac", "Stephen", "Matthew" },
                new[] { "Mary", "Sarah", "Lucy", "Rebecca", "Anna", "Ruth", "Martha", "Rachel", "Esther", "Hannah", "Susan", "Julia",
                        "Agnes", "Louisa", "Grace", "Lydia", "Naomi" },
                new[] { "Gray Elk", "Walks Far", "Many Horses", "Iron Shell", "Little Hawk", "Two Rivers", "Black Cloud", "Swift Fox",
                        "High Wolf", "Lone Pine", "Yellow Bird", "Morning Star", "Quiet Water", "Blue Shawl", "Kills Enemy", "White Horse",
                        "Red Hail", "Spotted Elk", "Bear Robe", "Good Voice", "Runs Above", "Long Grass", "Brings Plenty", "Hollow Horn",
                        "Eagle Shield", "Low Dog", "Crow Feather", "Last Horse", "Fast Thunder", "Two Moons", "Standing Elk" }),
            [Origins.Irish] = new NameSet(
                new[] { "Patrick", "Michael", "Cornelius", "Timothy", "Dennis", "Daniel", "Thomas", "Francis", "Hugh", "Owen", "Bartholomew",
                        "Jeremiah", "Lawrence", "Peter", "Malachy", "Fergus", "Terence", "Cormac", "Desmond", "Brian", "Martin", "Edward" },
                new[] { "Bridget", "Honora", "Ellen", "Johanna", "Nora", "Kathleen", "Delia", "Mary", "Margaret", "Annie", "Winifred",
                        "Brigid", "Maura", "Eileen", "Ellie", "Una", "Catherine", "Julia" },
                new[] { "Murphy", "Kelly", "O'Brien", "Ryan", "O'Sullivan", "Byrne", "Doyle", "Kennedy", "Lynch", "Daly", "Connolly",
                        "Flanagan", "Maguire", "Quinn", "Brennan", "Fitzgerald", "Callahan", "Dunne", "Hogan", "Moran", "Keane", "Duffy",
                        "Regan", "Shea", "Mahoney", "Brady", "Carroll", "Toomey", "McGrath", "Kavanagh", "O'Neill", "Sweeney" }),
            [Origins.French] = new NameSet(
                new[] { "Jean", "Pierre", "Louis", "Alcide", "Ovide", "Theophile", "Achille", "Hippolyte", "Aristide", "Gustave", "Jules",
                        "Adolphe", "Emile", "Octave", "Numa", "Valsin", "Lucien", "Etienne", "Honore", "Placide", "Ulger", "Marcel" },
                new[] { "Marie", "Celeste", "Odile", "Aurelie", "Eulalie", "Josephine", "Clothilde", "Delphine", "Marguerite", "Felicie",
                        "Azelie", "Philomene", "Euphemie", "Odalie", "Adele", "Victorine", "Leontine", "Noemi", "Corinne" },
                new[] { "Boudreaux", "Thibodeaux", "LeBlanc", "Fontenot", "Broussard", "Guidry", "Landry", "Hebert", "Arceneaux", "Robichaux",
                        "Dupre", "Mouton", "Delacroix", "Fortier", "Prejean", "Savoie", "Breaux", "Richard", "Theriot", "Comeaux", "Gautreaux",
                        "Lacour", "Benoit", "Chauvin", "Doucet", "Marchand", "Villere", "Roman", "Sauve", "Trosclair" }),
            [Origins.Italian] = new NameSet(
                new[] { "Giuseppe", "Salvatore", "Antonio", "Vincenzo", "Francesco", "Carmelo", "Giovanni", "Pietro", "Calogero", "Domenico",
                        "Luigi", "Angelo", "Gaetano", "Natale", "Sebastiano", "Paolo" },
                new[] { "Maria", "Rosaria", "Concetta", "Giuseppina", "Angela", "Lucia", "Carmela", "Teresa", "Antonina", "Francesca",
                        "Vincenza", "Grazia", "Santa", "Rosalia" },
                new[] { "Russo", "Esposito", "Marino", "Romano", "Ferraro", "Caruso", "Lombardi", "Greco", "Costa", "Ricci", "Bruno", "Gallo",
                        "Puglisi", "Messina", "Lo Cicero", "Taormina", "Scaglione", "Bonura", "Cusimano", "Marchese", "Palermo", "Monteleone" }),
            [Origins.German] = new NameSet(
                new[] { "Heinrich", "Friedrich", "Wilhelm", "Karl", "Otto", "Gottlieb", "Hermann", "August", "Ernst", "Johann", "Konrad",
                        "Ludwig", "Gustav", "Albrecht" },
                new[] { "Gretchen", "Wilhelmina", "Frieda", "Elsa", "Hedwig", "Katharina", "Anna", "Bertha", "Louisa", "Margarethe",
                        "Johanna", "Emma", "Ottilie" },
                new[] { "Schmidt", "Becker", "Hoffman", "Wagner", "Keller", "Fischer", "Zimmerman", "Krueger", "Vogel", "Schultz", "Weber",
                        "Bauer", "Koch", "Richter", "Hartmann", "Brandt", "Sommer", "Lehmann", "Dietrich", "Engel" }),
            [Origins.Scandinavian] = new NameSet(
                new[] { "Lars", "Nils", "Anders", "Gunnar", "Olaf", "Sven", "Erik", "Johan", "Knut", "Halvor", "Per", "Magnus", "Axel", "Ole" },
                new[] { "Ingrid", "Karin", "Sigrid", "Astrid", "Britta", "Kristina", "Ingeborg", "Gudrun", "Hilda", "Elin", "Signe", "Maren" },
                new[] { "Larsen", "Nilsson", "Johansson", "Berg", "Lund", "Halvorsen", "Strand", "Olsen", "Lindqvist", "Gunderson", "Dahl",
                        "Sandberg", "Bergstrom", "Hagen", "Nygaard", "Ekdahl", "Swenson", "Thorsen" })
        };

        /// A person's whole name for their people (Origins); null, or a people with no lists, gives the plain American lists
        /// exactly as before, so a seed named before the change gives the same name.
        public static string NameFor(bool woman, string seed, string origin)
        {
            NameSet s;
            if (origin == null || !Sets.TryGetValue(origin, out s))
                return NameFor(woman, seed);
            return FirstName(woman, seed, origin) + " " + s.Surnames[Voice.Hash(seed + "s") % s.Surnames.Length];
        }

        /// A first name alone for someone of these people (a child, a sweetheart).
        public static string FirstName(bool woman, string seed, string origin)
        {
            NameSet s;
            if (origin == null || !Sets.TryGetValue(origin, out s))
                return FirstName(woman, seed);
            string[] first = woman ? s.Women : s.Men;
            return first[Voice.Hash(seed + "f") % first.Length];
        }

        /// The people a name's lists belong to, if they're one of the sets (the harness's check).
        public static bool InPeoplesLists(string origin, string full)
        {
            NameSet s;
            if (origin == null || full == null || !Sets.TryGetValue(origin, out s))
                return false;
            string first = full.Split(' ')[0];
            string last = SurnameOf(full);
            return (Array.IndexOf(s.Men, first) >= 0 || Array.IndexOf(s.Women, first) >= 0) && Array.IndexOf(s.Surnames, last) >= 0;
        }

        private static bool InSets(string w, bool womenOnly)
        {
            if (w == null)
                return false;
            foreach (NameSet s in Sets.Values)
                if (Array.IndexOf(s.Women, w) >= 0 || (!womenOnly && (Array.IndexOf(s.Men, w) >= 0 || Array.IndexOf(s.Surnames, w) >= 0)))
                    return true;
            return false;
        }

        private static readonly string[] Personalities = { "Hothead", "Coward", "Proud", "Cold", "Greedy", "Loyal", "Drifter", null, null, null, null };
        private static readonly string[] Dispositions = { "Friendly", "Neutral", "Neutral", "Surly" };

        public static string NameFor(bool woman, string seed)
        {
            return FirstName(woman, seed) + " " + Surname(seed);
        }

        /// A first name or a surname alone, for the people in someone's life (a child, a creditor, a sweetheart).
        public static string FirstName(bool woman, string seed)
        {
            string[] first = woman ? WomenFirst : MenFirst;
            return first[Voice.Hash(seed + "f") % first.Length];
        }

        /// Whether a first name is one of the women's (he or she in a line about them).
        public static bool IsWomansName(string w)
        {
            return w != null && (Array.IndexOf(WomenFirst, w) >= 0 || InSets(w, true));
        }

        /// Whether a word is one of the names residents are given (the language layer's check: a line naming someone it wasn't
        /// given fails). Of the peoples' lists (2026-11-02) only names of five letters or more that aren't plain words count:
        /// "Chin up", "Good morning", "Two." and "Patience" are words first, and the words of a surname of two ("Gray Elk") never
        /// count alone.
        internal static bool IsName(string w)
        {
            return Array.IndexOf(MenFirst, w) >= 0 || Array.IndexOf(WomenFirst, w) >= 0 || Array.IndexOf(Surnames, w) >= 0 ||
                   (w != null && w.Length >= 5 && !PlainWords.Contains(w) && InSets(w, false));
        }

        private static readonly HashSet<string> PlainWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Patience", "Charity", "Easter", "Prince", "Green", "Brown", "Grant", "Banks", "Fields", "Battle", "Booker", "Strand",
            "Sommer", "Roman", "Santa", "Holmes", "Freeman", "Walker", "Barnes", "Moore", "Scott", "Marchese", "Palermo", "Messina",
            "Richard", "Bruno", "Costa", "Engel", "Richter", "Bauer", "Weber", "Keller", "Lucky", "Trinidad", "Rosario", "Soledad",
            "Carmen", "Mercedes", "Bell", "Thomas", "Lewis", "Harris", "Jones", "Williams", "Johnson", "Jackson"
        };

        // surnames of more than one word ("Gray Elk", "Lo Cicero"): a full name's surname is never just its last word for these
        private static readonly List<string> LongSurnames = Sets.Values.SelectMany(s => s.Surnames).Where(s => s.IndexOf(' ') > 0).Distinct().ToList();

        /// A full name's surname: its last word, or a whole surname of two words ("Joseph Gray Elk" -> "Gray Elk"). Every
        /// place that cut a surname off with the last space goes through here since 2026-11-02.
        public static string SurnameOf(string full)
        {
            if (string.IsNullOrEmpty(full))
                return full;
            string f = full.Trim();
            foreach (string s in LongSurnames)
                if (f.Length > s.Length && f.EndsWith(" " + s, StringComparison.Ordinal))
                    return s;
            int sp = f.LastIndexOf(' ');
            return sp >= 0 ? f.Substring(sp + 1) : f;
        }

        public static string Surname(string seed)
        {
            return Surnames[Voice.Hash(seed + "s") % Surnames.Length];
        }

        /// A new resident for an ordinary person. The same seed always gives the same person.
        public static Resident Make(string id, string seed, uint model, bool woman, string town, string trade, long now, int year)
        {
            string origin = Origins.Of(model, town, seed);
            return new Resident
            {
                Id = id,
                Name = NameFor(woman, seed, origin),
                Origin = origin,
                Woman = woman,
                Model = model,
                Town = town,
                Trade = trade,
                Personality = Personalities[Voice.Hash(seed + "p") % Personalities.Length],
                Disposition = Dispositions[Voice.Hash(seed + "d") % Dispositions.Length],
                Sociability = Voice.Hash(seed + "o") % 2 == 0 ? "Outgoing" : "Introvert",
                Devotion = Voice.Pick(seed + "v", "Devout", "Churchgoer", "Churchgoer", "Lapsed", "Lapsed", "Godless"),
                Smarts = Voice.Pick(seed + "m", "Sharp", "Average", "Average", "Average", "Dim"),
                BornYear = year - 18 - Voice.Hash(seed + "b") % 45,
                MetAt = now,
                TouchedAt = now
            };
        }

        /// The plain Person the voice and awareness code works on. Every collection is filled in, nothing is
        /// known about the player, and it carries no enRAGE data.
        public static Person ToPerson(Resident r)
        {
            return new Person
            {
                Id = r.Id,
                Name = r.Name,
                Woman = r.Woman,
                Home = r.Town,
                Trade = r.Trade,
                Personality = r.Personality,
                Disposition = r.Disposition,
                Sociability = r.Sociability,
                Devotion = r.Devotion,
                Smarts = r.Smarts,
                BornYear = r.BornYear,
                ModelHash = r.Model,
                IsAlive = !r.Dead
            };
        }
    }
}
