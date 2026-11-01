// Two full street scenes at once ([Scenes] MainScenesAtOnce; NIGHT4 item 3): the rule that keeps them apart, pure, so the
// offline street sim (mindtest MainScenesTest) and the Director (Game/Director.cs Start) agree on it.
using System;

namespace EnrageWestworld.Core
{
    public static class SceneSlots
    {
        /// Every one of a full scene's people keeps this far from every one of the other's.
        public const float Apart = 20f;
        /// At most this many full scenes play at once, whatever the setting says (1-3 in the ini).
        public const int MostMains = 2;

        /// Whether a cast at these points (x, y pairs) is far enough from the other scene's people (null or empty: no other).
        public static bool FarEnough(float[] castXY, float[] otherXY, float metres = Apart)
        {
            if (castXY == null || otherXY == null)
                return true;
            for (int i = 0; i + 1 < castXY.Length; i += 2)
                for (int j = 0; j + 1 < otherXY.Length; j += 2)
                {
                    float dx = castXY[i] - otherXY[j], dy = castXY[i + 1] - otherXY[j + 1];
                    if (dx * dx + dy * dy < metres * metres)
                        return false;
                }
            return true;
        }

        /// How many full scenes may play now for this setting.
        public static int Mains(int setting)
        {
            return Math.Max(1, Math.Min(MostMains, setting));
        }
    }
}
