// The model composes (FIVEDAY2 item 9): see WestworldScript.Composed.cs. Without it, or with it late or unreadable, the rules' order is played.
using System;
using System.Collections.Generic;
using EnrageWestworld.Core;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private readonly List<int> _composedAsks = new List<int>();

        private bool ComposedAsk(int h, MindWho w, Held hd, List<Expression> menu, int target, int now, string feeling)
        {
            return false;
        }

        private void ComposedAskStep(int now)
        {
        }

        private string ComposedAskStatus()
        {
            return "";
        }
    }
}
