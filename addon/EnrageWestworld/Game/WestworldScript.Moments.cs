// The moment a person is in, read off the game as their turn on the ticker comes (Core/Moments.cs has the thoughts; Lex,
// 2026-11-03: a man fighting Arthur thought "Can't complain"). MomentOf: what they're doing (in combat with you or with
// someone else, fists or guns; fleeing; knocked down; hurt; a gun of yours pointed at them; the saddle, a wagon, the water,
// a drink, a meal, a smoke, their work, a seat or a lean, walking, running, standing about), a gun in their hand, drunk,
// night and rain and the place, and you beside them (near, a gun out, aiming, mounted, wanted, how you look); and the
// street around you, counted by RefreshNear's sweep once a second (people in combat within 20 m, the dead within 12 m,
// gunfire within 40 m in the last 8 s). ThoughtNow puts the loud moments first for everyone, awake or not, in a written
// line whose seed turns over every 8 s, and hands the quiet side on in PlayerLook.Now for the mind to weigh. The ticker
// gives someone in a loud moment the first turn and a new line every 6 s (TickerBrain, NearThought.Urgent). F7 "TEST
// moments:"; log "MOMENT" (detail).
using System;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private int _momentFights, _momentBodies, _momentShotAt = -100000, _momentSweepFights, _momentSweepBodies, _momentUrgent;
        private string _momentLast;

        private void MomentSweepStart()
        {
            _momentSweepFights = 0;
            _momentSweepBodies = 0;
        }

        /// One person in RefreshNear's sweep (the living and the dead, ours or the game's): counted for everyone's moment.
        private void MomentSweep(int h, float d, bool dead, int nowMs)
        {
            if (dead)
            {
                if (d < 12f)
                    _momentSweepBodies++;
                return;
            }
            if (d < 20f && PED.IS_PED_IN_COMBAT(h, 0))
                _momentSweepFights++;
            if (d < 40f && PED.IS_PED_SHOOTING(h))
                _momentShotAt = nowMs;
        }

        private void MomentSweepEnd(Ped me, int nowMs)
        {
            if (PED.IS_PED_SHOOTING(me.Handle))
                _momentShotAt = nowMs;
            _momentFights = _momentSweepFights;
            _momentBodies = _momentSweepBodies;
        }

        /// The moment this person is in, read now.
        private Moment MomentOf(Ped ped, Ped me, float d)
        {
            int h = ped.Handle, you = me.Handle, nowMs = Game.GameTime;
            var m = new Moment();
            m.YouNear = d < 10f;
            m.YouArmed = WEAPON.IS_PED_ARMED(you, 4);
            m.YouAiming = m.YouArmed && PLAYER.IS_PLAYER_FREE_AIMING_AT_ENTITY(Game.Player.Handle, h);   // the unarmed lock-on (a greeting, Talk, a flirt) is the same input: no gun, no gun on them (the advisor)
            m.YouMounted = PED.IS_PED_ON_MOUNT(you);
            m.YouWanted = PLAYER.GET_PLAYER_WANTED_LEVEL(Game.Player.Handle) > 0;
            m.YouLook = m.YouNear ? _sightState : null;
            m.Armed = WEAPON.IS_PED_ARMED(h, 4);
            int health = ENTITY.GET_ENTITY_HEALTH(h), most = ENTITY.GET_ENTITY_MAX_HEALTH(h, false);
            m.Hurt = most > 0 && health > 0 && health < most * 0.55f;
            bool inCombat = PED.IS_PED_IN_COMBAT(h, 0);
            if (PED.IS_PED_IN_COMBAT(h, you))
                m.Doing = "fighting-you";
            else if (inCombat)
                m.Doing = "fighting";
            else if (PED.IS_PED_RAGDOLL(h))
                m.Doing = "down";
            else if (PED.IS_PED_FLEEING(h))
                m.Doing = "fleeing";
            else if (m.YouAiming)
                m.Doing = "aimed-at";
            else if (m.Hurt)
                m.Doing = "hurt";
            else if (PED.IS_PED_ON_MOUNT(h))
                m.Doing = "riding";
            else if (PED.IS_PED_IN_ANY_VEHICLE(h, false))
                m.Doing = "driving";
            else if (ENTITY.IS_ENTITY_IN_WATER(h))
                m.Doing = "swimming";
            else if (PED.IS_PED_USING_ANY_SCENARIO(h))
                m.Doing = MomentScenario(h);
            else
            {
                float speed = ENTITY.GET_ENTITY_SPEED(h);   // a walk is about 1.5 m/s, a run 3 and up
                m.Doing = speed > 3f ? "running" : speed > 0.5f ? "walking" : "standing";
            }
            // fists: nobody with a gun out in it (a man with a gun of his own is shot at, or shooting)
            m.Melee = m.Doing == "fighting-you" ? !m.Armed && !m.YouArmed : !m.Armed;
            m.Drunk = InHisCups(ped, null);
            int hour = CLOCK.GET_CLOCK_HOURS();
            m.Night = hour < 5 || hour >= 21;
            string w = _engine != null ? _engine.NowWeather : null;
            m.Rain = w == "rain" || w == "storm" || w == "snow";
            m.Indoors = _engine != null && _engine.NowIndoors;
            m.Place = _engine != null ? _engine.NowPlace : null;
            m.Woman = !PED.IS_PED_MALE(h);
            m.FightNear = Math.Max(0, _momentFights - (inCombat ? 1 : 0));
            m.BodyNear = _momentBodies > 0;
            m.ShotsNear = nowMs >= _momentShotAt && nowMs - _momentShotAt < 8000;
            if (_cfg.MoreMoments)
            {
                m.More = true;
                // FIVEDAY item 32e: the storm, the horse bolting, a scene of ours they're at (the theft is read in ThoughtNow, by id)
                m.Storm = w == "storm";
                if (m.Doing == "riding")
                {
                    int mount = PED.GET_MOUNT(h);
                    m.Spooked = mount != 0 && ENTITY.DOES_ENTITY_EXIST(mount) && PED.IS_PED_FLEEING(mount);
                }
                if (_ring.Contains(h))
                    m.AtScene = "ring";
                else if (_exec != null && _exec.Captive != null && _exec.Captive.Exists() && h != _exec.Captive.Handle && !_exec.Men.Exists(x => x != null && x.Handle == h) &&
                         ped.Position.DistanceTo(_exec.Captive.Position) < 40f)
                    m.AtScene = "execution";
                else if (_cfg.RoadThoughts && _eventRun != null && _eventRun.E.Kind == "gallows" && ped.Position.DistanceTo(_eventRun.At) < 40f)
                    m.AtScene = "gallows";   // the crowd at a hanging (FIVEDAY2 item 26)
            }
            return m;
        }

        /// Whether this person is in a loud moment, for the ticker's turn (RefreshNear, once a second); a native throwing on an
        /// odd entity is false, not the ticker down.
        private bool MomentLoud(Ped ped, Ped me, float d)
        {
            try
            {
                return Moments.Urgent(MomentOf(ped, me, d));
            }
            catch (Exception ex)
            {
                Log.Detail("MOMENT " + ped.Handle + " couldn't be read for the turn: " + ex.Message);
                return false;
            }
        }

        /// What a scenario is to the one in it: a drink, a meal, their work, a smoke, a rest (sat or leaning), or standing about.
        private static string MomentScenario(int h)
        {
            string name = null;
            try
            {
                name = MindScenarios.NameOf((uint)TASK._GET_SCENARIO_POINT_TYPE_PED_IS_USING(h));
            }
            catch { }
            if (name != null)
            {
                if (Array.IndexOf(MindScenarios.DrinkPoints, name) >= 0 || Array.IndexOf(MindScenarios.OtherDrinks, name) >= 0 || name == "WORLD_HUMAN_COFFEE_DRINK")
                    return "drinking";
                if (Array.IndexOf(MindScenarios.EatPoints, name) >= 0)
                    return "eating";
                if (Array.IndexOf(MindScenarios.WorkPoints, name) >= 0 || Array.IndexOf(MindScenarios.OtherWork, name) >= 0)
                    return "working";
                if (name.IndexOf("SMOKE", StringComparison.Ordinal) >= 0)
                    return "smoking";
                string kind = MindScenarios.Kind(name);
                if (kind == MindScenarios.Seat || kind == MindScenarios.Lean || kind == MindScenarios.Table)
                    return "resting";
                return "standing";
            }
            return PED.IS_PED_SITTING(h) ? "resting" : "standing";
        }

        /// The moment first, awake or asleep: someone fighting, fleeing, down, hurt, aimed at, or with shots, a body or a
        /// fight close by thinks of that before anything, in a written line that turns over every few seconds (even with the
        /// AI model on: its answer would come after the fight); otherwise the thought as before, the quiet side of the
        /// moment handed on in look.Now for the mind to weigh (Engine.Minds.Candidates "moment:", InnerLife.Thought).
        private string ThoughtNow(Ped ped, Ped me, float d, Person p, PersonState st, string key, InnerLife.PlayerLook look, Func<InnerLife.PlayerLook, string> otherwise)
        {
            Moment m = null;
            try
            {
                m = MomentOf(ped, me, d);
            }
            catch (Exception ex)
            {
                Log.Detail("MOMENT " + key + " couldn't be read: " + ex.Message);
            }
            if (m != null && _cfg.MoreMoments && p != null && p.Id != null)
                m.Robbed = RobbedLately(p.Id);   // (FIVEDAY item 32e)
            if (m != null && Moments.Urgent(m))
            {
                string src;
                string t = Moments.Thought(p, m, key + "|m" + Game.GameTime / Moments.UrgentSeedMs, out src, st != null ? st.WakeStage : 0);
                if (t != null)
                {
                    _momentUrgent++;
                    ThreadLoud(key, src, Game.GameTime);   // it's what's on their mind for a while (WestworldScript.Threads.cs)
                    _momentLast = (p != null ? p.Name : key) + " " + src;
                    if (Log.Debug)
                        Log.Detail("MOMENT " + (p != null ? p.Name : key) + " (" + src + "): \"" + t + "\"");
                    return t;
                }
            }
            if (m == null || !Moments.Urgent(m))
            {
                string follows = ThreadFollow(key, p, Game.GameTime);
                if (follows != null)
                    return follows;
            }
            if (look == null)
                look = Look(me, d);
            look.Now = m;
            return otherwise(look);
        }

        /// A theft on them in the last ninety game minutes (a pickpocket, a mugging, a lift: they know the purse is gone,
        /// whether or not they saw who), by the store's ties (FIVEDAY item 32e).
        private bool RobbedLately(string id)
        {
            long g = Bridge.Now();
            foreach (SceneTie t in _store.Ties)
                if (t.Crime != null && t.CrimeDoer != null && t.CrimeDoer != id && t.Has(id) && g - t.CrimeAt >= 0 && g - t.CrimeAt < 90)
                    return true;
            return false;
        }

        private string MomentsStatus()
        {
            int nowMs = Game.GameTime;
            return _momentUrgent + " loud-moment thoughts (a fight, a flight, a gun on them, shots or a body near)" + (_momentLast != null ? ", last " + _momentLast : "") +
                   "; around you now: " + _momentFights + " in combat within 20 m, " + _momentBodies + " dead within 12 m, " +
                   (nowMs - _momentShotAt < 8000 && nowMs >= _momentShotAt ? "shots " + (nowMs - _momentShotAt) / 1000 + " s ago" : "no shots lately");
        }
    }
}
