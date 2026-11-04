// The VRAM gauge's reader (Lex, 2026-11-01), on a thread of its own. Every 2.5 s: the whole card's dedicated memory in
// use (the game, the AI model and everything else on it; the figure Task Manager shows) from Windows' performance
// counters "GPU Adapter Memory" \ "Dedicated Usage", one instance per adapter, all read in one ReadCategory. The adapter
// is the game's own (the one this process holds the most memory on, from "GPU Process Memory", asked until known), else
// the one with the most in use. Its size: DXGI's DedicatedVideoMemory for the same LUID (CreateDXGIFactory1,
// EnumAdapters1, GetDesc1 through the COM vtable, once), else the display driver's HardwareInformation.qwMemorySize in
// the registry. The game thread reads only Latest (one volatile reference; a new Sample only when the words or the colour
// change) and the read's time; no game natives here, nothing here runs per frame. Opening the counters can take a while
// the first time: that is done here too. Unavailable (no such counters, or reads that keep failing): logged once, and the
// gauge stays blank for the session. The counters go by their English names; .NET looks a category up in English first,
// but a Windows that knew them only by other names would leave the gauge blank (PDH's PdhAddEnglishCounter is the way
// round that, if it is ever needed). Nothing here may throw into the game: every step is caught.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using EnrageWestworld.Core;
using Microsoft.Win32;

namespace EnrageWestworld.InGame
{
    internal static class VramReader
    {
        /// One reading as the gauge shows it; never changed once made.
        internal sealed class Sample
        {
            public readonly string Text, Formed;
            public readonly int Tier;
            public readonly long Used, Total;

            public Sample(string text, string formed, int tier, long used, long total)
            {
                Text = text;
                Formed = formed;
                Tier = tier;
                Used = used;
                Total = total;
            }
        }

        private sealed class Card
        {
            public ulong Luid;
            public string Name, From;
            public long Bytes;
        }

        private const string AdapterCategory = "GPU Adapter Memory", ProcessCategory = "GPU Process Memory", Dedicated = "Dedicated Usage";
        public const int EveryMs = 2500;
        private const int StaleMs = 15000, GameAskMs = 30000, GameAsks = 10, FailsBeforeGivingUp = 8;

        private static volatile bool _wanted, _started, _stop, _gaveUp, _haveRead;
        private static volatile Sample _latest;
        private static volatile int _readAt, _reads, _readMs, _openMs, _fails, _adapters;
        private static volatile string _state = "not started", _adapter, _luid, _how, _totalFrom, _cardsWhy;
        private static long _usedNow, _totalNow;   // the last read, exact, for F7 (Interlocked)
        private static Thread _thread;
        private static readonly AutoResetEvent Wake = new AutoResetEvent(false);
        private static Func<string, string> _form;
        private static Action<string> _log;
        private static int _pid;

        /// The latest reading, or null (none yet, given up, or none for 15 s).
        public static Sample Latest
        {
            get
            {
                Sample s = _latest;
                return s != null && _haveRead && Environment.TickCount - _readAt < StaleMs ? s : null;
            }
        }

        public static bool Started
        {
            get { return _started; }
        }

        /// The gauge shown or not (the setting and the AI model, from the game thread): the thread reads only while it is.
        public static void Want(bool on)
        {
            bool was = _wanted;
            _wanted = on;
            if (on && !was && _started)
                try { Wake.Set(); } catch { }
        }

        /// Once, from the game thread, when the gauge is first shown. form: the words as the frame draws them (Draw.Formed),
        /// made here so the frame makes nothing; log: thread-safe; pid: the game's process.
        public static void Start(Func<string, string> form, Action<string> log, int pid)
        {
            if (_started)
                return;
            _started = true;
            _stop = false;   // started again after a Stop (the review)
            _form = form;
            _log = log;
            _pid = pid;
            _state = "starting";
            try
            {
                var t = new Thread(Run) { IsBackground = true, Name = "RealNPCs VRAM gauge", Priority = ThreadPriority.BelowNormal };
                _thread = t;
                t.Start();
            }
            catch (Exception ex)
            {
                GiveUp("its thread couldn't start (" + ex.Message + ")");
            }
        }

        /// The thread ended with the script (OnAborted), before Script Hook unloads it.
        public static void Stop()
        {
            _stop = true;
            Thread t = _thread;
            if (t == null)
                return;
            try { Wake.Set(); } catch { }
            try { t.Join(2000); } catch { }
            _thread = null;
        }

        private static void Run()
        {
            try
            {
                _state = "opening Windows' GPU memory counters";
                long t0 = Stopwatch.GetTimestamp();
                PerformanceCounterCategory adapters = null;
                string why = null;
                try
                {
                    if (PerformanceCounterCategory.Exists(AdapterCategory))
                        adapters = new PerformanceCounterCategory(AdapterCategory);
                    else
                        why = "Windows has no \"" + AdapterCategory + "\" counters here (Windows 10 has them from 2017)";
                }
                catch (Exception ex)
                {
                    why = "Windows' \"" + AdapterCategory + "\" counters couldn't be opened (" + ex.GetType().Name + ": " + ex.Message + ")";
                }
                if (why != null)
                {
                    GiveUp(why);
                    return;
                }
                _openMs = Ms(t0);
                List<Card> cards = Cards();
                var used = new Dictionary<ulong, long>();
                ulong game = 0, shown = 0;
                int asks = 0, askAt = Environment.TickCount;
                while (!_stop)
                {
                    if (!_wanted)
                    {
                        _state = "waiting (the gauge isn't shown)";
                        Wake.WaitOne(1000);
                        continue;
                    }
                    long r0 = Stopwatch.GetTimestamp();
                    try
                    {
                        used.Clear();
                        InstanceDataCollection ded = adapters.ReadCategory()[Dedicated];
                        if (ded == null)
                            throw new InvalidOperationException("no \"" + Dedicated + "\" counter in \"" + AdapterCategory + "\"");
                        foreach (InstanceData d in ded.Values)
                        {
                            ulong id;
                            if (!Vram.LuidOf(d.InstanceName, out id))
                                continue;
                            long v = d.RawValue, had;
                            if (!used.TryGetValue(id, out had) || v > had)
                                used[id] = v;   // one adapter's phys_0, phys_1 (linked cards): the larger
                        }
                        if (game == 0 && asks < GameAsks && Environment.TickCount - askAt >= 0)
                        {
                            asks++;
                            askAt = Environment.TickCount + GameAskMs;
                            game = GameAdapter();
                        }
                        ulong luid;
                        bool isGame;
                        if (!Vram.Pick(used, game, out luid, out isGame))
                            throw new InvalidOperationException("no adapter in \"" + AdapterCategory + "\"");
                        long u = used[luid];
                        Card card = CardOf(cards, luid);
                        long total = card != null ? card.Bytes : 0;
                        Publish(u, total);
                        Interlocked.Exchange(ref _usedNow, u);
                        Interlocked.Exchange(ref _totalNow, total);
                        _adapters = used.Count;
                        _readAt = Environment.TickCount;
                        _haveRead = true;
                        _reads++;
                        _fails = 0;
                        if (luid != shown)
                        {
                            bool first = shown == 0;
                            shown = luid;
                            _adapter = card != null && !string.IsNullOrEmpty(card.Name) ? card.Name.Trim() : null;
                            _luid = Vram.LuidText(luid);
                            _how = isGame ? "the game's GPU" : game != 0 ? "the busiest (the game's isn't in the counters)" : "the busiest";
                            _totalFrom = card != null ? card.From : "unknown" + (_cardsWhy != null ? " (" + _cardsWhy + ")" : "");
                            _log?.Invoke("VRAM gauge: " + (first ? "" : "now ") + "reading " + (_adapter ?? "the adapter") + " (" + _how + ", luid " + _luid + ", " + used.Count +
                                         " in the counters) every " + EveryMs / 1000.0 + " s from Windows' " + AdapterCategory + " counters; its size " +
                                         (total > 0 ? Vram.Gb(total) + " GB from " + _totalFrom : "unknown") + ". " + (first ? "First: " : "Now: ") + Vram.Text(u, total) +
                                         (first ? " (counters opened in " + _openMs + " ms)" : ""));
                        }
                        _state = "reading";
                    }
                    catch (ThreadAbortException)
                    {
                        throw;   // the script's domain going away: no failed read, nothing logged
                    }
                    catch (Exception ex)
                    {
                        int f = ++_fails;
                        if (f == 1)
                            _log?.Invoke("VRAM gauge: a read failed (" + ex.GetType().Name + ": " + ex.Message + "); trying again");
                        // the counters can fail for a while (a driver update, a sleep and wake): after eight failures running the
                        // gauge tries every 30 s instead of giving up for good (the review)
                        if (f == FailsBeforeGivingUp)
                            _log?.Invoke("VRAM gauge: " + f + " reads running failed, the last: " + ex.GetType().Name + ": " + ex.Message + "; trying every 30 s now");
                        _state = f >= FailsBeforeGivingUp ? "reads failing (trying every 30 s)" : "a read failed, trying again";
                    }
                    _readMs = Ms(r0);
                    Wake.WaitOne(_fails >= FailsBeforeGivingUp ? 30000 : EveryMs);
                }
                _state = "stopped";
            }
            catch (ThreadAbortException)
            {
                _state = "stopped";
            }
            catch (Exception ex)
            {
                GiveUp("its thread stopped on " + ex.GetType().Name + ": " + ex.Message);
            }
            finally
            {
                try { PerformanceCounter.CloseSharedResources(); } catch { }
            }
        }

        /// A new Sample only when the words or the colour change, its text formed here, not by the frame.
        private static void Publish(long used, long total)
        {
            string text = Vram.Text(used, total);
            int tier = Vram.Tier(used, total);
            Sample last = _latest;
            if (last != null && last.Tier == tier && last.Text == text)
                return;
            string formed = text;
            try
            {
                if (_form != null)
                    formed = _form(text) ?? text;
            }
            catch
            {
            }
            _latest = new Sample(text, formed, tier, used, total);
        }

        private static void GiveUp(string why)
        {
            if (_gaveUp)
                return;
            _gaveUp = true;
            _latest = null;
            _state = "unavailable: " + why;
            _log?.Invoke("VRAM gauge: " + why + "; the gauge stays off for this session (F7: TEST vram)");
        }

        private static int Ms(long since)
        {
            return (int)((Stopwatch.GetTimestamp() - since) * 1000L / Stopwatch.Frequency);
        }

        // ---- the game's own adapter ---------------------------------------------------------------------------------

        /// The adapter this process holds the most dedicated memory on ("GPU Process Memory", pid_<ours>_luid_...), or 0.
        private static ulong GameAdapter()
        {
            try
            {
                InstanceDataCollection ded = new PerformanceCounterCategory(ProcessCategory).ReadCategory()[Dedicated];
                if (ded == null)
                    return 0;
                ulong best = 0;
                long most = 0;
                foreach (InstanceData d in ded.Values)
                {
                    ulong id;
                    if (Vram.PidOf(d.InstanceName) != _pid || !Vram.LuidOf(d.InstanceName, out id))
                        continue;
                    if (d.RawValue > most)
                    {
                        most = d.RawValue;
                        best = id;
                    }
                }
                return best;
            }
            catch
            {
                return 0;
            }
        }

        // ---- the card's size ----------------------------------------------------------------------------------------

        private static List<Card> Cards()
        {
            var cards = new List<Card>();
            string why = null;
            try
            {
                cards.AddRange(Dxgi());
            }
            catch (Exception ex)
            {
                why = "DXGI: " + ex.Message;
            }
            try
            {
                cards.AddRange(RegistryCards());
            }
            catch (Exception ex)
            {
                why = (why != null ? why + "; " : "") + "the registry: " + ex.Message;
            }
            _cardsWhy = why;
            return cards;
        }

        /// DXGI's card for the LUID; else the registry's of the same name; else the registry's largest.
        private static Card CardOf(List<Card> cards, ulong luid)
        {
            Card dxgi = null, named = null, largest = null;
            foreach (Card c in cards)
                if (c.Luid == luid && c.From == "DXGI")
                    dxgi = c;
            foreach (Card c in cards)
            {
                if (c.From == "DXGI")
                    continue;
                if (dxgi != null && c.Name != null && dxgi.Name != null && string.Equals(c.Name.Trim(), dxgi.Name.Trim(), StringComparison.OrdinalIgnoreCase))
                    named = c;
                if (largest == null || c.Bytes > largest.Bytes)
                    largest = c;
            }
            // DXGI's dedicated memory can read a little under the card's physical size (a 16 GB card as 15.6): the registry's
            // entry of the same name says what's on the card, as Task Manager shows it (the review)
            if (dxgi != null && dxgi.Bytes > 0)
                return named != null && named.Bytes > dxgi.Bytes ? new Card { Luid = luid, Name = dxgi.Name, From = named.From, Bytes = named.Bytes } : dxgi;
            Card got = named ?? largest;
            if (got != null && dxgi != null && got.Luid == 0)
                return new Card { Luid = luid, Name = dxgi.Name, From = got.From, Bytes = got.Bytes };
            return got ?? dxgi;
        }

        // DXGI through the COM vtable (no interop interfaces): IDXGIFactory1 slot 12 EnumAdapters1, IDXGIAdapter1 slot 10
        // GetDesc1, IUnknown's Release by Marshal.Release. A software adapter (Flags 2, the basic render driver) is left out.
        [DllImport("dxgi.dll", ExactSpelling = true)]
        private static extern int CreateDXGIFactory1([In] ref Guid riid, out IntPtr factory);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int EnumAdapters1Fn(IntPtr self, uint index, out IntPtr adapter);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int GetDesc1Fn(IntPtr self, out AdapterDesc1 desc);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct AdapterDesc1
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string Description;
            public uint VendorId, DeviceId, SubSysId, Revision;
            public UIntPtr DedicatedVideoMemory, DedicatedSystemMemory, SharedSystemMemory;
            public uint LuidLow;
            public int LuidHigh;
            public uint Flags;
        }

        private const int DxgiNotFound = unchecked((int)0x887A0002);
        private const int SlotEnumAdapters1 = 12, SlotGetDesc1 = 10;

        private static Delegate Slot(IntPtr com, int index, Type type)
        {
            IntPtr table = Marshal.ReadIntPtr(com);
            return Marshal.GetDelegateForFunctionPointer(Marshal.ReadIntPtr(table, index * IntPtr.Size), type);
        }

        private static List<Card> Dxgi()
        {
            var cards = new List<Card>();
            Guid iid = new Guid("770aae78-f26f-4dba-a829-253c83d1b387");   // IID_IDXGIFactory1
            IntPtr factory;
            int hr = CreateDXGIFactory1(ref iid, out factory);
            if (hr < 0 || factory == IntPtr.Zero)
                throw new InvalidOperationException("CreateDXGIFactory1 gave 0x" + hr.ToString("x8"));
            try
            {
                var enumAdapters = (EnumAdapters1Fn)Slot(factory, SlotEnumAdapters1, typeof(EnumAdapters1Fn));
                for (uint i = 0; i < 16; i++)
                {
                    IntPtr adapter;
                    hr = enumAdapters(factory, i, out adapter);
                    if (hr == DxgiNotFound || hr < 0 || adapter == IntPtr.Zero)
                        break;
                    try
                    {
                        var getDesc = (GetDesc1Fn)Slot(adapter, SlotGetDesc1, typeof(GetDesc1Fn));
                        AdapterDesc1 d;
                        if (getDesc(adapter, out d) >= 0 && (d.Flags & 2u) == 0)
                            cards.Add(new Card { Luid = Vram.Luid(d.LuidLow, d.LuidHigh), Name = d.Description, From = "DXGI", Bytes = (long)d.DedicatedVideoMemory.ToUInt64() });
                    }
                    finally
                    {
                        Marshal.Release(adapter);
                    }
                }
            }
            finally
            {
                Marshal.Release(factory);
            }
            return cards;
        }

        private const string DisplayClass = @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}";

        /// The display drivers' own word for their card's memory (0000, 0001...; a key we may not read is passed over).
        private static List<Card> RegistryCards()
        {
            var cards = new List<Card>();
            using (RegistryKey cls = Registry.LocalMachine.OpenSubKey(DisplayClass))
            {
                if (cls == null)
                    return cards;
                foreach (string sub in cls.GetSubKeyNames())
                {
                    if (sub.Length != 4 || !char.IsDigit(sub[0]))
                        continue;
                    try
                    {
                        using (RegistryKey k = cls.OpenSubKey(sub))
                        {
                            object v = k != null ? k.GetValue("HardwareInformation.qwMemorySize") : null;
                            byte[] raw = v as byte[];
                            long bytes = v is long ? (long)v : raw != null && raw.Length >= 8 ? BitConverter.ToInt64(raw, 0) : 0;
                            if (bytes > 0)
                                cards.Add(new Card { Name = k.GetValue("DriverDesc") as string, From = "the registry", Bytes = bytes });
                        }
                    }
                    catch
                    {
                    }
                }
            }
            return cards;
        }

        // ---- F7 -----------------------------------------------------------------------------------------------------

        /// TEST vram: where the number comes from, which card, used of its size, and how long ago it was read.
        public static string Status()
        {
            if (!_started)
                return "not started";
            if (_gaveUp)
                return _state;
            if (!_haveRead)
                return _state + (_fails > 0 ? " (" + _fails + " reads failed)" : "");
            long used = Interlocked.Read(ref _usedNow), total = Interlocked.Read(ref _totalNow);
            int age = Environment.TickCount - _readAt;
            return _state + ": Windows' " + AdapterCategory + "\\" + Dedicated + ", " + (_adapter ?? "the adapter") + " (" + _how + ", luid " + _luid + ", " + _adapters +
                   " in the counters); " + (used / (double)Vram.GiB).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + " / " +
                   (total > 0 ? (total / (double)Vram.GiB).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + " GB (" + Vram.Share(used, total) + ", " + Vram.TierWord(Vram.Tier(used, total)) + "), size from " + _totalFrom
                              : "? GB (size unknown: " + _totalFrom + ")") +
                   "; GB as Task Manager (1024^3); last read " + (age / 1000.0).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " s ago, " + _reads + " reads, the last " +
                   _readMs + " ms; counters opened in " + _openMs + " ms" + (_fails > 0 ? "; " + _fails + " failed since" : "") + (Latest == null ? "; NOT SHOWN (no reading in 15 s)" : "");
        }
    }
}
