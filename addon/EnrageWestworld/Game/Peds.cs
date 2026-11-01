// Every ped in the world, once a frame (Lex in game, 2026-10-31, Saint Denis: after going in and out of the saloon
// every name plate went for minutes; F7 showed "TEST near: nobody" with people standing about). The API's
// World.GetAllPeds() calls Script Hook's worldGetAllPeds through its own wrapper and, on any failure, returns an empty
// array without a word, so every part of the add-on went blind at once. Peds.All() calls ScriptHookRDR2.dll's export
// itself (int worldGetAllPeds(int *arr, int arrSize)) into a reused buffer, builds the Ped[] once a game frame and hands
// the same array to every caller that frame: never write into it (copy first). A failure, or a 0 (never true in the
// world: Arthur is in the pool himself) gives the last good list back, cut to the handles that still exist and are still
// peds, for as long as the trouble lasts; logged once a minute (and once when it's over), counted for F7 "TEST peds:".
// With the export missing it falls back to
// World.GetAllPeds and says so once. Script thread only: a call from another thread is logged once and gets nothing.
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using RDR2;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    internal static class Peds
    {
        private const int BufferSize = 2048, LogGapMs = 60000;

        [DllImport("ScriptHookRDR2.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "?worldGetAllPeds@@YAHPEAHH@Z")]   // the C++ name ScriptHookRDR2.dll exports (int __cdecl worldGetAllPeds(int*, int)); the plain name isn't exported
        private static extern int worldGetAllPeds([In, Out] int[] arr, int arrSize);

        private static readonly int[] Buffer = new int[BufferSize];
        private static readonly Ped[] None = new Ped[0];
        private static Ped[] _frameList = None, _lastGood = None;
        private static int _frame = -1, _lastGoodAt, _thread, _loggedAt = -LogGapMs, _lastCount;
        // the API's own wrapper, always: called directly, worldGetAllPeds sometimes faults inside the game (an access
        // violation .NET can't catch: the game went down, Lex 2026-10-31 20:54, 0.7.11); the wrapper catches it and gives
        // nobody, and the last good list below covers that
        private static bool _haveFrame, _apiOnly = true, _otherThreadLogged;
        private static int _calls, _empties, _failures, _keptFrames, _lastKept, _lastFailAt, _peak;
        private static int _failingSince;
        private static bool _failing, _everGood;
        private static string _lastWhy;
        private static readonly List<Ped> Kept = new List<Ped>(BufferSize);

        // Every ped in the world this frame. The array is shared by every caller this frame: read it, don't change it.
        public static Ped[] All()
        {
            int tid = Thread.CurrentThread.ManagedThreadId;
            if (_thread == 0)
                _thread = tid;
            else if (tid != _thread)
            {
                if (!_otherThreadLogged)
                {
                    _otherThreadLogged = true;
                    Log.Write("PEDS list asked for from another thread (" + (Thread.CurrentThread.Name ?? "#" + tid) + "): it gets nobody; natives belong to the script thread");
                }
                return None;
            }
            int frame = Game.FrameCount;
            if (_haveFrame && frame == _frame)
                return _frameList;
            _frame = frame;
            _haveFrame = true;
            _calls++;
            _frameList = Build();
            return _frameList;
        }

        private static Ped[] Build()
        {
            int now = Environment.TickCount;
            int n;
            string why = null;
            if (_apiOnly)
            {
                Ped[] api = None;
                try { api = World.GetAllPeds() ?? None; }
                catch (Exception e) { why = "failed: " + e.Message; }
                if (why == null && api.Length > 0)
                    return Good(api, now);
                return Bad(why ?? "empty", now);
            }
            try
            {
                n = worldGetAllPeds(Buffer, BufferSize);
            }
            catch (Exception e) when (e is DllNotFoundException || e is EntryPointNotFoundException || e is BadImageFormatException)
            {
                _apiOnly = true;
                Log.Write("PEDS: ScriptHookRDR2.dll's worldGetAllPeds can't be called (" + e.GetType().Name + ": " + e.Message + "), so the list comes from World.GetAllPeds, which gives nobody when it fails");
                return Build();
            }
            catch (Exception e)
            {
                return Bad("failed: " + e.Message, now);
            }
            if (n < 0)
                return Bad("failed: worldGetAllPeds gave " + n, now);
            if (n == 0)
                return Bad("empty", now);
            if (n > BufferSize)
                n = BufferSize;
            int count = 0;
            for (int i = 0; i < n; i++)
                if (Buffer[i] != 0)
                    count++;
            Ped[] list = new Ped[count];
            int k = 0;
            for (int i = 0; i < n; i++)
                if (Buffer[i] != 0)
                    list[k++] = new Ped(Buffer[i]);
            return Good(list, now);
        }

        private static Ped[] Good(Ped[] list, int now)
        {
            if (_failing)
            {
                _failing = false;
                Log.Write("PEDS list: back, " + list.Length + " peds after " + (now - _failingSince) / 1000 + " s of trouble");
            }
            _everGood = true;
            _lastGood = list;
            _lastGoodAt = now;
            _lastCount = list.Length;
            if (list.Length > _peak)
                _peak = list.Length;
            return list;
        }

        // A failure or an empty list: the last good one, cut to who is still a ped in the world, for as long as the trouble
        // lasts (Lex's went on for minutes; a 5 s cutoff left him as blind as before). The survivors become the last good
        // list, so each frame checks only them: one DOES_ENTITY_EXIST and one IS_ENTITY_A_PED a ped (a handle freed by a
        // ped that went can be handed to a wagon or a prop). Nobody new shows until the export gives a list again.
        private static Ped[] Bad(string why, int now)
        {
            if (why == "empty")
                _empties++;
            else
                _failures++;
            _lastWhy = why;
            _lastFailAt = now;
            if (!_failing)
            {
                _failing = true;
                _failingSince = now;
            }
            Ped[] kept = None;
            if (_lastGood.Length > 0)
            {
                Kept.Clear();
                for (int i = 0; i < _lastGood.Length; i++)
                {
                    Ped p = _lastGood[i];
                    if (p != null && ENTITY.DOES_ENTITY_EXIST(p.Handle) && ENTITY.IS_ENTITY_A_PED(p.Handle))
                        Kept.Add(p);
                }
                kept = Kept.Count == _lastGood.Length ? _lastGood : Kept.ToArray();
                _lastGood = kept;
                _keptFrames++;
            }
            _lastKept = kept.Length;
            _lastCount = kept.Length;
            if (now - _loggedAt >= LogGapMs)
            {
                _loggedAt = now;
                Log.Write("PEDS list: " + why + " (" + Where() + "; " + (!_everGood
                    ? "nothing kept: no good list yet"
                    : kept.Length + " kept from the last good list, " + (now - _lastGoodAt) / 1000 + " s old") + ")");
            }
            return kept;
        }

        private static string Where()
        {
            try
            {
                int me = Game.Player.Character.Handle;
                int interior = INTERIOR.GET_INTERIOR_FROM_ENTITY(me);
                return interior != 0 ? "indoors " + interior : "out of doors";
            }
            catch
            {
                return "where unknown";
            }
        }

        // F7 "TEST peds:".
        public static string Status()
        {
            int now = Environment.TickCount;
            return (_apiOnly ? "from World.GetAllPeds (its own wrapper catches what worldGetAllPeds throws)" : "from worldGetAllPeds") + ", " + _lastCount + " now (most " + _peak + "), " +
                _calls + " frames listed; empty " + _empties + ", failed " + _failures + ", the last good list given back " + _keptFrames + " times" +
                (_lastWhy != null ? "; last trouble " + (now - _lastFailAt) / 1000 + " s ago: " + _lastWhy + " (" + _lastKept + " kept)" : string.Empty) +
                (_otherThreadLogged ? "; asked for from another thread" : string.Empty);
        }
    }
}
