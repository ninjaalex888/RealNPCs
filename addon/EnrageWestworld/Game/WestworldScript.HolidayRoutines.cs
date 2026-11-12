// The Testing row "holidays" (LIVES item 1f; [World] HolidayRoutines): what holiday the clock says it is, and where the
// nearest regular's routine has them now with the holidays and without (the log and the status line; nothing moves).
using EnrageWestworld.Core;
using RDR2;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private string HolidayRoutinesTest(out string said)
        {
            said = null;
            if (!_cfg.HolidayRoutines || !_cfg.RegularsRoutines)
                return "[World] HolidayRoutines and RegularsRoutines must be on";
            Ped me = Game.Player.Character;
            if (me == null || !me.Exists() || _store == null)
                return "not loaded";
            string hol = Routines.HolidayNow();
            int hour = (int)(_store.LastGameMin % 1440 / 60), wd = InnerLife.Weekday < 0 ? 1 : InnerLife.Weekday;
            foreach (int h in _mindNear)
            {
                if (!ENTITY.DOES_ENTITY_EXIST(h) || ENTITY.IS_ENTITY_DEAD(h))
                    continue;
                Ped p = new Ped(h);
                if (p.Position.DistanceTo(me.Position) >= 25f)
                    continue;
                Person who = KnownPerson(p);
                Resident r = who != null && who.Id != null ? _store.FindResident(who.Id) : null;
                if (r == null || !r.Regular)
                    continue;
                Routines.Profile prof = Routines.ProfileOf(r, Voice.NowYear);
                string with = Routines.Expected(prof, hour, wd);
                Routines.HolidaysOn = false;
                string without = Routines.Expected(prof, hour, wd);
                Routines.HolidaysOn = true;
                said = (hol ?? "no holiday today") + "; " + r.Name + " at " + hour + ":00: " + with + (with != without ? " (an ordinary day: " + without + ")" : " (the same as any day)");
                Log.Write("HOLIDAY routines (test): " + said);
                return null;
            }
            said = (hol ?? "no holiday today") + " (no regular within 25 m to show)";
            Log.Write("HOLIDAY routines (test): " + said);
            return null;
        }
    }
}
