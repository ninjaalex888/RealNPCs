// CasualHookups (another mod: a native .asi by its own author, in the game folder) with RealNPCs deciding who is
// willing (2026-10-26, Lex's choice). RealNPCs can't start its scene: it has its own Flirt prompt in the lock-on menu (its
// PromptKey, G as it comes) and its own dice. So with [Romance] CasualHookupsDecides on and CasualHookups.asi present:
//   - a woman's yes to the player's flirting here (FlirtResult.Tryst) leaves her willing for a while (the tryst offer,
//     with no fade of ours: TrystAsked only says the words);
//   - its key is held (DISABLE_CONTROL_ACTION, every frame) while the lock-on target is a woman who isn't willing, but
//     never G (INPUT_INTERACT_OPTION1), which enRAGE's own lock-on prompts use (Interrogate, Give Her a Few Dollars);
//   - once (Lex's choices): its key moved off G to B, her clothes back after (femaleStaysNaked false: enRAGE knows its
//     people by their look), and its chances set to always give in (0/100/0) only once its key is off G, after a copy
//     of its ini goes to RealNPCs\backup (that copy is also the mark that it was done: the panel's changes stand).
//     B is the lock-on menu's horse-brush key; on foot B also opens the satchel, which the first test will show.
// Not known yet: whether the hold stops its prompt, and whether it reads its ini while running or only at the start.
// The log says what happened (HOOKUPS ...): the key pressed, held, and whether a scene seemed to follow (the player's
// controls off or his undressing, for 3 s; judged from his side, since its strings speak of hiding the woman and replacing
// a stale one), with her handle's state, so a night still counts here. The two of them within 0.9 m counts only for a
// woman who never said yes (did the hold hold): a willing woman stands that close at the ask anyway (review, 2026-10-31).
// Hook up (H) on a willing woman (Lex, 2026-10-31, after Opal Ackerman's yes led nowhere: CasualHookups shows no prompt
// at all for a kind it won't take, so B was never there to press): a kind it is known to take (Store.Counts
// "hookups takes:<model>", learned when its scene follows a B press on a willing woman) is told to press B; a kind it
// is known not to take gets RealNPCs' own night at once (the fade); an unknown kind is told to press B, and if no scene
// of its starts within HookupAskWaitMs (15 s) the night is RealNPCs' own, with a short notice, so the player is never
// left waiting on a prompt that isn't there. That kind is learned as "won't" when B was pressed on her and nothing
// followed, or after two asks with B never pressed (a missed press alone proves nothing), and never while CasualHookups
// may be rolling its own chances (_hookupsStartOdds: what it loaded at the game's start isn't 0/100/0).
// Not deciding (Romance, Trysts or CasualHookupsDecides off): its own chances are put back from the copy in
// RealNPCs\backup (only while they are still RealNPCs' 0/100/0), and 0/100/0 again when RealNPCs decides once more (only
// while they are still that copy's), so the panel's own numbers stand. Its key stays on B (G is enRAGE's). It may read
// them only at the start, so while it started this game on 0/100/0 its key is held on every woman (never G) till then.
using System;
using System.Collections.Generic;
using System.IO;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private const string HookupsGKey = "INPUT_INTERACT_OPTION1", HookupsBKey = "INPUT_INTERACT_HORSE_BRUSH";
        private const uint HookupsGHash = 0x760A9C6F;   // INPUT_INTERACT_OPTION1, G: enRAGE's lock-on prompts, never held
        private bool _hookupsToldNoHold, _hookupsToldHoldFailed;
        private const int HookupWillingMs = 300000;   // how long a yes leaves her willing for CasualHookups' Flirt

        private sealed class HookupRun
        {
            public int Handle, PressedAt, CloseSince, StartedAt, ApartSince, NextLook;
            public string Name, Kind, After;
            public Person Who;
            public TrystOffer Offer;   // her yes, for RealNPCs' own night if CasualHookups starts nothing
            public bool Willing, Stirred;   // Stirred: any sign of its scene at all (controls off, undressing, the two close)
            public bool FromAsk;   // made at the Hook up ask (a kind not known yet), before any press of its key
            public bool Pressed;   // its key pressed on her (before that, her standing close at the ask is no sign)
            public bool SawScene;  // the player's side of a scene seen (controls off, undressing): enough to learn "takes"
            public int AskedAt;
        }

        private string _hookupsStartOdds = "?";   // the chances CasualHookups loaded when the game started, as far as known
        private bool _hookupsToldUnlinked;

        private const int HookupFallbackMs = 10000;   // no sign of its scene this long after its key: RealNPCs' own night
        private const int HookupAskWaitMs = 15000;    // a kind not known yet: this long after the ask for its scene to begin

        private string _hookupsIni;          // CasualHookups.ini, when CasualHookups.asi is in the game folder
        private string _hookupsKey;          // its PromptKey
        private string _hookupsStartKey;     // the PromptKey it started the game with
        private uint _hookupsKeyHash;
        private uint _hookupsStartHash;      // its key when the game started: it may go on using that one until the next start
        private string _hookupsOdds = "?";   // its three chances, as read
        private DateTime _hookupsStamp;
        private int _hookupsNextRead, _hookupsHeld, _hookupsHints, _hookupsScenes;
        private HookupRun _hookRun;
        private string _hookupsLast;
        private bool _hookupsLooked;
        private bool? _hookupsWasLinked;   // whether RealNPCs decided at the last look (its chances follow a change)
        private Action _aHookups;

        /// CasualHookups is installed and RealNPCs decides who it works on.
        private bool HookupsLinked
        {
            get { return _hookupsIni != null && _cfg.Romance && _cfg.Trysts && _cfg.CasualHookupsDecides; }
        }

        /// The game folder (the scripts folder's parent), where .asi mods sit.
        private string GameDir()
        {
            string scripts = Path.GetDirectoryName(base.FileName);
            return scripts != null ? Path.GetDirectoryName(scripts) : null;
        }

        /// Once at the start: is it there, and (if RealNPCs decides) its chances and key set once.
        private void HookupsFind()
        {
            _hookupsLooked = true;
            string dir = GameDir();
            if (dir == null || !File.Exists(Path.Combine(dir, "CasualHookups.asi")))
            {
                Log.Write("HOOKUPS: CasualHookups isn't installed; a woman's yes leads to RealNPCs' own night (a fade to black)");
                return;
            }
            string ini = Path.Combine(dir, "CasualHookups.ini");
            if (!File.Exists(ini))
            {
                Log.Write("HOOKUPS: CasualHookups.asi is installed but its ini isn't there yet (it writes one when the game starts); RealNPCs leaves it alone this time");
                return;
            }
            _hookupsIni = ini;
            HookupsRead();
            _hookupsStartHash = _hookupsKeyHash;
            _hookupsStartKey = _hookupsKey;
            HookupsStartOdds();
            try
            {
                // RealNPCs moved its key after this game started (and was reloaded since, with Insert): it may still be on
                // the one in the copy made before (File.Copy keeps the old write time, so the copy's creation time)
                string mark = Path.Combine(_dir, "backup", "CasualHookups.ini.before-realnpcs");
                if (File.Exists(mark) && File.GetCreationTimeUtc(mark) > System.Diagnostics.Process.GetCurrentProcess().StartTime.ToUniversalTime())
                {
                    IniEntry was = IniDoc.Read(mark, false).FindKey("PromptKey");
                    if (was != null && was.Value.Trim().Length > 0)
                    {
                        _hookupsStartKey = was.Value.Trim();
                        _hookupsStartHash = Natives.Hash(_hookupsStartKey);
                    }
                }
            }
            catch
            {
            }
            if (_cfg.CasualHookupsDecides)
            {
                HookupsSetUpOnce();
                _hookupsStamp = default(DateTime);
                HookupsRead();
            }
            Log.Write("HOOKUPS: CasualHookups found (its Flirt on " + HookupsKeyText + ", chances " + _hookupsOdds + " turned down / yes at once / a second try). " +
                      (HookupsLinked ? "It works only on a woman who has said yes to your flirting here; RealNPCs doesn't fade to black itself." : "RealNPCs leaves it alone ([Romance] CasualHookupsDecides off, or Trysts or Romance off), with its own chances."));
        }

        /// The chances CasualHookups loaded when this game started (it may read its ini only then): the file's as RealNPCs
        /// first saw it in this game, kept in RealNPCs\backup\CasualHookups.chances-at-start, so an Insert reload after
        /// RealNPCs changed them still knows.
        private void HookupsStartOdds()
        {
            _hookupsStartOdds = _hookupsOdds;
            try
            {
                string f = Path.Combine(_dir, "backup", "CasualHookups.chances-at-start");
                if (File.Exists(f) && File.GetLastWriteTimeUtc(f) > System.Diagnostics.Process.GetCurrentProcess().StartTime.ToUniversalTime())
                    _hookupsStartOdds = File.ReadAllText(f).Trim();
                else
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(f));
                    File.WriteAllText(f, _hookupsOdds);
                }
            }
            catch
            {
            }
            Log.Write("HOOKUPS: CasualHookups started this game with chances " + _hookupsStartOdds + (_hookupsStartOdds == "0/100/0" ? "" : " (if it reads them only at the start, it rolls its own dice till the next start, so till then RealNPCs learns no kind it won't take)"));
        }

        /// CasualHookups may be rolling its own dice this game: a willing woman's B can fail on its chances, not her kind.
        private bool HookupsOwnDice
        {
            get { return _hookupsStartOdds != "0/100/0"; }
        }

        /// Its own chances, as in the copy in RealNPCs\backup (null if there's none or it lacks one).
        private string HookupsBackupOdds()
        {
            if (_hookupsBackupOdds != null)
                return _hookupsBackupOdds.Length > 0 ? _hookupsBackupOdds : null;
            _hookupsBackupOdds = "";
            try
            {
                string mark = Path.Combine(_dir, "backup", "CasualHookups.ini.before-realnpcs");
                if (!File.Exists(mark))
                    return null;   // looked at again at the next change of HookupsLinked (SetUpOnce may make it)
                IniDoc was = IniDoc.Read(mark, false);
                if (was.Error != null)
                    return null;
                string[] keys = { "notInterested", "givesInImmediately", "needsSecondFlirt" };
                string[] orig = new string[3];
                for (int i = 0; i < 3; i++)
                {
                    IniEntry e = was.FindKey(keys[i]);
                    orig[i] = e != null ? e.Value.Trim() : "";
                    if (orig[i].Length == 0)
                        return null;
                }
                _hookupsBackupOdds = string.Join("/", orig);
            }
            catch
            {
            }
            return _hookupsBackupOdds.Length > 0 ? _hookupsBackupOdds : null;
        }

        private string _hookupsBackupOdds;

        /// Its chances to always give in, and its key off enRAGE's G, once (the backup copy is the mark).
        private void HookupsSetUpOnce()
        {
            string mark = Path.Combine(_dir, "backup", "CasualHookups.ini.before-realnpcs");
            if (File.Exists(mark))
                return;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(mark));
                File.Copy(_hookupsIni, mark, false);
            }
            catch (Exception ex)
            {
                Log.Write("HOOKUPS: couldn't copy CasualHookups.ini to RealNPCs\\backup, so nothing in it was changed: " + ex.Message);
                return;
            }
            IniDoc doc = IniDoc.Read(_hookupsIni, false);
            if (doc.Error != null)
            {
                Log.Write("HOOKUPS: couldn't read CasualHookups.ini: " + doc.Error);
                return;
            }
            // her clothes back after (Lex: enRAGE knows its people by their look), and its key off G (Lex: to B)
            string done = HookupsSet(doc, "femaleStaysNaked", "false");
            IniEntry key = doc.FindKey("PromptKey");
            if (key != null && key.Value.Trim().Equals(HookupsGKey, StringComparison.OrdinalIgnoreCase))
                done += HookupsSet(doc, "PromptKey", HookupsBKey);
            // always giving in only where its key can be held: on G, every woman would go along, whatever she thinks
            key = doc.FindKey("PromptKey");
            if (key != null && !key.Value.Trim().Equals(HookupsGKey, StringComparison.OrdinalIgnoreCase))
                done += HookupsSet(doc, "notInterested", "0") + HookupsSet(doc, "givesInImmediately", "100") + HookupsSet(doc, "needsSecondFlirt", "0");
            if (done.Length == 0)
            {
                Log.Write("HOOKUPS: CasualHookups.ini already set as RealNPCs needs it; nothing changed (a copy is in RealNPCs\\backup)");
                return;
            }
            string err = doc.Save();
            Log.Write(err != null ? "HOOKUPS: couldn't save CasualHookups.ini (" + err + "); nothing changed. The panel's Romance page can still try."
                                  : "HOOKUPS: set CasualHookups.ini so RealNPCs decides who's willing:" + done + ". Its old file is in RealNPCs\\backup. If it read its settings when the game started, these count from the next start.");
        }

        private static string HookupsSet(IniDoc doc, string key, string value)
        {
            IniEntry e = doc.FindKey(key);
            if (e == null || e.Value.Trim() == value || !doc.Set(e, value))
                return "";
            return " " + key + " " + value;
        }

        /// Its key and chances, again whenever the file changes (looked at every 5 s).
        private void HookupsRead()
        {
            if (_hookupsIni == null)
                return;
            try
            {
                DateTime stamp = File.GetLastWriteTimeUtc(_hookupsIni);
                if (stamp == _hookupsStamp && _hookupsKey != null)
                    return;
                _hookupsStamp = stamp;
                IniDoc doc = IniDoc.Read(_hookupsIni, false);
                if (doc.Error != null)
                    return;
                IniEntry k = doc.FindKey("PromptKey"), n = doc.FindKey("notInterested"), y = doc.FindKey("givesInImmediately"), s = doc.FindKey("needsSecondFlirt");
                string key = k != null && k.Value.Trim().Length > 0 ? k.Value.Trim() : HookupsGKey;
                if (key != _hookupsKey && _hookupsKey != null)
                    Log.Write("HOOKUPS: its Flirt key is now " + HookupsKeyName(key));
                _hookupsKey = key;
                _hookupsKeyHash = Natives.Hash(key);
                _hookupsOdds = (n != null ? n.Value.Trim() : "?") + "/" + (y != null ? y.Value.Trim() : "?") + "/" + (s != null ? s.Value.Trim() : "?");
            }
            catch
            {
            }
        }

        /// Its key for the player: the one in its ini, and the one it started with if that differs (it may read its ini
        /// only when the game starts).
        private string HookupsKeyText
        {
            get { return _hookupsStartKey != null && !_hookupsStartKey.Equals(_hookupsKey, StringComparison.OrdinalIgnoreCase) ? HookupsKeyName(_hookupsKey) + ", or " + HookupsKeyName(_hookupsStartKey) + " until the game restarts" : HookupsKeyName(_hookupsKey); }
        }

        /// "B", "G", or the name as written.
        private static string HookupsKeyName(string key)
        {
            if (key == null)
                return "?";
            if (key.Equals(HookupsGKey, StringComparison.OrdinalIgnoreCase))
                return "G";
            if (key.Equals(HookupsBKey, StringComparison.OrdinalIgnoreCase))
                return "B";
            return key;
        }

        private void HookupsStep(int now)
        {
            if (_aHookups == null)
                _aHookups = () => HookupsTick(_now);
            if (!_hookupsLooked || _hookupsIni != null || _hookRun != null)   // installed: a change of HookupsLinked is seen too
                Safe("casual hookups", _aHookups);
        }

        private unsafe void HookupsTick(int now)
        {
            if (!_hookupsLooked)
                HookupsFind();
            if (now >= _hookupsNextRead)
            {
                _hookupsNextRead = now + 5000;
                HookupsRead();
            }
            if (_hookupsIni != null && _hookupsWasLinked != HookupsLinked)
            {
                if (_hookupsWasLinked.HasValue)
                    Log.Write("HOOKUPS: RealNPCs " + (HookupsLinked ? "decides who CasualHookups works on again" : "no longer decides who CasualHookups works on ([Romance] Romance, Trysts or CasualHookupsDecides off)"));
                _hookupsWasLinked = HookupsLinked;
                HookupsChances(HookupsLinked);
            }
            HookupsWatch(now);
            bool linked = HookupsLinked;
            // not deciding, but it started this game always giving in (0/100/0) and its own chances aren't that: put back
            // in the file, they may count only from the next start, so till then its key is held on every woman (never G)
            string own = linked || _hookupsIni == null ? null : HookupsBackupOdds();
            bool holdAll = own != null && own != "0/100/0" && _hookupsStartOdds == "0/100/0";
            if (!linked && _hookupsIni != null && !_hookupsToldUnlinked)
            {
                _hookupsToldUnlinked = true;
                Log.Write(holdAll ? "HOOKUPS: RealNPCs doesn't decide, but CasualHookups started this game always giving in (0/100/0), so its Flirt (" + HookupsKeyText + ") is held on every woman until the game restarts (never G); from then on it has its own chances (" + own + ")"
                                  : "HOOKUPS: RealNPCs doesn't decide, and CasualHookups started this game with chances " + _hookupsStartOdds + ", so it's left alone with its own dice");
            }
            if (linked)
                _hookupsToldUnlinked = false;
            if ((!linked && !holdAll) || _hookupsKeyHash == 0 || !PLAYER.IS_PLAYER_CONTROL_ON(Game.Player.Handle))
                return;
            int target = 0;
            if (!PLAYER.IS_PLAYER_FREE_AIMING(Game.Player.Handle))
                PLAYER.GET_PLAYER_INTERACTION_TARGET_ENTITY(Game.Player.Handle, &target, false, false);
            if (target == 0 || !ENTITY.DOES_ENTITY_EXIST(target) || !ENTITY.IS_ENTITY_A_PED(target) || !PED.IS_PED_HUMAN(target) || PED.IS_PED_MALE(target) || PED.IS_PED_A_PLAYER(target))
                return;
            if (!linked)
            {
                // nobody is willing for it while RealNPCs doesn't decide: held, no watch, no hint
                bool heldAll = HookupsHold(_hookupsKeyHash) | (_hookupsStartHash != _hookupsKeyHash && HookupsHold(_hookupsStartHash));
                if (heldAll && (PAD.IS_DISABLED_CONTROL_JUST_PRESSED(0, _hookupsKeyHash) || (_hookupsStartHash != 0 && PAD.IS_DISABLED_CONTROL_JUST_PRESSED(0, _hookupsStartHash))))
                {
                    _hookupsHeld++;
                    Log.Write("HOOKUPS held (not deciding): CasualHookups' Flirt on " + TalkName(new Ped(target)) + ", until the game restarts with its own chances");
                }
                return;
            }
            TrystOffer o = _trystOffer;
            bool willing = o != null && o.Hookups && o.Night && o.Handle == target && now <= o.Until;
            if (!willing)
            {
                // its Flirt does nothing on her; never G, which enRAGE's own lock-on prompts use (Interrogate, its money)
                bool held = HookupsHold(_hookupsKeyHash) | (_hookupsStartHash != _hookupsKeyHash && HookupsHold(_hookupsStartHash));
                if (!_hookupsToldNoHold && (_hookupsKeyHash == HookupsGHash || _hookupsStartHash == HookupsGHash))
                {
                    _hookupsToldNoHold = true;
                    Log.Write(_hookupsKeyHash == HookupsGHash
                        ? "HOOKUPS: not holding CasualHookups' Flirt: it's on G, which enRAGE's own lock-on prompts use, so it rolls its own dice (the panel's Romance page can move it to B)"
                        : "HOOKUPS: until the game restarts CasualHookups may still be on G, which RealNPCs doesn't hold (enRAGE's own lock-on prompts use it), so till then its Flirt may work on anyone");
                }
                if ((held && PAD.IS_DISABLED_CONTROL_JUST_PRESSED(0, _hookupsKeyHash)) || (held && _hookupsStartHash != 0 && PAD.IS_DISABLED_CONTROL_JUST_PRESSED(0, _hookupsStartHash)))
                {
                    _hookupsHeld++;
                    string name = TalkName(new Ped(target));
                    bool flirted = o != null && o.Handle == target && now <= o.Until;
                    bool ours = flirted && o.Night;   // willing, but CasualHookups won't take her kind
                    Log.Write("HOOKUPS held: " + name + (ours ? " is willing, but CasualHookups turns her kind down; the night is RealNPCs' own" : flirted ? " liked your flirting, but not that much today" : " hasn't said yes to your flirting") + ", so CasualHookups' Flirt (" + HookupsKeyText + ") was held");
                    if (_hookupsHints < 3)
                    {
                        _hookupsHints++;
                        Feed.Notice("RealNPCs", ours ? name + " is willing. CasualHookups won't take her, so Hook up (H) to go somewhere quieter."
                                                     : flirted ? name + " liked your flirting, but she won't go further today."
                                                     : name + " hasn't said yes to you. Greet her (R), then Flirt (H); CasualHookups follows her answer.");
                    }
                    if (_hookRun == null)
                    {
                        _hookRun = new HookupRun { Handle = target, Name = name, PressedAt = now, Willing = false, Pressed = true };   // to see whether the hold held
                        Log.Write("HOOKUPS her: " + HerState(target));
                    }
                }
                return;
            }
            // a press on her starts a new watch, unless a scene is already under way (2026-10-26: a watch on Pearl Babineaux
            // still waiting hid the press on Willa Tate)
            if ((_hookRun == null || _hookRun.StartedAt == 0) && (PAD.IS_CONTROL_JUST_PRESSED(0, _hookupsKeyHash) || (_hookupsStartHash != 0 && PAD.IS_CONTROL_JUST_PRESSED(0, _hookupsStartHash))))
            {
                HookupRun w = _hookRun;
                if (w != null && w.Handle == target && w.FromAsk && !w.Pressed)
                {
                    // the watch the ask began: its key now pressed, so her standing close counts from here (and 10 s more at least)
                    w.Pressed = true;
                    w.PressedAt = now;
                    Log.Write("HOOKUPS key pressed on " + o.Name + " " + (now - w.AskedAt) / 1000 + " s after Hook up (willing, " + o.Kind + ", " + (ModelOf(target) ?? "?") + "): CasualHookups' turn; her: " + HerState(target));
                    return;
                }
                if (w != null && w.Handle != target)
                    Log.Write("HOOKUPS: no longer watching " + w.Name + " (nothing came of it before a press on " + o.Name + ")");
                _hookRun = new HookupRun { Handle = target, Name = o.Name, Kind = o.Kind, Who = o.Who, Offer = o, PressedAt = now, Willing = true, Pressed = true };
                Log.Write("HOOKUPS key pressed on " + o.Name + " (willing, " + o.Kind + ", " + (ModelOf(target) ?? "?") + (HookupsTakes(target) ? ", a kind it has taken before" : "") + "): CasualHookups' turn; her: " + HerState(target));
            }
        }

        // the kinds of women CasualHookups turns down whatever its chances say ("Lady is not interested" with notInterested
        // 0): seen on 2026-10-26 with Saint Denis' working girls (Willa Tate, Pearl Babineaux). More are learned when its key
        // on a willing woman starts nothing (Store.Counts "hookups won't:<model>"). Its own file lists the other towns'
        // saloon women beside this one, but that alone doesn't say it turns them down.
        private static readonly HashSet<string> HookupsWontSeen = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "a_f_m_nbxwhore_01" };

        private static string ModelOf(int ped)
        {
            return ENTITY.DOES_ENTITY_EXIST(ped) ? Models.Name(ENTITY.GET_ENTITY_MODEL(ped)) : null;
        }

        /// CasualHookups won't take her kind: her yes leads to RealNPCs' own night instead.
        private bool HookupsWont(int ped)
        {
            string m = ModelOf(ped);
            return m != null && (HookupsWontSeen.Contains(m) || (_store != null && _store.Counts.ContainsKey("hookups won't:" + m.ToLowerInvariant())));
        }

        private void HookupsLearnWont(int ped, string name)
        {
            string m = ModelOf(ped);
            if (m == null || _store == null || HookupsWont(ped))
                return;
            _store.Counts["hookups won't:" + m.ToLowerInvariant()] = 1;
            Log.Write("HOOKUPS: CasualHookups won't take " + m + " (" + name + "); from now on that kind's yes leads to RealNPCs' own night");
        }

        /// CasualHookups' scene has followed its key on a willing woman of her kind before: Hook up says press B.
        private bool HookupsTakes(int ped)
        {
            string m = ModelOf(ped);
            return m != null && !HookupsWont(ped) && _store != null && _store.Counts.ContainsKey("hookups takes:" + m.ToLowerInvariant());
        }

        private void HookupsLearnTakes(int ped, string name)
        {
            string m = ModelOf(ped);
            if (m == null || _store == null || HookupsWont(ped) || HookupsTakes(ped))
                return;
            _store.Counts["hookups takes:" + m.ToLowerInvariant()] = 1;
            Log.Write("HOOKUPS: CasualHookups takes " + m + " (" + name + "); from now on Hook up on that kind says press " + HookupsKeyName(_hookupsKey) + " for its scene");
        }

        /// Hook up (H) asked of a willing woman whose night is CasualHookups': what to tell the player, and (a kind not known
        /// yet) a watch from now, so if CasualHookups has no prompt for her the night is RealNPCs' own after 15 s.
        private void HookupsAsked(TrystOffer o, int now)
        {
            string model = ModelOf(o.Handle) ?? "?";
            string key = HookupsKeyName(_hookupsKey);
            if (HookupsTakes(o.Handle))
            {
                Log.Write("HOOKUPS ask: " + o.Name + " (" + model + "): CasualHookups has taken her kind before, so press " + HookupsKeyText + " for its scene");
                Feed.Notice("RealNPCs", "Press " + key + " (CasualHookups' Flirt) for its scene with " + o.Name + ".");
                return;
            }
            if (_hookRun != null && _hookRun.StartedAt == 0)
                Log.Write("HOOKUPS: no longer watching " + _hookRun.Name + " (Hook up asked of " + o.Name + ")");
            _hookRun = new HookupRun { Handle = o.Handle, Name = o.Name, Kind = o.Kind, Who = o.Who, Offer = o, AskedAt = now, Willing = true, FromAsk = true };
            Log.Write("HOOKUPS ask: " + o.Name + " (" + model + "): not known yet whether CasualHookups takes her kind; press " + HookupsKeyText + ", and if no scene of its begins within " + HookupAskWaitMs / 1000 + " s the night is RealNPCs' own");
            Feed.Notice("RealNPCs", "Press " + key + " (CasualHookups' Flirt) within " + HookupAskWaitMs / 1000 + " s for its scene with " + o.Name + ". If it shows no prompt for her, wait: RealNPCs' own night follows.");
        }

        /// Its chances follow whether RealNPCs decides: 0/100/0 while it does, its own (the copy in RealNPCs\backup) while it
        /// doesn't. Only between those two, so numbers set on the panel stand. Its key is left where it is (off G, enRAGE's).
        /// Chosen over holding its key on every woman for good: off means "the two mods apart", which is CasualHookups with its
        /// own dice. Only while it may still have RealNPCs' 0/100/0 from the game's start is its key held (HookupsTick).
        private void HookupsChances(bool linked)
        {
            _hookupsBackupOdds = null;
            string mark = Path.Combine(_dir, "backup", "CasualHookups.ini.before-realnpcs");
            if (!File.Exists(mark))
            {
                if (linked)
                {
                    HookupsSetUpOnce();   // RealNPCs decides for the first time since CasualHookups was installed
                    _hookupsStamp = default(DateTime);
                    HookupsRead();
                }
                return;
            }
            try
            {
                IniDoc was = IniDoc.Read(mark, false);
                if (was.Error != null)
                    return;
                string[] keys = { "notInterested", "givesInImmediately", "needsSecondFlirt" };
                string[] orig = new string[3];
                for (int i = 0; i < 3; i++)
                {
                    IniEntry e = was.FindKey(keys[i]);
                    orig[i] = e != null ? e.Value.Trim() : "";
                    if (orig[i].Length == 0)
                        return;
                }
                string origOdds = string.Join("/", orig);
                if (origOdds == "0/100/0")
                    return;   // its own were always giving in already: nothing to put back
                _hookupsStamp = default(DateTime);
                HookupsRead();
                IniDoc doc = IniDoc.Read(_hookupsIni, false);
                if (doc.Error != null)
                    return;
                string done;
                if (_hookupsOdds == (linked ? "0/100/0" : origOdds))
                    return;   // already as they should be
                if (!linked && _hookupsOdds == "0/100/0")
                    done = HookupsSet(doc, keys[0], orig[0]) + HookupsSet(doc, keys[1], orig[1]) + HookupsSet(doc, keys[2], orig[2]);
                else if (linked && _hookupsOdds == origOdds && !HookupsGKey.Equals(_hookupsKey, StringComparison.OrdinalIgnoreCase))
                    done = HookupsSet(doc, keys[0], "0") + HookupsSet(doc, keys[1], "100") + HookupsSet(doc, keys[2], "0");
                else
                {
                    Log.Write("HOOKUPS: CasualHookups' chances left as they are (" + _hookupsOdds + "; RealNPCs " + (linked ? "decides" : "doesn't decide") + "; its own were " + origOdds + ")");
                    return;
                }
                if (done.Length == 0)
                    return;
                string err = doc.Save();
                _hookupsStamp = default(DateTime);
                HookupsRead();
                Log.Write(err != null ? "HOOKUPS: couldn't save CasualHookups.ini (" + err + "); its chances stay " + _hookupsOdds
                                      : linked ? "HOOKUPS: RealNPCs decides again, so CasualHookups always gives in to a woman who said yes here:" + done + ". It may read this only at the next start."
                                               : "HOOKUPS: RealNPCs doesn't decide now, so CasualHookups has its own chances back (from RealNPCs\\backup):" + done + ". It may read them only at the next start; till then it may still give in to every woman.");
            }
            catch (Exception ex)
            {
                Log.Write("HOOKUPS: its chances couldn't be set: " + ex.Message);
            }
        }

        /// Holds a key for this frame, unless it's G (enRAGE's) or unknown. True if held.
        private static bool HookupsHold(uint key)
        {
            if (key == 0 || key == HookupsGHash)
                return false;
            PAD.DISABLE_CONTROL_ACTION(0, key, false);
            return true;
        }

        /// What became of her handle, for the log: CasualHookups may hide her or put another ped in her place.
        private static string HerState(int h)
        {
            if (!ENTITY.DOES_ENTITY_EXIST(h))
                return "gone";
            return (ENTITY.IS_ENTITY_DEAD(h) ? "dead, " : "") + (ENTITY.IS_ENTITY_VISIBLE(h) ? "visible" : "hidden") + ", alpha " + ENTITY.GET_ENTITY_ALPHA(h) +
                   ", " + ENTITY.GET_ENTITY_COORDS(h, true, true).DistanceTo(Game.Player.Character.Position).ToString("0.0") + " m from you";
        }

        /// The player in a scene of CasualHookups': undressing (its outfit change), or his controls off.
        private static bool PlayerInScene(int me, bool controlOff)
        {
            return controlOff || ENTITY.IS_ENTITY_PLAYING_ANIM(me, "mech_inventory@clothing@outfit_change", "outfit_change_unarmed", 3);
        }

        /// After its key: did a scene follow, and when is it over.
        private void HookupsWatch(int now)
        {
            HookupRun h = _hookRun;
            if (h == null || now < h.NextLook)
                return;
            h.NextLook = now + 250;
            Ped me = Game.Player.Character;
            bool there = ENTITY.DOES_ENTITY_EXIST(h.Handle) && !ENTITY.IS_ENTITY_DEAD(h.Handle);
            float d = there ? ENTITY.GET_ENTITY_COORDS(h.Handle, true, true).DistanceTo(me.Position) : 999f;
            bool controlOff = !PLAYER.IS_PLAYER_CONTROL_ON(Game.Player.Handle);
            bool inScene = PlayerInScene(me.Handle, controlOff);   // from his side: she may be hidden or stood in for
            if (h.StartedAt == 0)
            {
                // a willing woman stands close to him at the ask anyway, so for her only his side begins a scene (review,
                // 2026-10-31: her standing distance after a B press with no prompt counted as its scene, and the fallback
                // never came); the two close still stirs a watch on a pressed known kind (the 60 s end falls back), and
                // begins one for a woman who never said yes (did the hold hold)
                bool close = h.Pressed && there && d < 0.9f;
                bool sign = h.Willing ? inScene : inScene || close;
                if (sign || (close && !h.FromAsk))
                    h.Stirred = true;
                if (inScene)
                    h.SawScene = true;
                bool due = h.FromAsk ? now - h.AskedAt >= HookupAskWaitMs && (!h.Pressed || now - h.PressedAt >= HookupFallbackMs)
                                     : now - h.PressedAt >= HookupFallbackMs;
                if (h.Willing && !h.Stirred && due)
                {
                    // her words said yes and CasualHookups started nothing (it turned Saint Denis' working girls down,
                    // 2026-10-26; it showed no prompt at all for Opal Ackerman, 2026-10-31): the night is RealNPCs' own, so
                    // what she said and what happens agree (Lex)
                    bool takes = HookupsTakes(h.Handle);
                    string model = ModelOf(h.Handle);
                    Log.Write("HOOKUPS with " + h.Name + " (" + (model ?? "?") + "): no sign of its scene " +
                              (h.FromAsk ? (now - h.AskedAt) / 1000 + " s after Hook up (its key " + (h.Pressed ? "pressed " + (now - h.PressedAt) / 1000 + " s ago" : "never seen pressed: its prompt may never have shown") + ")"
                                         : (now - h.PressedAt) / 1000 + " s after its key") +
                              (takes ? ", though it has taken her kind before" : "") + " (her: " + HerState(h.Handle) + ")");
                    // learned as "won't" only from a press that came to nothing (or two asks with none), and only while its
                    // chances are surely RealNPCs' 0/100/0 (else its own dice may have said no)
                    if (takes || model == null || _store == null)
                    {
                    }
                    else if (HookupsOwnDice)
                        Log.Write("HOOKUPS: " + model + " not learned as a kind it won't take: its chances may be its own (" + _hookupsStartOdds + ") until the next start");
                    else if (h.Pressed)
                        HookupsLearnWont(h.Handle, h.Name);
                    else
                    {
                        string miss = "hookups no press:" + model.ToLowerInvariant();
                        _store.Bump(miss, 1);
                        if (_store.Count(miss) >= 2)
                            HookupsLearnWont(h.Handle, h.Name);
                        else
                            Log.Write("HOOKUPS: " + model + " not learned yet: its key was never seen pressed (once; twice and it's learned as a kind it won't take)");
                    }
                    _hookRun = null;
                    _hookupsLast = h.Name + ": CasualHookups started nothing, RealNPCs' night instead";
                    TrystFallback(h.Offer, "CasualHookups started nothing", h.Offer == null || !h.Offer.Asked, true);   // asked: her yes was just said
                    return;
                }
                if (sign)
                {
                    if (h.CloseSince == 0)
                        h.CloseSince = now;
                    if (now - h.CloseSince >= 3000)
                    {
                        h.StartedAt = now;
                        _hookupsScenes++;
                        if (h.Willing)
                        {
                            HookupsLearnTakes(h.Handle, h.Name);   // his side of its scene seen (a willing watch begins on nothing else)
                            h.After = _engine.Tryst(h.Who, h.Kind, Bridge.Now(), _placeTown, _placeKind == "saloon", h.Offer != null ? h.Offer.Cents : 0);   // written down as RealNPCs' own night is: the record, the husband's chance, the price (the review of items 15-19)
                            _trystOffer = null;
                            _trystCount++;
                            _trystLast = h.Name + " (" + h.Kind + ", CasualHookups)";
                            Log.Write("HOOKUPS with " + h.Name + ": its scene seems to have begun " + (h.Pressed ? (now - h.PressedAt) / 1000 + " s after its key (" : (now - h.AskedAt) / 1000 + " s after Hook up, its key not seen pressed (") + "your controls " + (controlOff ? "off" : "on") + "; her: " + HerState(h.Handle) + "); noted as a night together");
                        }
                        else
                        {
                            Log.Write("HOOKUPS: a scene with " + h.Name + " seems to have begun though she never said yes to you: the hold on its key didn't work (your controls " + (controlOff ? "off" : "on") + "; her: " + HerState(h.Handle) + ")");
                            if (!_hookupsToldHoldFailed)
                            {
                                // with its chances at always giving in, every woman would go along: say how to have its own dice back
                                _hookupsToldHoldFailed = true;
                                Feed.Notice("RealNPCs", "RealNPCs couldn't stop CasualHookups' Flirt on a woman who hadn't said yes. To give it back its own chances, set them on the settings panel's Romance page.");
                            }
                        }
                    }
                }
                else
                    h.CloseSince = 0;
                int since = h.Pressed ? h.PressedAt : h.AskedAt;   // a watch the ask began, not pressed yet, counts from the ask
                if (h.StartedAt == 0 && (now - since > 60000 || !there))
                {
                    Log.Write(h.Willing ? "HOOKUPS with " + h.Name + ": nothing seemed to come of " + (h.Pressed ? "its key" : "Hook up") + " in " + (now - since) / 1000 + " s (" + (there ? d.ToString("0.0") + " m apart" : "she's gone") + ", your controls " + (controlOff ? "off" : "on") + ")"
                                        : "HOOKUPS held: no scene followed with " + h.Name + ", so the hold worked");
                    _hookupsLast = h.Name + (h.Willing ? ": nothing came of it" : ": held, no scene");
                    _hookRun = null;
                    if (h.Willing && there && h.Offer != null && h.Offer == _trystOffer)
                    {
                        // signs (the two close), never a scene: still her yes, so the player isn't left waiting
                        TrystFallback(h.Offer, "nothing came of CasualHookups' key", !h.Offer.Asked, true);
                    }
                }
                return;
            }
            // under way: over once he's out of it (controls back, not undressing) and apart from her or she's gone, for
            // 3 s, or after 20 minutes
            bool apart = !inScene && (!there || d > 3f);
            if (apart)
            {
                if (h.ApartSince == 0)
                    h.ApartSince = now;
            }
            else
                h.ApartSince = 0;
            if ((h.ApartSince != 0 && now - h.ApartSince >= 3000) || now - h.StartedAt > 1200000)
            {
                if (h.Willing && h.After != null && there)
                    _ticker.Push(h.Name + ", thinking", h.After, "near", h.Handle);
                Log.Write("HOOKUPS over: " + h.Name + " (" + (now - h.StartedAt) / 1000 + " s; her: " + HerState(h.Handle) + ")" + (h.Willing ? "; she thinks: " + h.After : ""));
                _hookupsLast = h.Name + (h.Willing ? ", a night together" : ", unasked");
                _hookRun = null;
            }
        }

        /// F7.
        private string HookupsStatus()
        {
            if (!_hookupsLooked)
                return "not looked for yet";
            if (_hookupsIni == null)
                return "not installed";
            TrystOffer o = _trystOffer;
            return "installed, its Flirt on " + HookupsKeyText + ", chances " + _hookupsOdds + "; " + (HookupsLinked ? "RealNPCs decides" : "left alone") +
                   "; willing now: " + (o != null && o.Hookups && Game.GameTime <= o.Until ? o.Name + " (" + (o.Until - Game.GameTime) / 1000 + " s left)" : "nobody") +
                   "; held " + _hookupsHeld + " times, scenes seen " + _hookupsScenes + (_hookRun != null ? "; watching " + _hookRun.Name : "") + (_hookupsLast != null ? "; last " + _hookupsLast : "");
        }
    }
}
