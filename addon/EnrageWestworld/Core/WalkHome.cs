// Walk someone home, from J (THREEHOUR item 4; Game/WestworldScript.WalkHome.cs). The offer, the answers and the walk are Talk's own
// (Core/Fondness.cs EscortOffered, Escort; Game/WestworldScript.Escort.cs): this holds only what the event key adds.
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class WalkHome
    {
        public const string Kind = "walkhome";
        public const int PromptMs = 9000, LookMs = 1000, AgainMs = 30000;
        public const float NearMetres = 4f;   // the one beside you, not across the street

        /// One offer a night each: the night counted from noon, so 1 am belongs to the evening before.
        public static string NightKey(long gameMinute)
        {
            return ((gameMinute - 720) / 1440).ToString();
        }

        public static List<InteractOption> Rows(string name)
        {
            return new List<InteractOption>
            {
                new InteractOption { Id = "walkhome:offer", Label = "Offer to see " + (Person.FirstOf(name) ?? "them") + " home." },
                new InteractOption { Id = "walkhome:no", Label = "Leave them be." }
            };
        }
    }
}
